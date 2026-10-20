using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.Controllers
{
    [RequierePermiso(Permisos.ParticiparPQR)]
    public class PqrController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PqrController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Pqr o /Pqr/Index
        public async Task<IActionResult> Index(string? busqueda, string? tipo, string? estado)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string rol = Permisos.RolActual(HttpContext);
            bool esGestor = Permisos.TienePermiso(HttpContext, Permisos.GestionarPQR);

            var query = _context.PQRs.Where(p => p.Activo).AsQueryable();

            // Los usuarios normales solo pueden ver sus propias solicitudes radicadas
            if (!esGestor)
            {
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Su sesión no es válida. Por favor ingrese de nuevo.";
                    return RedirectToAction("Index", "Login");
                }
                query = query.Where(p => p.IdUsuario == userId.Value);
            }

            // Filtro por término de búsqueda (asunto, código de radicado o descripción)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var b = busqueda.Trim().ToLower();
                query = query.Where(p => p.CodigoRadicado.ToLower().Contains(b) ||
                                         p.Asunto.ToLower().Contains(b) ||
                                         p.Descripcion.ToLower().Contains(b) ||
                                         (esGestor && p.NombreUsuario.ToLower().Contains(b)));
            }

            // Filtro por tipo (Petición, Queja, Reclamo)
            if (!string.IsNullOrWhiteSpace(tipo) && tipo.ToLower() != "todos")
            {
                query = query.Where(p => p.Tipo.ToLower() == tipo.ToLower());
            }

            // Filtro por estado (Recibida, En revisión, Respondida, Cerrada)
            if (!string.IsNullOrWhiteSpace(estado) && estado.ToLower() != "todos")
            {
                query = query.Where(p => p.Estado.ToLower() == estado.ToLower());
            }

            var lista = await query
                .OrderByDescending(p => p.FechaRegistro)
                .ToListAsync();

            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.Tipo = tipo ?? "todos";
            ViewBag.Estado = estado ?? "todos";
            ViewBag.EsGestor = esGestor;
            ViewBag.RolActual = rol;
            ViewBag.Total = lista.Count;
            ViewBag.TotalRecibidas = lista.Count(p => p.Estado == "Recibida");
            ViewBag.TotalEnRevision = lista.Count(p => p.Estado == "En revisión");
            ViewBag.TotalRespondidas = lista.Count(p => p.Estado == "Respondida");
            ViewBag.TotalCerradas = lista.Count(p => p.Estado == "Cerrada");

            return View(lista);
        }

        // GET: /Pqr/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var pqr = await _context.PQRs.FirstOrDefaultAsync(p => p.IdPqr == id && p.Activo);
            if (pqr == null)
            {
                TempData["Error"] = "La solicitud solicitada no fue encontrada.";
                return RedirectToAction(nameof(Index));
            }

            int? userId = HttpContext.Session.GetInt32("UserId");
            bool esGestor = Permisos.TienePermiso(HttpContext, Permisos.GestionarPQR);

            // Validación de permisos de privacidad
            if (!esGestor && pqr.IdUsuario != userId)
            {
                TempData["Error"] = "No tienes autorización para consultar esta solicitud privada.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.EsGestor = esGestor;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    pqr = new
                    {
                        pqr.IdPqr,
                        pqr.CodigoRadicado,
                        pqr.Tipo,
                        pqr.Asunto,
                        pqr.Descripcion,
                        FechaRegistro = pqr.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
                        pqr.Estado,
                        pqr.NombreUsuario,
                        pqr.RolUsuario,
                        pqr.Respuesta,
                        FechaRespuesta = pqr.FechaRespuesta?.ToString("dd/MM/yyyy HH:mm"),
                        pqr.RespondidoPor
                    }
                });
            }

            return View(pqr);
        }

        // GET: /Pqr/Crear
        public IActionResult Crear()
        {
            return View(new PqrCrearViewModel());
        }

        // POST: /Pqr/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PqrCrearViewModel modelo)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string nombreUsuario = HttpContext.Session.GetString("NombreUsuario") ?? "Usuario PAE";
            string rolUsuario = Permisos.RolActual(HttpContext);

            if (!userId.HasValue)
            {
                TempData["Error"] = "Su sesión ha expirado. Por favor ingrese de nuevo.";
                return RedirectToAction("Index", "Login");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor verifique los campos del formulario e intente nuevamente.";
                return View(modelo);
            }

            try
            {
                // Obtener datos del usuario para asignar correo
                var usuario = await _context.Usuarios.FindAsync(userId.Value);
                string? correo = usuario?.CORREO_ELECTRONICO;

                // Generar radicado único y correlativo
                int correlativo = await _context.PQRs.CountAsync() + 1;
                string radicado = $"PQR-{DateTime.Now.Year}-{correlativo:D4}";

                // Evitar colisión accidental de radicado
                while (await _context.PQRs.AnyAsync(p => p.CodigoRadicado == radicado))
                {
                    correlativo++;
                    radicado = $"PQR-{DateTime.Now.Year}-{correlativo:D4}";
                }

                var pqr = new Pqr
                {
                    CodigoRadicado = radicado,
                    IdUsuario = userId.Value,
                    NombreUsuario = !string.IsNullOrWhiteSpace(usuario?.NOMBRES) ? $"{usuario.NOMBRES} {usuario.APELLIDOS}".Trim() : nombreUsuario,
                    CorreoUsuario = correo,
                    RolUsuario = rolUsuario,
                    Tipo = modelo.Tipo,
                    Asunto = modelo.Asunto.Trim(),
                    Descripcion = modelo.Descripcion.Trim(),
                    FechaRegistro = DateTime.Now,
                    Estado = "Recibida",
                    Activo = true
                };

                _context.PQRs.Add(pqr);
                await _context.SaveChangesAsync();

                TempData["Exito"] = $"Su solicitud fue radicada con éxito con el código {pqr.CodigoRadicado}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Registro interno sin exponer datos técnicos al usuario
                System.Diagnostics.Debug.WriteLine($"[Error Pqr/Crear]: {ex.Message}");
                TempData["Error"] = "No fue posible registrar la solicitud en este momento. Inténtelo nuevamente.";
                return View(modelo);
            }
        }

        // POST: /Pqr/Responder
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.GestionarPQR)]
        public async Task<IActionResult> Responder(PqrResponderViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "La respuesta no cumple con los requisitos mínimos de validación.";
                return RedirectToAction(nameof(Index));
            }

            var pqr = await _context.PQRs.FirstOrDefaultAsync(p => p.IdPqr == modelo.IdPqr && p.Activo);
            if (pqr == null)
            {
                TempData["Error"] = "La solicitud que intenta responder no existe.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                string respondidoPor = HttpContext.Session.GetString("NombreUsuario") ?? "Administración PAE";
                pqr.Respuesta = modelo.Respuesta.Trim();
                pqr.Estado = string.IsNullOrWhiteSpace(modelo.Estado) ? "Respondida" : modelo.Estado;
                pqr.FechaRespuesta = DateTime.Now;
                pqr.RespondidoPor = respondidoPor;

                await _context.SaveChangesAsync();

                TempData["Exito"] = $"La respuesta a la solicitud {pqr.CodigoRadicado} fue registrada correctamente.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error Pqr/Responder]: {ex.Message}");
                TempData["Error"] = "Ocurrió un error al guardar la respuesta. Inténtelo más tarde.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Pqr/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.GestionarPQR)]
        public async Task<IActionResult> CambiarEstado(PqrCambiarEstadoViewModel modelo)
        {
            var pqr = await _context.PQRs.FirstOrDefaultAsync(p => p.IdPqr == modelo.IdPqr && p.Activo);
            if (pqr == null)
            {
                return Json(new { success = false, message = "La solicitud no fue encontrada." });
            }

            var estadosValidos = new[] { "Recibida", "En revisión", "Respondida", "Cerrada" };
            if (!estadosValidos.Contains(modelo.Estado))
            {
                return Json(new { success = false, message = "El estado indicado no es válido." });
            }

            pqr.Estado = modelo.Estado;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"El estado se actualizó a '{modelo.Estado}' correctamente." });
        }
    }
}
