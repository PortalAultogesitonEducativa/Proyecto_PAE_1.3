using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace ProyectoPAE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Esta es la página principal (Pública)
        public IActionResult Index()
        {
            var courses = new List<Course>
            {
                new Course {
                    Title = "Desarrollo Personal",
                    Description = "Cursos para mejorar tus habilidades personales y profesionales.",
                    IconClass = "bi-lightbulb",
                    LinkText = "Ver Curso >"
                },
                new Course {
                    Title = "Tecnología e Innovación",
                    Description = "Capacítate en las últimas tendencias tecnológicas.",
                    IconClass = "bi-laptop",
                    LinkText = "Ver Curso >"
                },
                new Course {
                    Title = "Negocios y Liderazgo",
                    Description = "Aprende a gestionar y liderar equipos eficientemente.",
                    IconClass = "bi-briefcase",
                    LinkText = "Ver Curso >"
                }
            };

            return View(courses);
        }

        // --- MÉTODO DASHBOARD ACTUALIZADO PARA PADRES ---
        // --- MÉTODO DASHBOARD ACTUALIZADO PARA SOPORTAR NAVEGACIÓN Y ROLES ---
        public IActionResult Dashboard(string rol = null)
        {
            var nombreUsuario = HttpContext.Session.GetString("NombreUsuario");
            var rolSesion = HttpContext.Session.GetString("UserRol");
            var userIdStr = HttpContext.Session.GetString("UserIdStr");

            // 2. Verificación de seguridad: si no hay sesión, redirecciona al Login
            if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(rolSesion))
            {
                return RedirectToAction("Index", "Login");
            }

            // 3. Determinamos el rol a mostrar y lo convertimos SIEMPRE a minúsculas (.ToLower())
            // Esto evita fallos si la base de datos o el parámetro trae mayúsculas como "Docente" o "ADMINISTRADOR"
            string rolActivo = !string.IsNullOrEmpty(rol) ? rol.Trim().ToLower() : rolSesion.Trim().ToLower();

            ViewBag.Nombre = nombreUsuario;
            ViewBag.Rol = rolActivo; // Se enviará limpio (ej: "docente", "admin", "acudiente")

            // 4. Lógica específica para el ACUDIENTE
            if (rolActivo == "acudiente")
            {
                // Usamos TryParse para evitar errores de formato (FormatException)
                if (int.TryParse(userIdStr, out int userId))
                {
                    var hijosIds = _context.ESTUDIANTE_PADRE
                                           .Where(ep => ep.ID_PADRE == userId)
                                           .Select(ep => ep.ID_ESTUDIANTE)
                                           .ToList();

                    var listaHijos = _context.Usuarios
                        .Where(u => hijosIds.Contains(u.ID_Usuario))
                        .AsEnumerable()
                        .Where(u => u.ROL != null && u.ROL.Trim().ToLower() == "estudiante")
                        .ToList();

                    if (listaHijos.Any())
                    {
                        int primerHijoId = listaHijos.First().ID_Usuario;

                        var promedio = _context.EVALUACIONES
                                               .Where(e => e.id_estudiante == primerHijoId)
                                               .Select(e => (double?)e.nota)
                                               .Average() ?? 0.0;

                        var fallas = _context.ASISTENCIAS
                                             .Count(a => a.id_estudiante == primerHijoId && a.estado == "Falla");

                        var listaCitaciones = _context.CITACIONES
                                                      .Where(c => c.id_estudiante == primerHijoId)
                                                      .OrderByDescending(c => c.fecha)
                                                      .ToList();

                        ViewBag.TotalCitaciones = listaCitaciones.Count;
                        ViewBag.PromedioRapido = promedio.ToString("0.0");
                        ViewBag.FallasRapidas = fallas;
                        ViewBag.ListaHijos = listaHijos;
                    }
                }
                else
                {
                    // Si el ID llega corrupto, lo mandamos a re-identificarse
                    return RedirectToAction("Index", "Login");
                }
            }

            // 5. Lógica específica para el ADMIN: datos reales de matrículas
            if (rolActivo == "admin")
            {
                var anioActual = DateTime.Now.Year;

                ViewBag.TotalMatriculadosActivos = _context.Matriculas.Count(m => m.estado != null && (m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo"));
                ViewBag.MatriculasEsteAnio = _context.Matriculas.Count(m => m.ano == anioActual);
                ViewBag.MatriculasFinalizadas = _context.Matriculas.Count(m => m.estado != null && m.estado.ToLower() == "finalizada");

                var listaMatriculas = (from m in _context.Matriculas
                                       join e in _context.ESTUDIANTE on m.id_estudiante equals e.id_estudiante
                                       orderby m.fecha_matricula descending
                                       select new MatriculaAdminItemViewModel
                                       {
                                           IdMatricula = m.id_matricula,
                                           IdEstudiante = e.id_estudiante,
                                           NombreEstudiante = e.nombre + " " + e.apellido,
                                           CodigoEstudiante = e.codigo_estudiante,
                                           Grado = m.id_curso,
                                           FechaMatricula = m.fecha_matricula,
                                           PeriodoAcademico = m.periodo_academico,
                                           Estado = m.estado
                                       })
                                       .Take(20)
                                       .ToList();

                ViewBag.ListaMatriculas = listaMatriculas;
            }

            // 6. Lógica específica para el ESTUDIANTE: Cargar el horario según su curso
            if (rolActivo == "estudiante")
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    // 1. Buscar la entidad Estudiante vinculada al id_usuario en sesión o por correo
                    var usuarioActual = _context.Usuarios.Find(userId);
                    var estudiante = _context.ESTUDIANTE
                        .FirstOrDefault(e => e.id_usuario == userId || (usuarioActual != null && e.email == usuarioActual.CORREO_ELECTRONICO));

                    if (estudiante != null)
                    {
                        // 2. Buscar la matrícula activa para conocer su id_curso
                        var matriculaActiva = _context.Matriculas
                            .FirstOrDefault(m => m.id_estudiante == estudiante.id_estudiante && m.estado != null && (m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo"));

                        if (matriculaActiva != null)
                        {
                            // 3. Obtener el horario registrado para ese curso en la tabla HORARIO
                            var horarioEstudiante = _context.HORARIOS
                                .Where(h => h.id_curso == matriculaActiva.id_curso)
                                .OrderBy(h => h.dia)
                                .ThenBy(h => h.hora_inicio)
                                .ToList();

                            ViewBag.CursoId = matriculaActiva.id_curso;
                            ViewBag.Horario = horarioEstudiante;
                        }
                        else
                        {
                            ViewBag.MensajeHorario = "No tienes una matrícula activa registrada.";
                        }
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Login");
                }
            }

            // 7. Cargar notificaciones activas — el admin NO las ve en el dashboard
            //    (él es el emisor; las gestiona en /Admin/Notificaciones)
            if (rolActivo != "admin")
            {
                string rolNotif = rolActivo;
                if (rolNotif == "profesor") rolNotif = "docente";

                var notificaciones = _context.Notificaciones
                    .Include(n => n.Emisor)
                    .Where(n => n.Activa && (n.RolDestino == "todos" || n.RolDestino == rolNotif))
                    .OrderByDescending(n => n.FechaCreacion)
                    .Take(5)
                    .ToList();

                ViewBag.NotificacionesActivas = notificaciones;
            }

            return View();
        }

        public IActionResult Perfil()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var usuario = _context.Usuarios.Find(userId);
            if (usuario == null)
                return RedirectToAction("Index", "Login");

            string rolUser = (usuario.ROL ?? "").Trim().ToLower();
            if (rolUser == "profesor") rolUser = "docente";

            var notificaciones = _context.Notificaciones
                .Include(n => n.Emisor)
                .Where(n => n.Activa && (n.RolDestino == "todos" || n.RolDestino == rolUser))
                .OrderByDescending(n => n.FechaCreacion)
                .Take(10)
                .ToList();

            ViewBag.NotificacionesPerfil = notificaciones;

            return View(usuario);
        }

        // 1. Mostrar el formulario de edición
        public IActionResult EditarPerfil()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var usuario = _context.Usuarios.Find(userId);
            return View(usuario);
        }

        // 2. Procesar la actualización
        [HttpPost]
        public IActionResult ActualizarPerfil(Usuario usuarioEditado)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var usuarioBd = _context.Usuarios.Find(userId);
            if (usuarioBd != null)
            {
                usuarioBd.TELEFONO = usuarioEditado.TELEFONO;
                usuarioBd.DIRECCION = usuarioEditado.DIRECCION;
                usuarioBd.CORREO_ELECTRONICO = usuarioEditado.CORREO_ELECTRONICO;
                _context.SaveChanges();
                TempData["Mensaje"] = "Perfil actualizado correctamente";
            }

            return RedirectToAction("Perfil");
        }

        // 3. Cambiar contraseña desde el perfil
        [HttpPost]
        public IActionResult CambiarContrasena(string contrasenaActual, string nuevaContrasena, string confirmar)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var usuario = _context.Usuarios.Find(userId);

            if (usuario.CONTRASEÑA != contrasenaActual)
            {
                ViewBag.ErrorPassword = "La contraseña actual es incorrecta.";
                return View("Perfil", usuario);
            }

            if (nuevaContrasena != confirmar)
            {
                ViewBag.ErrorPassword = "Las contraseñas nuevas no coinciden.";
                return View("Perfil", usuario);
            }

            if (nuevaContrasena == contrasenaActual)
            {
                ViewBag.ErrorPassword = "La nueva contraseña debe ser diferente a la actual.";
                return View("Perfil", usuario);
            }

            // Validación de contraseña segura (aporte de Andrés)
            if (!ValidarContrasenaSegura(nuevaContrasena, out string mensajeError))
            {
                ViewBag.ErrorPassword = mensajeError;
                return View("Perfil", usuario);
            }

            // Guardar en historial
            _context.HistorialPasswords.Add(new GU_HISTORIAL_PASSWORD
            {
                id_usuario = usuario.ID_Usuario,
                contrasena_hash = usuario.CONTRASEÑA,
                fecha_cambio = DateTime.Now
            });

            // Actualizar contraseña
            usuario.CONTRASEÑA = nuevaContrasena;
            _context.SaveChanges();

            ViewBag.ExitoPassword = "Contraseña Se Actualizo Correctamente.";
            return View("Perfil", usuario);
        }

        // Endpoint AJAX para validar la contraseña actual sin recargar la página (aporte de Andrés)
        [HttpPost]
        public IActionResult ValidarContrasenaActual([FromBody] ValidarContrasenaRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { valido = false });

            var usuario = _context.Usuarios.Find(userId);
            if (usuario == null) return Json(new { valido = false });

            bool valido = usuario.CONTRASEÑA == request.ContrasenaActual;
            return Json(new { valido });
        }

        public IActionResult RenovarSesion()
        {
            var _ = HttpContext.Session.GetString("NombreUsuario");
            return Ok();
        }

        // Endpoint AJAX para la campana de notificaciones del navbar
        [HttpGet]
        public IActionResult GetNotificacionesCampana()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { count = 0, items = Array.Empty<object>() });

            string rolSesion = (HttpContext.Session.GetString("UserRol") ?? "").Trim().ToLower();
            // Admin solo ve sus propias notificaciones gestionadas en /Admin/Notificaciones
            // Para el navbar mostramos notifs dirigidas al rol del usuario
            string rolNotif = rolSesion;
            if (rolNotif == "profesor") rolNotif = "docente";
            if (rolNotif == "admin") rolNotif = "admin_skip"; // admin no recibe notifs

            var notifs = _context.Notificaciones
                .Where(n => n.Activa && (n.RolDestino == "todos" || n.RolDestino == rolNotif))
                .OrderByDescending(n => n.FechaCreacion)
                .Take(10)
                .Select(n => new
                {
                    n.ID_Notificacion,
                    n.Titulo,
                    n.Mensaje,
                    Fecha = n.FechaCreacion.ToString("dd/MM/yyyy HH:mm"),
                    n.RolDestino
                })
                .ToList();

            return Json(new { count = notifs.Count, items = notifs });
        }

        // Otros métodos existentes
        public IActionResult Nosotros() => View();
        public IActionResult Contacto() => View();
        public IActionResult Cursos() => View();
        public IActionResult Blog() => View();

        // ─── MÉTODOS PRIVADOS (aporte de Andrés) ────────────────────────────
        private bool ValidarContrasenaSegura(string contrasena, out string mensaje)
        {
            if (contrasena.Length < 8)
            {
                mensaje = "La contraseña debe tener al menos 8 caracteres.";
                return false;
            }
            if (!contrasena.Any(char.IsUpper))
            {
                mensaje = "La contraseña debe contener al menos una letra mayúscula.";
                return false;
            }
            if (!contrasena.Any(char.IsLower))
            {
                mensaje = "La contraseña debe contener al menos una letra minúscula.";
                return false;
            }
            if (!contrasena.Any(char.IsDigit))
            {
                mensaje = "La contraseña debe contener al menos un número.";
                return false;
            }
            if (!contrasena.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
            {
                mensaje = "La contraseña debe contener al menos un carácter especial (!@#$%^&*...).";
                return false;
            }
            mensaje = string.Empty;
            return true;
        }
    }

    // ─── CLASES AUXILIARES (aporte de Andrés) ──────────────────────────────
    public class ValidarContrasenaRequest
    {
        public string ContrasenaActual { get; set; }
    }
}