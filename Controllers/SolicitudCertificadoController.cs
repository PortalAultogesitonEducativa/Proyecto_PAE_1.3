using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.Controllers
{
    /// <summary>
    /// Solicitudes de certificados con plazo de entrega.
    /// - Acudiente: solicita para sus hijos y consulta el estado.
    /// - Coordinador y admin: registran a nombre de cualquier estudiante, gestionan y entregan.
    /// - Estudiante: solo consulta el estado de sus solicitudes.
    /// </summary>
    [RequierePermiso(Permisos.VerSolicitudesCertificado)]
    public class SolicitudCertificadoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CertificadoService _certificadoService;
        private readonly SolicitudCertificadoService _solicitudService;

        public SolicitudCertificadoController(
            ApplicationDbContext context,
            CertificadoService certificadoService,
            SolicitudCertificadoService solicitudService)
        {
            _context = context;
            _certificadoService = certificadoService;
            _solicitudService = solicitudService;
        }

        // ──────────────────────────────────────────────────────────────
        // CONSULTA
        // ──────────────────────────────────────────────────────────────

        // GET: /SolicitudCertificado
        public async Task<IActionResult> Index(string? busqueda, string? estado, string? plazo)
        {
            _context.AsegurarEsquemaSolicitudesCertificado();

            bool esGestor = Permisos.TienePermiso(HttpContext, Permisos.GestionarSolicitudesCertificado);
            var solicitudes = await Visibles().AsNoTracking().ToListAsync();

            var ahora = DateTime.Now;
            var items = solicitudes
                .Select(s => new SolicitudCertificadoItem { Solicitud = s, Plazo = PlazoCertificado.Evaluar(s, ahora) })
                .ToList();

            // Contadores sobre todo lo visible para el usuario (antes de aplicar filtros)
            ViewBag.Total = items.Count;
            ViewBag.TotalAbiertas = items.Count(i => PlazoCertificado.EstaAbierta(i.Solicitud));
            ViewBag.TotalVencidas = items.Count(i => i.Plazo.Clave == "vencida");
            ViewBag.TotalPorVencer = items.Count(i => i.Plazo.Clave == "por-vencer");
            ViewBag.TotalEntregadas = items.Count(i => i.Solicitud.Estado == EstadoSolicitudCertificado.Entregada);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var b = busqueda.Trim().ToLower();
                items = items.Where(i =>
                    i.Solicitud.CodigoRadicado.ToLower().Contains(b) ||
                    i.Solicitud.NombreEstudiante.ToLower().Contains(b) ||
                    (esGestor && i.Solicitud.NombreSolicitante.ToLower().Contains(b))).ToList();
            }

            if (!string.IsNullOrWhiteSpace(estado) && estado.ToLower() != "todos")
                items = items.Where(i => i.Solicitud.Estado.ToLower() == estado.ToLower()).ToList();

            if (!string.IsNullOrWhiteSpace(plazo) && plazo.ToLower() != "todos")
                items = items.Where(i => i.Plazo.Clave == plazo.ToLower()).ToList();

            // Primero lo más urgente (vencidas, por vencer, en plazo) y después lo ya cerrado
            items = items
                .OrderBy(i => i.Plazo.Prioridad)
                .ThenBy(i => i.Plazo.Prioridad < 3 ? i.Solicitud.FechaLimite : DateTime.MaxValue)
                .ThenByDescending(i => i.Solicitud.FechaSolicitud)
                .ToList();

            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.Estado = estado ?? "todos";
            ViewBag.Plazo = plazo ?? "todos";
            ViewBag.EsGestor = esGestor;
            ViewBag.PuedeSolicitar = Permisos.TienePermiso(HttpContext, Permisos.SolicitarCertificado);
            ViewBag.RolActual = Permisos.RolActual(HttpContext);

            return View(items);
        }

        // GET: /SolicitudCertificado/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            _context.AsegurarEsquemaSolicitudesCertificado();

            var solicitud = await Visibles().AsNoTracking().FirstOrDefaultAsync(s => s.IdSolicitud == id);
            if (solicitud == null)
            {
                TempData["Error"] = "La solicitud no fue encontrada o no tienes autorización para consultarla.";
                return RedirectToAction(nameof(Index));
            }

            Certificado? certificado = null;
            if (solicitud.IdCertificado.HasValue)
                certificado = await _context.Certificados.AsNoTracking().FirstOrDefaultAsync(c => c.IdCertificado == solicitud.IdCertificado.Value);

            var modelo = new SolicitudCertificadoDetalleViewModel
            {
                Solicitud = solicitud,
                Plazo = PlazoCertificado.Evaluar(solicitud),
                Certificado = certificado,
                EsGestor = Permisos.TienePermiso(HttpContext, Permisos.GestionarSolicitudesCertificado)
            };

            ViewBag.RolActual = Permisos.RolActual(HttpContext);
            return View(modelo);
        }

        // ──────────────────────────────────────────────────────────────
        // REGISTRO DE SOLICITUDES
        // ──────────────────────────────────────────────────────────────

        // GET: /SolicitudCertificado/Crear
        [RequierePermiso(Permisos.SolicitarCertificado)]
        public IActionResult Crear()
        {
            var modelo = PrepararFormulario(new SolicitudCertificadoCrearViewModel());
            ViewBag.RolActual = Permisos.RolActual(HttpContext);
            return View(modelo);
        }

        // POST: /SolicitudCertificado/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.SolicitarCertificado)]
        public async Task<IActionResult> Crear(SolicitudCertificadoCrearViewModel modelo)
        {
            _context.AsegurarEsquemaSolicitudesCertificado();

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["Error"] = "Tu sesión ha expirado. Por favor ingresa de nuevo.";
                return RedirectToAction("Index", "Login");
            }

            bool esGestor = Permisos.TienePermiso(HttpContext, Permisos.GestionarSolicitudesCertificado);
            string rol = Permisos.RolActual(HttpContext);

            // 1. Tipo de certificado
            if (modelo.TipoCertificado != EstadoSolicitudCertificado.TipoMatricula &&
                modelo.TipoCertificado != EstadoSolicitudCertificado.TipoNotas)
            {
                ModelState.AddModelError("", "Selecciona un tipo de certificado válido.");
            }

            // 2. Estudiante al que pertenece el certificado
            Usuario? estudiante = null;
            if (esGestor)
            {
                string documento = (modelo.NumeroDocumento ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(documento))
                {
                    ModelState.AddModelError("", "Ingresa el número de documento del estudiante.");
                }
                else
                {
                    estudiante = await _context.Usuarios.FirstOrDefaultAsync(u =>
                        u.NUM_DOCUMENTO == documento &&
                        u.ROL != null && u.ROL.Trim().ToLower() == "estudiante");

                    if (estudiante == null)
                        ModelState.AddModelError("", $"No se encontró ningún estudiante con el número de documento '{documento}'.");
                }
            }
            else
            {
                // Acudiente: solo puede solicitar para sus hijos vinculados
                var hijos = AccesoEstudiantes.IdsHijos(_context, userId.Value);
                if (!modelo.IdEstudiante.HasValue || !hijos.Contains(modelo.IdEstudiante.Value))
                {
                    ModelState.AddModelError("", "Selecciona uno de tus hijos para solicitar el certificado.");
                }
                else
                {
                    estudiante = await _context.Usuarios.FindAsync(modelo.IdEstudiante.Value);
                }
            }

            // 3. Fechas
            var ahora = DateTime.Now;
            DateTime fechaSolicitud = ahora;
            DateTime fechaLimite = PlazoCertificado.SumarDiasHabiles(ahora, PlazoCertificado.DiasHabilesPlazo);

            if (esGestor)
            {
                if (modelo.FechaSolicitud.HasValue)
                {
                    var f = modelo.FechaSolicitud.Value.Date;
                    if (f > ahora.Date)
                        ModelState.AddModelError("", "La fecha de solicitud no puede ser futura.");
                    else
                        fechaSolicitud = f == ahora.Date ? ahora : f;
                }

                fechaLimite = modelo.FechaLimite.HasValue
                    ? modelo.FechaLimite.Value.Date
                    : PlazoCertificado.SumarDiasHabiles(fechaSolicitud, PlazoCertificado.DiasHabilesPlazo);

                if (fechaLimite < fechaSolicitud.Date)
                    ModelState.AddModelError("", "La fecha límite no puede ser anterior a la fecha de solicitud.");
            }

            // 4. Evitar duplicados: una solicitud abierta del mismo tipo para el mismo estudiante
            if (estudiante != null && ModelState.IsValid)
            {
                var abierta = await _context.SolicitudesCertificado.FirstOrDefaultAsync(s =>
                    s.Activo &&
                    s.IdEstudiante == estudiante.ID_Usuario &&
                    s.TipoCertificado == modelo.TipoCertificado &&
                    (s.Estado == EstadoSolicitudCertificado.Pendiente || s.Estado == EstadoSolicitudCertificado.EnProceso));

                if (abierta != null)
                {
                    ModelState.AddModelError("", $"Ya existe una solicitud abierta de este certificado para el estudiante (radicado {abierta.CodigoRadicado}).");
                }
            }

            if (!ModelState.IsValid || estudiante == null)
            {
                PrepararFormulario(modelo);
                ViewBag.RolActual = rol;
                return View(modelo);
            }

            try
            {
                var solicitante = await _context.Usuarios.FindAsync(userId.Value);
                string nombreSolicitante = solicitante != null
                    ? $"{solicitante.NOMBRES} {solicitante.APELLIDOS}".Trim()
                    : (HttpContext.Session.GetString("NombreUsuario") ?? "Usuario PAE");

                var solicitud = new SolicitudCertificado
                {
                    CodigoRadicado = await _solicitudService.SiguienteRadicadoAsync(),
                    IdEstudiante = estudiante.ID_Usuario,
                    NombreEstudiante = $"{estudiante.NOMBRES} {estudiante.APELLIDOS}".Trim(),
                    TipoCertificado = modelo.TipoCertificado,
                    ObservacionesSolicitante = string.IsNullOrWhiteSpace(modelo.Observaciones) ? null : modelo.Observaciones.Trim(),
                    IdSolicitante = userId.Value,
                    NombreSolicitante = nombreSolicitante,
                    RolSolicitante = rol,
                    FechaSolicitud = fechaSolicitud,
                    FechaLimite = fechaLimite,
                    Estado = EstadoSolicitudCertificado.Pendiente,
                    Activo = true
                };

                _context.SolicitudesCertificado.Add(solicitud);
                await _context.SaveChangesAsync();

                TempData["Exito"] = $"La solicitud fue registrada con el radicado {solicitud.CodigoRadicado}. Fecha límite de entrega: {solicitud.FechaLimite:dd/MM/yyyy}.";
                return RedirectToAction(nameof(Detalle), new { id = solicitud.IdSolicitud });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error SolicitudCertificado/Crear]: {ex.Message}");
                ModelState.AddModelError("", "No fue posible registrar la solicitud en este momento. Inténtalo nuevamente.");
                PrepararFormulario(modelo);
                ViewBag.RolActual = rol;
                return View(modelo);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // GESTIÓN (coordinador y admin)
        // ──────────────────────────────────────────────────────────────

        // POST: /SolicitudCertificado/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.GestionarSolicitudesCertificado)]
        public async Task<IActionResult> CambiarEstado(int id, string estado, string? observaciones)
        {
            var solicitud = await ObtenerAbiertaAsync(id);
            if (solicitud == null)
                return RedirectToAction(nameof(Index));

            if (estado == EstadoSolicitudCertificado.EnProceso)
            {
                if (solicitud.Estado != EstadoSolicitudCertificado.Pendiente)
                {
                    TempData["Error"] = "Solo una solicitud pendiente puede pasar a \"En proceso\".";
                    return RedirectToAction(nameof(Detalle), new { id });
                }

                solicitud.Estado = EstadoSolicitudCertificado.EnProceso;
                AgregarBitacora(solicitud, "Trámite iniciado", observaciones);
                TempData["Exito"] = $"La solicitud {solicitud.CodigoRadicado} quedó en proceso.";
            }
            else if (estado == EstadoSolicitudCertificado.Cancelada)
            {
                if (string.IsNullOrWhiteSpace(observaciones) || observaciones.Trim().Length < 5)
                {
                    TempData["Error"] = "Indica el motivo de la cancelación (mínimo 5 caracteres).";
                    return RedirectToAction(nameof(Detalle), new { id });
                }

                solicitud.Estado = EstadoSolicitudCertificado.Cancelada;
                AgregarBitacora(solicitud, "Solicitud cancelada", observaciones);
                TempData["Exito"] = $"La solicitud {solicitud.CodigoRadicado} fue cancelada.";
            }
            else
            {
                TempData["Error"] = "El estado solicitado no es válido.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // POST: /SolicitudCertificado/AjustarPlazo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.GestionarSolicitudesCertificado)]
        public async Task<IActionResult> AjustarPlazo(int id, DateTime? fechaLimite, string? motivo)
        {
            var solicitud = await ObtenerAbiertaAsync(id);
            if (solicitud == null)
                return RedirectToAction(nameof(Index));

            if (!fechaLimite.HasValue)
            {
                TempData["Error"] = "Indica la nueva fecha límite.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            if (fechaLimite.Value.Date < solicitud.FechaSolicitud.Date)
            {
                TempData["Error"] = "La fecha límite no puede ser anterior a la fecha de solicitud.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5)
            {
                TempData["Error"] = "Indica el motivo del ajuste de plazo (mínimo 5 caracteres).";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            string anterior = solicitud.FechaLimite.ToString("dd/MM/yyyy");
            solicitud.FechaLimite = fechaLimite.Value.Date;
            AgregarBitacora(solicitud, $"Plazo ajustado de {anterior} a {solicitud.FechaLimite:dd/MM/yyyy}", motivo);

            await _context.SaveChangesAsync();
            TempData["Exito"] = $"El plazo de la solicitud {solicitud.CodigoRadicado} ahora vence el {solicitud.FechaLimite:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // POST: /SolicitudCertificado/EntregarMatricula
        // Genera la constancia de matrícula con el servicio de certificados y cierra la solicitud en un solo paso.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.GestionarSolicitudesCertificado)]
        [RequierePermiso(Permisos.GenerarCertificadoMatricula)]
        public async Task<IActionResult> EntregarMatricula(int id)
        {
            var solicitud = await ObtenerAbiertaAsync(id);
            if (solicitud == null)
                return RedirectToAction(nameof(Index));

            if (solicitud.TipoCertificado != EstadoSolicitudCertificado.TipoMatricula)
            {
                TempData["Error"] = "Los certificados de notas se entregan desde la pantalla de generación de certificados.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            int idGestor = HttpContext.Session.GetInt32("UserId") ?? 1;

            try
            {
                var certificado = await _certificadoService.GenerarCertificadoMatriculaAsync(solicitud.IdEstudiante, idGestor);
                var resultado = await _solicitudService.MarcarEntregadaAsync(
                    solicitud.IdSolicitud, certificado.IdCertificado, DateTime.Now, idGestor, "Constancia de matrícula generada desde el sistema.");

                if (resultado.Exito)
                    TempData["Exito"] = $"{resultado.Mensaje} El certificado quedó generado y enlazado.";
                else
                    TempData["Error"] = resultado.Mensaje;
            }
            catch (InvalidOperationException ex)
            {
                // Por ejemplo, el estudiante no tiene una matrícula vigente
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }

        // POST: /SolicitudCertificado/RegistrarEntrega
        // Para certificados que se entregaron por otra vía (por ejemplo, generados desde el reporte grupal).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.GestionarSolicitudesCertificado)]
        public async Task<IActionResult> RegistrarEntrega(int id, DateTime? fechaEntrega, string? observaciones)
        {
            var solicitud = await ObtenerAbiertaAsync(id);
            if (solicitud == null)
                return RedirectToAction(nameof(Index));

            var ahora = DateTime.Now;
            var fecha = fechaEntrega.HasValue ? fechaEntrega.Value.Date : ahora.Date;

            if (fecha > ahora.Date)
            {
                TempData["Error"] = "La fecha de entrega no puede ser futura.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            if (fecha < solicitud.FechaSolicitud.Date)
            {
                TempData["Error"] = "La fecha de entrega no puede ser anterior a la fecha de solicitud.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            int idGestor = HttpContext.Session.GetInt32("UserId") ?? 1;
            var fechaConHora = fecha == ahora.Date ? ahora : fecha;

            var resultado = await _solicitudService.MarcarEntregadaAsync(solicitud.IdSolicitud, null, fechaConHora, idGestor, observaciones);

            if (resultado.Exito)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Detalle), new { id });
        }

        // ──────────────────────────────────────────────────────────────
        // APOYO
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Solicitudes que el usuario en sesión puede ver: el gestor ve todas, el acudiente las de sus hijos
        /// y las que él registró, y el estudiante solo las suyas.
        /// </summary>
        private IQueryable<SolicitudCertificado> Visibles()
        {
            var consulta = _context.SolicitudesCertificado.Where(s => s.Activo);

            if (Permisos.TienePermiso(HttpContext, Permisos.GestionarSolicitudesCertificado))
                return consulta;

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return consulta.Where(s => false);

            if (Permisos.RolActual(HttpContext) == "acudiente")
            {
                var hijos = AccesoEstudiantes.IdsHijos(_context, userId.Value);
                return consulta.Where(s => hijos.Contains(s.IdEstudiante) || s.IdSolicitante == userId.Value);
            }

            return consulta.Where(s => s.IdEstudiante == userId.Value);
        }

        private async Task<SolicitudCertificado?> ObtenerAbiertaAsync(int id)
        {
            _context.AsegurarEsquemaSolicitudesCertificado();

            var solicitud = await _context.SolicitudesCertificado.FirstOrDefaultAsync(s => s.IdSolicitud == id && s.Activo);
            if (solicitud == null)
            {
                TempData["Error"] = "La solicitud no fue encontrada.";
                return null;
            }

            if (!PlazoCertificado.EstaAbierta(solicitud))
            {
                TempData["Error"] = $"La solicitud {solicitud.CodigoRadicado} ya está {solicitud.Estado.ToLower()} y no admite cambios.";
                return null;
            }

            return solicitud;
        }

        private SolicitudCertificadoCrearViewModel PrepararFormulario(SolicitudCertificadoCrearViewModel modelo)
        {
            modelo.EsGestor = Permisos.TienePermiso(HttpContext, Permisos.GestionarSolicitudesCertificado);

            if (modelo.EsGestor)
            {
                var hoy = DateTime.Now.Date;
                modelo.FechaSolicitud ??= hoy;
                modelo.FechaLimite ??= PlazoCertificado.SumarDiasHabiles(modelo.FechaSolicitud.Value, PlazoCertificado.DiasHabilesPlazo);
                modelo.Hijos = new List<OpcionEstudiante>();
            }
            else
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                var idsHijos = userId.HasValue ? AccesoEstudiantes.IdsHijos(_context, userId.Value) : new List<int>();

                modelo.Hijos = _context.Usuarios
                    .Where(u => idsHijos.Contains(u.ID_Usuario))
                    .AsEnumerable()
                    .Select(u => new OpcionEstudiante
                    {
                        Id = u.ID_Usuario,
                        Nombre = $"{u.NOMBRES} {u.APELLIDOS}".Trim()
                    })
                    .OrderBy(o => o.Nombre)
                    .ToList();
            }

            return modelo;
        }

        private void AgregarBitacora(SolicitudCertificado solicitud, string accion, string? detalle)
        {
            int? idGestor = HttpContext.Session.GetInt32("UserId");
            string nombreGestor = HttpContext.Session.GetString("NombreUsuario") ?? "Usuario PAE";

            solicitud.IdUsuarioGestor = idGestor;
            solicitud.NombreGestor = nombreGestor;

            string linea = $"[{DateTime.Now:dd/MM/yyyy HH:mm}] {accion} por {nombreGestor}" +
                           (string.IsNullOrWhiteSpace(detalle) ? "." : $": {detalle.Trim()}");

            solicitud.ObservacionesGestion = string.IsNullOrWhiteSpace(solicitud.ObservacionesGestion)
                ? linea
                : solicitud.ObservacionesGestion + Environment.NewLine + linea;
        }
    }
}