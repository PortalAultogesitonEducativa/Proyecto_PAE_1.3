using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;

namespace ProyectoPAE.Servicios
{
    public class ResumenAlertasSolicitudes
    {
        public int Abiertas { get; set; }
        public int Vencidas { get; set; }
        public int PorVencer { get; set; }
    }

    public class ResultadoOperacion
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>Operaciones compartidas de las solicitudes de certificado: radicado, alertas y entrega.</summary>
    public class SolicitudCertificadoService
    {
        private readonly ApplicationDbContext _context;

        public SolicitudCertificadoService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Código correlativo de la solicitud, con el mismo estilo del radicado de PQR (SCE-2026-0001).</summary>
        public async Task<string> SiguienteRadicadoAsync()
        {
            _context.AsegurarEsquemaSolicitudesCertificado();

            int correlativo = await _context.SolicitudesCertificado.CountAsync() + 1;
            string radicado = $"SCE-{DateTime.Now.Year}-{correlativo:D4}";

            while (await _context.SolicitudesCertificado.AnyAsync(s => s.CodigoRadicado == radicado))
            {
                correlativo++;
                radicado = $"SCE-{DateTime.Now.Year}-{correlativo:D4}";
            }

            return radicado;
        }

        /// <summary>Cuántas solicitudes abiertas están vencidas o próximas a vencer. Nunca lanza error: es un aviso.</summary>
        public async Task<ResumenAlertasSolicitudes> ObtenerResumenAlertasAsync()
        {
            var resumen = new ResumenAlertasSolicitudes();

            try
            {
                _context.AsegurarEsquemaSolicitudesCertificado();

                var abiertas = await _context.SolicitudesCertificado
                    .AsNoTracking()
                    .Where(s => s.Activo &&
                                (s.Estado == EstadoSolicitudCertificado.Pendiente || s.Estado == EstadoSolicitudCertificado.EnProceso))
                    .ToListAsync();

                var ahora = DateTime.Now;
                resumen.Abiertas = abiertas.Count;

                foreach (var solicitud in abiertas)
                {
                    var plazo = PlazoCertificado.Evaluar(solicitud, ahora);
                    if (plazo.Clave == "vencida")
                        resumen.Vencidas++;
                    else if (plazo.Clave == "por-vencer")
                        resumen.PorVencer++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error SolicitudCertificadoService/ObtenerResumenAlertas]: {ex.Message}");
            }

            return resumen;
        }

        /// <summary>
        /// Marca la solicitud como entregada, enlaza el certificado (si hay uno) y avisa a quien corresponda.
        /// </summary>
        /// <param name="idEstudianteEsperado">Si se indica, la solicitud debe pertenecer a ese estudiante.</param>
        /// <param name="notificarEstudiante">Falso cuando otro flujo ya le avisó al estudiante que se emitió el certificado.</param>
        public async Task<ResultadoOperacion> MarcarEntregadaAsync(
            int idSolicitud,
            int? idCertificado,
            DateTime fechaEntrega,
            int idGestor,
            string? observaciones,
            int? idEstudianteEsperado = null,
            bool notificarEstudiante = true)
        {
            _context.AsegurarEsquemaSolicitudesCertificado();

            var solicitud = await _context.SolicitudesCertificado
                .FirstOrDefaultAsync(s => s.IdSolicitud == idSolicitud && s.Activo);

            if (solicitud == null)
                return new ResultadoOperacion { Exito = false, Mensaje = "No se encontró la solicitud." };

            if (idEstudianteEsperado.HasValue && solicitud.IdEstudiante != idEstudianteEsperado.Value)
                return new ResultadoOperacion { Exito = false, Mensaje = "La solicitud no corresponde a este estudiante." };

            if (!PlazoCertificado.EstaAbierta(solicitud))
                return new ResultadoOperacion { Exito = false, Mensaje = $"La solicitud {solicitud.CodigoRadicado} ya está {solicitud.Estado.ToLower()}." };

            var gestor = await _context.Usuarios.FindAsync(idGestor);
            string nombreGestor = gestor != null
                ? $"{gestor.NOMBRES} {gestor.APELLIDOS}".Trim()
                : "Sistema PAE";

            solicitud.Estado = EstadoSolicitudCertificado.Entregada;
            solicitud.FechaEntrega = fechaEntrega;
            solicitud.IdUsuarioGestor = idGestor;
            solicitud.NombreGestor = nombreGestor;
            if (idCertificado.HasValue)
                solicitud.IdCertificado = idCertificado.Value;

            string nota = $"[{DateTime.Now:dd/MM/yyyy HH:mm}] Entrega registrada por {nombreGestor}" +
                          (string.IsNullOrWhiteSpace(observaciones) ? "." : $": {observaciones.Trim()}");
            solicitud.ObservacionesGestion = string.IsNullOrWhiteSpace(solicitud.ObservacionesGestion)
                ? nota
                : solicitud.ObservacionesGestion + Environment.NewLine + nota;

            string tipo = EstadoSolicitudCertificado.NombreTipo(solicitud.TipoCertificado).ToLower();
            string mensaje = $"Tu solicitud {solicitud.CodigoRadicado} ({tipo} de {solicitud.NombreEstudiante}) fue entregada.";

            // Avisos: el acudiente que la pidió y, si corresponde, el estudiante.
            bool solicitanteEsAcudiente = solicitud.RolSolicitante == "acudiente";
            if (solicitanteEsAcudiente)
                AgregarNotificacion(solicitud.IdSolicitante, idGestor, mensaje);

            if (notificarEstudiante && !(solicitanteEsAcudiente && solicitud.IdSolicitante == solicitud.IdEstudiante))
                AgregarNotificacion(solicitud.IdEstudiante, idGestor, $"Tu {tipo} solicitado con el radicado {solicitud.CodigoRadicado} fue entregado.");

            await _context.SaveChangesAsync();

            return new ResultadoOperacion { Exito = true, Mensaje = $"La solicitud {solicitud.CodigoRadicado} quedó entregada." };
        }

        private void AgregarNotificacion(int idUsuarioDestino, int idEmisor, string mensaje)
        {
            _context.Notificaciones.Add(new Notificacion
            {
                Titulo = "Certificado entregado",
                Mensaje = mensaje,
                RolDestino = $"user_{idUsuarioDestino}",
                FechaCreacion = DateTime.Now,
                ID_UsuarioEmisor = idEmisor,
                Activa = true,
                Prioridad = "leve"
            });
        }
    }
}