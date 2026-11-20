using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.Controllers
{
    /// <summary>
    /// Cambio de correo electrónico con confirmación por enlace (RF-03.2).
    /// Solicitar y Cancelar exigen sesión; Confirmar es público porque el enlace llega al correo nuevo
    /// y se abre desde el correo (el token es la prueba de que el usuario controla ese buzón).
    /// </summary>
    public class CorreoController : Controller
    {
        private readonly CambioCorreoService _servicio;
        private readonly IConfiguration _config;

        public CorreoController(CambioCorreoService servicio, IConfiguration config)
        {
            _servicio = servicio;
            _config = config;
        }

        // POST: /Correo/Solicitar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Solicitar(string? correoNuevo, string? passwordActual)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var resultado = await _servicio.SolicitarAsync(userId.Value, correoNuevo, passwordActual, ConstruirEnlace);

            // El mensaje de éxito es fijo (el layout lo muestra sin escapar), nunca se mezcla con datos del usuario
            if (resultado.Exito)
                TempData["Mensaje"] = resultado.Mensaje;
            else
                TempData["ErrorCorreo"] = resultado.Mensaje;

            return RedirectToAction("EditarPerfil", "Home");
        }

        // POST: /Correo/Cancelar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Login");

            await _servicio.CancelarPendientesAsync(userId.Value);
            TempData["Mensaje"] = "El cambio de correo pendiente fue cancelado.";

            return RedirectToAction("EditarPerfil", "Home");
        }

        // GET: /Correo/Confirmar?token=...
        [HttpGet]
        public async Task<IActionResult> Confirmar(string? token)
        {
            var resultado = await _servicio.ConfirmarAsync(token);
            return View(resultado);
        }

        /// <summary>
        /// Dirección completa del enlace. Si existe App:BaseUrl en appsettings se usa esa (más seguro);
        /// si no, se arma con el dominio de la petición actual.
        /// </summary>
        private string ConstruirEnlace(string token)
        {
            string ruta = Url.Action("Confirmar", "Correo", new { token })
                          ?? $"/Correo/Confirmar?token={Uri.EscapeDataString(token)}";

            string? baseUrl = _config["App:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}";

            return baseUrl.TrimEnd('/') + ruta;
        }
    }
}
