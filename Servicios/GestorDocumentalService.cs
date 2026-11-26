using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;

namespace ProyectoPAE.Servicios
{
    /// <summary>
    /// Servicio centralizado de gestión documental y almacenamiento organizado en /uploads.
    /// Garantiza nombres no colisionables con consecutivos por categoría, persistencia
    /// en DOCUMENTO, DOCUMENTO_VERSION y DOCUMENTO_ETIQUETA con clasificación de datos críticos
    /// de menores de edad conforme al Art. 2.2.2.25.2.9 del Decreto 1074 de 2015.
    /// </summary>
    public class GestorDocumentalService
    {
        private readonly ApplicationDbContext _context;

        public const string ETIQUETA_CRITICA_MENOR = "Estado Crítico - Dato Menor de Edad (Dec. 1074/2015)";

        public GestorDocumentalService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Guarda un archivo físico en la subcarpeta correspondiente de /uploads con nomenclatura
        /// consecutiva única y lo registra en DOCUMENTO, DOCUMENTO_VERSION y DOCUMENTO_ETIQUETA.
        /// </summary>
        public async Task<(string RutaWeb, int IdDocumento, string NombreArchivoFinal)> GuardarArchivoConsecutivoAsync(
            IFormFile archivo,
            string categoria, // "actividades", "mejoras", "pqrs", "certificados", "extracurriculares", "documentos"
            int idUsuarioAutor,
            string titulo,
            string? descripcion = null,
            bool esCriticoMenor = false,
            string? etiquetaAdicional = null,
            int? idCurso = null,
            int? idMateria = null)
        {
            if (archivo == null || archivo.Length == 0)
                throw new ArgumentException("El archivo proporcionado está vacío o no es válido.");

            // 1. Normalizar categoría y determinar prefijo institucional
            categoria = (categoria ?? "documentos").ToLower().Trim();
            string prefijo = categoria switch
            {
                "actividades" => "ACT",
                "mejoras" => "MEJ",
                "pqrs" or "pqr" => "PQR",
                "certificados" or "certificado" => "CERT",
                "extracurriculares" or "extracurricular" => "EXT",
                _ => "DOC"
            };

            // 2. Asegurar existencia de la carpeta física
            var carpetaFisica = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", categoria);
            if (!Directory.Exists(carpetaFisica))
            {
                Directory.CreateDirectory(carpetaFisica);
            }

            // 3. Calcular número consecutivo único para esta categoría
            int maxConsecutivoCarpeta = 0;
            try
            {
                var archivosExistentes = Directory.GetFiles(carpetaFisica, $"{prefijo}_*.*");
                foreach (var f in archivosExistentes)
                {
                    var nombreSinRuta = Path.GetFileName(f);
                    var match = Regex.Match(nombreSinRuta, $@"^{prefijo}_(\d{{4,6}})_");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
                    {
                        if (num > maxConsecutivoCarpeta) maxConsecutivoCarpeta = num;
                    }
                }
            }
            catch
            {
                // Fallback si no hay permisos de escaneo de carpeta
            }

            // Consultar también en la base de datos para no colisionar
            int maxConsecutivoBD = await _context.Documentos
                .Where(d => d.TipoDocumento == categoria)
                .CountAsync();

            int consecutivoFinal = Math.Max(maxConsecutivoCarpeta, maxConsecutivoBD) + 1;

            // 4. Limpiar nombre original del archivo
            string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            string nombreSinExt = Path.GetFileNameWithoutExtension(archivo.FileName);
            string nombreLimpio = Regex.Replace(nombreSinExt, @"[^a-zA-Z0-9_\-]", "_");
            if (nombreLimpio.Length > 40) nombreLimpio = nombreLimpio.Substring(0, 40);

            string nombreArchivoConsecutivo = $"{prefijo}_{consecutivoFinal:D5}_{nombreLimpio}{extension}";
            string rutaFisicaCompleta = Path.Combine(carpetaFisica, nombreArchivoConsecutivo);

            // 5. Guardar archivo en disco
            using (var stream = new FileStream(rutaFisicaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            string rutaWeb = $"/uploads/{categoria}/{nombreArchivoConsecutivo}";

            // 6. Registrar en tabla DOCUMENTO
            var nuevoDocumento = new Documento
            {
                Titulo = string.IsNullOrWhiteSpace(titulo) ? nombreSinExt : titulo.Trim(),
                Descripcion = descripcion,
                TipoDocumento = categoria,
                EstadoDocumento = "Activo",
                Version = 1,
                FechaSubida = DateTime.Today,
                FechaActualizacion = DateTime.Today,
                IdUsuarioAutor = idUsuarioAutor,
                NombreArchivo = nombreArchivoConsecutivo,
                ArchivoUrl = rutaWeb,
                EsPublico = false,
                IdCurso = idCurso,
                IdMateria = idMateria
            };

            _context.Documentos.Add(nuevoDocumento);
            await _context.SaveChangesAsync();

            // 7. Registrar en tabla DOCUMENTO_VERSION (Versión inicial 1)
            var versionInicial = new DocumentoVersion
            {
                IdDocumento = nuevoDocumento.IdDocumento,
                NumeroVersion = 1,
                ArchivoUrl = rutaWeb,
                NombreArchivo = nombreArchivoConsecutivo,
                FechaVersion = DateTime.Today,
                IdUsuario = idUsuarioAutor,
                NotasVersion = "Carga inicial consecutiva"
            };
            _context.DocumentoVersiones.Add(versionInicial);

            // 8. Registrar etiqueta funcional en DOCUMENTO_ETIQUETA
            string etiquetaBase = !string.IsNullOrWhiteSpace(etiquetaAdicional) 
                ? etiquetaAdicional 
                : Capitalizar(categoria);

            _context.DocumentoEtiquetas.Add(new DocumentoEtiqueta
            {
                IdDocumento = nuevoDocumento.IdDocumento,
                Etiqueta = etiquetaBase.Length > 80 ? etiquetaBase.Substring(0, 80) : etiquetaBase
            });

            // 9. REQUISITO LEGAL: Si contiene datos de menores de edad (foto carnet, paz y salvo, etc.),
            // registrar la etiqueta obligatoria de "Estado Crítico" conforme al Dec. 1074/2015
            if (esCriticoMenor || EsNombreTipoCritico(etiquetaAdicional) || EsNombreTipoCritico(titulo))
            {
                _context.DocumentoEtiquetas.Add(new DocumentoEtiqueta
                {
                    IdDocumento = nuevoDocumento.IdDocumento,
                    Etiqueta = ETIQUETA_CRITICA_MENOR.Length > 80 
                        ? ETIQUETA_CRITICA_MENOR.Substring(0, 80) 
                        : ETIQUETA_CRITICA_MENOR
                });
            }

            await _context.SaveChangesAsync();

            return (rutaWeb, nuevoDocumento.IdDocumento, nombreArchivoConsecutivo);
        }

        /// <summary>
        /// Sobrecarga simplificada de GuardarArchivoAsync para controladores y servicios del sistema.
        /// </summary>
        public async Task<(string RutaRelativa, string NombreOriginal, int IdDocumento)> GuardarArchivoAsync(
            IFormFile archivo,
            string subcarpeta,
            int idUsuario,
            bool esCriticoMenor = false,
            string? tipoEtiqueta = null,
            string? titulo = null,
            int? idCurso = null,
            int? idMateria = null)
        {
            var res = await GuardarArchivoConsecutivoAsync(
                archivo: archivo,
                categoria: subcarpeta,
                idUsuarioAutor: idUsuario,
                titulo: titulo ?? archivo.FileName,
                descripcion: null,
                esCriticoMenor: esCriticoMenor,
                etiquetaAdicional: tipoEtiqueta,
                idCurso: idCurso,
                idMateria: idMateria
            );

            return (res.RutaWeb, archivo.FileName, res.IdDocumento);
        }

        /// <summary>
        /// Comprueba si un documento posee la etiqueta de Estado Crítico de Menor de Edad
        /// para exigir la ventana de aceptación conforme al Art. 2.2.2.25.2.9 del Decreto 1074 de 2015.
        /// </summary>
        public async Task<bool> EsDocumentoCriticoMenorAsync(int idDocumento)
        {
            return await _context.DocumentoEtiquetas
                .AnyAsync(e => e.IdDocumento == idDocumento && e.Etiqueta.Contains("Estado Crítico"));
        }

        private static bool EsNombreTipoCritico(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            var t = texto.ToLowerInvariant();
            return t.Contains("foto") || 
                   t.Contains("carnet") || 
                   t.Contains("paz y salvo") || 
                   t.Contains("médic") || 
                   t.Contains("medic") || 
                   t.Contains("psicol") || 
                   t.Contains("observad") ||
                   t.Contains("menor");
        }

        private static string Capitalizar(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return str;
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
}
