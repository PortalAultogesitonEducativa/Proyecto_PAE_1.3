using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProyectoPAE.Models;

namespace ProyectoPAE.Servicios
{
    public class ResultadoVerificacion
    {
        public bool Valido { get; set; }
        public string Motivo { get; set; }
        public Certificado Certificado { get; set; }
        public List<CertificadoDetalle> Detalles { get; set; }
    }

    public class CertificadoService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _claveSecreta;

        public CertificadoService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;

            // La clave se lee de appsettings.json (ver sección "Firma:ClaveSecreta")
            // NUNCA se guarda en el código ni en la base de datos.
            _claveSecreta = configuration["Firma:ClaveSecreta"];

            if (string.IsNullOrEmpty(_claveSecreta))
            {
                throw new InvalidOperationException(
                    "Falta configurar 'Firma:ClaveSecreta' en appsettings.json");
            }
        }

        /// <summary>
        /// Genera un certificado nuevo a partir de un conjunto de calificaciones
        /// del mismo estudiante, calcula el hash HMAC y lo guarda en la BD.
        /// </summary>
        public async Task<Certificado> GenerarCertificadoAsync(
            int idEstudiante,
            string tipoCertificado,
            List<int> idsCalificacion,
            int idUsuarioEmisor)
        {
            var calificaciones = await _context.Calificaciones
                .Where(c => idsCalificacion.Contains(c.ID_Calificacion) && c.ID_Estudiante == idEstudiante)
                .ToListAsync();

            if (calificaciones.Count == 0)
                throw new InvalidOperationException("No se encontraron calificaciones válidas para ese estudiante.");

            var ahora = DateTime.Now;
            var fechaSinFraccion = new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute, ahora.Second);

            var certificado = new Certificado
            {
                IdEstudiante = idEstudiante,
                TipoCertificado = tipoCertificado,
                FechaEmision = fechaSinFraccion,
                IdUsuarioEmisor = idUsuarioEmisor,
                Estado = "Vigente",
                CodigoVerificacion = GenerarCodigoUnico()
            };

            foreach (var c in calificaciones)
            {
                certificado.Detalles.Add(new CertificadoDetalle
                {
                    IdCalificacion = c.ID_Calificacion,
                    Materia = c.Materia,
                    Periodo = c.Periodo.ToString(),
                    NotaFinal = c.Nota,
                    EstadoNota = c.Nota >= 3.0m ? "Aprobado" : "Reprobado"
                });
            }

            // El hash se calcula sobre cabecera + TODAS las líneas, ya generado el código
            certificado.HashContenido = CalcularHash(certificado);

            _context.Certificados.Add(certificado);
            await _context.SaveChangesAsync();

            return certificado;
        }

        /// <summary>
        /// Busca un certificado por su código público y valida que el hash
        /// almacenado coincida con el recalculado a partir de su contenido actual.
        /// </summary>
        public async Task<ResultadoVerificacion> VerificarAsync(string codigoVerificacion)
        {
            var certificado = await _context.Certificados
                .Include(c => c.Estudiante)
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.CodigoVerificacion == codigoVerificacion);

            if (certificado == null)
            {
                return new ResultadoVerificacion
                {
                    Valido = false,
                    Motivo = "No existe ningún certificado con ese código."
                };
            }

            if (certificado.Estado == "Revocado")
            {
                return new ResultadoVerificacion
                {
                    Valido = false,
                    Motivo = "Este certificado fue revocado por la institución.",
                    Certificado = certificado,
                    Detalles = certificado.Detalles.ToList()
                };
            }

            string hashRecalculado = CalcularHash(certificado);
            bool coincide = hashRecalculado == certificado.HashContenido;

            return new ResultadoVerificacion
            {
                Valido = coincide,
                Motivo = coincide
                    ? "El certificado es auténtico y no ha sido alterado."
                    : "El contenido no coincide con la firma registrada. El certificado pudo haber sido modificado.",
                Certificado = certificado,
                Detalles = certificado.Detalles.ToList()
            };
        }

        /// <summary>
        /// Construye una cadena canónica (cabecera + detalles ordenados) y la firma
        /// con HMAC-SHA256 usando la clave secreta del servidor.
        /// </summary>
        private string CalcularHash(Certificado certificado)
        {
            var sb = new StringBuilder();

            sb.Append(certificado.IdEstudiante).Append('|');
            sb.Append(certificado.TipoCertificado).Append('|');
            sb.Append(certificado.CodigoVerificacion).Append('|');
            sb.Append(certificado.FechaEmision.ToString("O", CultureInfo.InvariantCulture)).Append('|');

            // Orden fijo por id_calificacion para que el hash sea determinista
            var detallesOrdenados = certificado.Detalles.OrderBy(d => d.IdCalificacion).ToList();

            foreach (var d in detallesOrdenados)
            {
                sb.Append(d.IdCalificacion).Append(':')
                  .Append(d.Materia).Append(':')
                  .Append(d.Periodo).Append(':')
                  .Append(d.NotaFinal.ToString(CultureInfo.InvariantCulture)).Append(':')
                  .Append(d.EstadoNota).Append(';');
            }

            byte[] claveBytes = Encoding.UTF8.GetBytes(_claveSecreta);
            byte[] contenidoBytes = Encoding.UTF8.GetBytes(sb.ToString());

            using var hmac = new HMACSHA256(claveBytes);
            byte[] firma = hmac.ComputeHash(contenidoBytes);
            return Convert.ToBase64String(firma);
        }

        private string GenerarCodigoUnico()
        {
            // Ej: PAE-8F2A91C4
            string parte = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            return $"PAE-{parte}";
        }
    }
}