using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;
using QRCoder;
using Rotativa.AspNetCore;

namespace ProyectoPAE.Controllers
{
    public class CertificadoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CertificadoService _certificadoService;

        public CertificadoController(ApplicationDbContext context, CertificadoService certificadoService)
        {
            _context = context;
            _certificadoService = certificadoService;
        }

        // ==========================================================
        // GENERACIÓN (requiere sesión de administrador/docente)
        // TODO: agrega aquí tu filtro de autenticación/rol existente
        // (el mismo que usan AdminController / DocenteController)
        // ==========================================================

        // GET: /Certificado/Generar/5
        [HttpGet]
        public async Task<IActionResult> Generar(int idEstudiante)
        {
            var estudiante = await _context.Usuarios.FindAsync(idEstudiante);
            if (estudiante == null) return NotFound();

            var calificaciones = await _context.Calificaciones
                .Where(c => c.ID_Estudiante == idEstudiante)
                .ToListAsync();

            ViewBag.Estudiante = estudiante;
            return View(calificaciones);
        }

        // POST: /Certificado/Generar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generar(int idEstudiante, List<int> idsCalificacion, string tipoCertificado)
        {
            if (idsCalificacion == null || idsCalificacion.Count == 0)
            {
                ModelState.AddModelError("", "Selecciona al menos una calificación para certificar.");
                return RedirectToAction(nameof(Generar), new { idEstudiante });
            }

            int idUsuarioEmisor = HttpContext.Session.GetInt32("UserId") ?? 1;

            var certificado = await _certificadoService.GenerarCertificadoAsync(
                idEstudiante, tipoCertificado ?? "Reporte de calificaciones", idsCalificacion, idUsuarioEmisor);

            return RedirectToAction(nameof(Confirmacion), new { id = certificado.IdCertificado });
        }

        // GET: /Certificado/GenerarMatricula?numeroDocumento=1122334455
        [HttpGet]
        public async Task<IActionResult> GenerarMatricula(string numeroDocumento)
        {
            if (string.IsNullOrWhiteSpace(numeroDocumento))
            {
                TempData["ErrorMatricula"] = "Debes ingresar el número de documento del estudiante.";
                return RedirectToAction("Dashboard", "Home", new { rol = "admin" });
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.NUM_DOCUMENTO == numeroDocumento.Trim() &&
                u.ROL != null && u.ROL.Trim().ToLower() == "estudiante");

            if (usuario == null)
            {
                TempData["ErrorMatricula"] = $"No se encontró ningún estudiante matriculado con el número de documento '{numeroDocumento}'.";
                return RedirectToAction("Dashboard", "Home", new { rol = "admin" });
            }

            var idUsuarioEmisor = HttpContext.Session.GetInt32("UserId") ?? 1;

            try
            {
                var certificado = await _certificadoService.GenerarCertificadoMatriculaAsync(usuario.ID_Usuario, idUsuarioEmisor);
                return RedirectToAction(nameof(Confirmacion), new { id = certificado.IdCertificado });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMatricula"] = ex.Message;
                return RedirectToAction("Dashboard", "Home", new { rol = "admin" });
            }
        }

        // GET: /Certificado/Confirmacion/3
        [HttpGet]
        public async Task<IActionResult> Confirmacion(int id)
        {
            var certificado = await _context.Certificados
                .Include(c => c.Estudiante)
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.IdCertificado == id);

            if (certificado == null) return NotFound();

            // Construye la URL pública de verificación y genera el QR como PNG base64
            string urlVerificacion = Url.Action(
                nameof(VerificarCodigo), "Certificado",
                new { codigo = certificado.CodigoVerificacion },
                protocol: Request.Scheme);

            ViewBag.QrBase64 = GenerarQRBase64(urlVerificacion);
            ViewBag.UrlVerificacion = urlVerificacion;

            // Nombre de quien emitió el certificado, para la línea de firma
            var emisor = await _context.Usuarios.FindAsync(certificado.IdUsuarioEmisor);
            ViewBag.NombreEmisor = emisor != null ? $"{emisor.NOMBRES} {emisor.APELLIDOS}" : "Rectoría";
            ViewBag.NombreColegio = await ObtenerNombreInstitucionAsync();

            return View(certificado);
        }

        // GET: /Certificado/DescargarPdf/3
        // Genera el mismo certificado que Confirmacion, pero como PDF descargable.
        [HttpGet]
        public async Task<IActionResult> DescargarPdf(int id)
        {
            var certificado = await _context.Certificados
                .Include(c => c.Estudiante)
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.IdCertificado == id);

            if (certificado == null) return NotFound();

            string urlVerificacion = Url.Action(
                nameof(VerificarCodigo), "Certificado",
                new { codigo = certificado.CodigoVerificacion },
                protocol: Request.Scheme);

            ViewBag.UrlVerificacion = urlVerificacion;
            ViewBag.QrBase64 = GenerarQRBase64(urlVerificacion);

            // Nombre de quien emitió el certificado, para la línea de firma del PDF
            var emisor = await _context.Usuarios.FindAsync(certificado.IdUsuarioEmisor);
            ViewBag.NombreEmisor = emisor != null ? $"{emisor.NOMBRES} {emisor.APELLIDOS}" : "Rectoría";
            ViewBag.NombreColegio = await ObtenerNombreInstitucionAsync();

            string nombreArchivo = certificado.TipoCertificado == "Matricula"
                ? $"ConstanciaMatricula_{certificado.IdCertificado}.pdf"
                : $"Certificado_{certificado.IdCertificado}.pdf";

            return new ViewAsPdf("DescargarPdf", certificado)
            {
                FileName = nombreArchivo,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                CustomSwitches = "--allow ./"
            };
        }

        /// <summary>
        /// Genera un código QR en memoria a partir de una URL y lo devuelve
        /// como cadena base64 lista para usar en un <img src="data:image/png;base64,...">.
        /// </summary>
        private string GenerarQRBase64(string contenido)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrBytes);
        }

        // ==========================================================
        // VERIFICACIÓN PÚBLICA (sin login — cualquiera con el código
        // debe poder consultarla)
        // ==========================================================

        // GET: /Certificado/Verificar
        [HttpGet]
        public IActionResult Verificar()
        {
            return View(new ResultadoVerificacion());
        }

        // GET: /Certificado/Verificar?codigo=PAE-8F2A91C4
        [HttpGet]
        public async Task<IActionResult> VerificarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                ModelState.AddModelError("", "Ingresa un código de verificación.");
                return View("Verificar", new ResultadoVerificacion());
            }

            var resultado = await _certificadoService.VerificarAsync(codigo.Trim());
            if (resultado.Certificado != null)
            {
                var emisor = await _context.Usuarios.FindAsync(resultado.Certificado.IdUsuarioEmisor);
                ViewBag.NombreEmisor = emisor != null ? $"{emisor.NOMBRES} {emisor.APELLIDOS}" : "Rectoría";
                ViewBag.NombreColegio = await ObtenerNombreInstitucionAsync();
            }
            return View("Verificar", resultado);
        }

        /// <summary>
        /// Lee el nombre de la institución desde GU_PARAMETRIZACION (aporte de Andrés).
        /// Pensado para el futuro multi-colegio: hoy es un único valor global, pero
        /// deja el punto de extensión listo para cuando cada colegio tenga su propia fila.
        /// </summary>
        private async Task<string> ObtenerNombreInstitucionAsync()
        {
            try
            {
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT valor FROM GU_PARAMETRIZACION WHERE clave = 'nombre_institucion' AND activo = 1";
                _context.Database.OpenConnection();
                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? "Colegio San Agustín";
            }
            catch
            {
                return "Colegio San Agustín";
            }
        }
    }
}
