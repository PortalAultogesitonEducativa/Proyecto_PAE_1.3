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
            if (rolActivo == "profesor") rolActivo = "docente";

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

                // --- Cargar datos para Reportes Pro ---
                var repPro = new ReportesProViewModel();
                repPro.TotalEstudiantes = _context.ESTUDIANTE.Count();

                // 1. Rendimiento Académico Anual
                if (_context.Calificaciones.Any())
                {
                    repPro.TotalCalificaciones = _context.Calificaciones.Count();
                    repPro.PromedioInstitucional = Math.Round((double)_context.Calificaciones.Average(c => (double)c.Nota), 2);
                    repPro.Aprobados = _context.Calificaciones.Count(c => c.Nota >= 3.0m);
                    repPro.Reprobados = _context.Calificaciones.Count(c => c.Nota < 3.0m);

                    repPro.PromediosPorMateria = _context.Calificaciones
                        .GroupBy(c => c.Materia)
                        .Select(g => new MateriaPromedioItem
                        {
                            Materia = string.IsNullOrWhiteSpace(g.Key) ? "General" : g.Key,
                            Promedio = Math.Round((double)g.Average(c => (double)c.Nota), 2)
                        })
                        .ToList();
                }
                else if (_context.EVALUACIONES.Any())
                {
                    repPro.TotalCalificaciones = _context.EVALUACIONES.Count();
                    repPro.PromedioInstitucional = Math.Round((double)_context.EVALUACIONES.Average(e => (double)e.nota), 2);
                    repPro.Aprobados = _context.EVALUACIONES.Count(e => e.nota >= 3.0m);
                    repPro.Reprobados = _context.EVALUACIONES.Count(e => e.nota < 3.0m);

                    repPro.PromediosPorMateria = (from ev in _context.EVALUACIONES
                                                  join m in _context.MATERIA on ev.id_materia equals m.id_materia into mGroup
                                                  from mat in mGroup.DefaultIfEmpty()
                                                  group ev by mat != null ? mat.nombre_materia : $"Materia {ev.id_materia}" into g
                                                  select new MateriaPromedioItem
                                                  {
                                                      Materia = g.Key,
                                                      Promedio = Math.Round((double)g.Average(e => (double)e.nota), 2)
                                                  }).ToList();
                }

                if (repPro.TotalCalificaciones > 0)
                {
                    repPro.PorcentajeAprobacion = Math.Round((double)repPro.Aprobados / repPro.TotalCalificaciones * 100, 1);
                    repPro.PorcentajeReprobacion = Math.Round((double)repPro.Reprobados / repPro.TotalCalificaciones * 100, 1);
                }

                // 2. Convivencia y Permanencia
                repPro.TotalAsistencias = _context.ASISTENCIAS.Count();
                if (repPro.TotalAsistencias > 0)
                {
                    repPro.AsistenciasPresentes = _context.ASISTENCIAS.Count(a => a.estado != null && (a.estado.ToLower() == "presente" || a.estado.ToLower() == "asistio"));
                    repPro.PorcentajeAsistencia = Math.Round((double)repPro.AsistenciasPresentes / repPro.TotalAsistencias * 100, 1);
                }

                repPro.TotalMatriculas = _context.Matriculas.Count();
                if (repPro.TotalMatriculas > 0)
                {
                    repPro.Desertores = _context.Matriculas.Count(m => m.estado != null && (m.estado.ToLower() == "retirado" || m.estado.ToLower() == "cancelada" || m.estado.ToLower() == "inactiva"));
                    repPro.PorcentajeDesercion = Math.Round((double)repPro.Desertores / repPro.TotalMatriculas * 100, 1);
                }

                // 3. Matrícula por Grado y Género
                int[] listaGrados = new[] { 6, 7, 8, 9, 10, 11 };
                foreach (var gr in listaGrados)
                {
                    repPro.MatriculasPorGrado[gr] = _context.Matriculas.Count(m => m.id_curso == gr);
                }

                var generosEstudiantes = (from u in _context.Usuarios
                                          where u.ROL != null && u.ROL.ToLower() == "estudiante"
                                          select u.GENERO).ToList();

                repPro.TotalHombres = generosEstudiantes.Count(g => !string.IsNullOrEmpty(g) && (g.Trim().ToLower() == "masculino" || g.Trim().ToLower() == "m" || g.Trim().ToLower() == "hombre"));
                repPro.TotalMujeres = generosEstudiantes.Count(g => !string.IsNullOrEmpty(g) && (g.Trim().ToLower() == "femenino" || g.Trim().ToLower() == "f" || g.Trim().ToLower() == "mujer"));
                repPro.TotalSinGenero = generosEstudiantes.Count - (repPro.TotalHombres + repPro.TotalMujeres);

                ViewBag.ReportesPro = repPro;
            }

            // 6. Lógica específica para el ESTUDIANTE: Cargar el horario según su curso y cursos extracurriculares
            if (rolActivo == "estudiante")
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    // 1. Buscar la entidad Estudiante vinculada al id_usuario en sesión o por correo
                    var usuarioActual = _context.Usuarios.Find(userId);
                    var estudiante = _context.ESTUDIANTE
                        .FirstOrDefault(e => e.id_usuario == userId || (usuarioActual != null && e.email == usuarioActual.CORREO_ELECTRONICO));

                    int? gradoEstudiante = null;

                    if (estudiante != null)
                    {
                        // 2. Buscar la matrícula activa para conocer su id_curso
                        var matriculaActiva = _context.Matriculas
                            .FirstOrDefault(m => m.id_estudiante == estudiante.id_estudiante && m.estado != null && (m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo"));

                        if (matriculaActiva != null)
                        {
                            gradoEstudiante = matriculaActiva.id_curso;

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

                    ViewBag.GradoEstudiante = gradoEstudiante;

                    // Cursos Extracurriculares para Estudiante
                    _context.AsegurarEsquemaExtracurriculares();
                    var inscripcionesEstudiante = _context.ExtraInscripciones
                        .Where(i => i.IdEstudiante == userId && i.Estado == "Inscrito")
                        .ToList();

                    var todosCursosExtra = _context.ExtraCursos
                        .OrderByDescending(c => c.Activo)
                        .ThenBy(c => c.FechaInicio)
                        .ToList();

                    var listaViewModelExtra = todosCursosExtra.Select(c =>
                    {
                        var insc = inscripcionesEstudiante.FirstOrDefault(i => i.IdExtraCurso == c.IdExtraCurso);
                        return new ExtraCursoItemViewModel
                        {
                            IdExtraCurso = c.IdExtraCurso,
                            Nombre = c.Nombre,
                            Descripcion = c.Descripcion,
                            Instructor = c.Instructor,
                            CuposTotales = c.CuposTotales,
                            CuposDisponibles = c.CuposDisponibles,
                            FechaInicio = c.FechaInicio,
                            FechaFin = c.FechaFin,
                            FechaInicioInscripcion = c.FechaInicioInscripcion,
                            FechaFinInscripcion = c.FechaFinInscripcion,
                            Activo = c.Activo,
                            GradoMin = c.GradoMin,
                            GradoMax = c.GradoMax,
                            Horario = c.Horario,
                            IdDocente = c.IdDocente,
                            EstaInscrito = insc != null,
                            IdInscripcion = insc?.IdExtraInscripcion,
                            FechaInscripcionEstudiante = insc?.FechaInscripcion
                        };
                    }).ToList();

                    ViewBag.CursosExtraEstudiante = listaViewModelExtra;
                    ViewBag.MisCursosExtraInscritos = listaViewModelExtra.Where(c => c.EstaInscrito).ToList();
                }
                else
                {
                    return RedirectToAction("Index", "Login");
                }
            }

            // Lógica para DOCENTE y ADMIN: Cursos Extracurriculares
            if (rolActivo == "docente" || rolActivo == "admin" || rolActivo == "profesor")
            {
                _context.AsegurarEsquemaExtracurriculares();
                var cursosExtraDocente = _context.ExtraCursos
                    .OrderByDescending(c => c.IdExtraCurso)
                    .ToList();

                var cursosDocenteVM = cursosExtraDocente.Select(c => new ExtraCursoItemViewModel
                {
                    IdExtraCurso = c.IdExtraCurso,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Instructor = c.Instructor,
                    CuposTotales = c.CuposTotales,
                    CuposDisponibles = c.CuposDisponibles,
                    FechaInicio = c.FechaInicio,
                    FechaFin = c.FechaFin,
                    FechaInicioInscripcion = c.FechaInicioInscripcion,
                    FechaFinInscripcion = c.FechaFinInscripcion,
                    Activo = c.Activo,
                    GradoMin = c.GradoMin,
                    GradoMax = c.GradoMax,
                    Horario = c.Horario,
                    IdDocente = c.IdDocente
                }).ToList();

                ViewBag.CursosExtraDocente = cursosDocenteVM;
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

            // 8. Cargar lista de estudiantes registrados en la base de datos para Control de Asistencia
            var estudiantesAsistencia = _context.Usuarios
                .Where(u => u.ROL != null && u.ROL.ToLower() == "estudiante" && u.ACTIVO)
                .OrderBy(u => u.APELLIDOS)
                .ThenBy(u => u.NOMBRES)
                .ToList();

            ViewBag.EstudiantesAsistencia = estudiantesAsistencia;

            // 9. Cargar lista de cursos/grados para Control de Asistencia
            var listaCursos = _context.Cursos.ToList();
            var cursosAsistencia = listaCursos
                .Select(c => c.nombre_curso)
                .Distinct()
                .OrderBy(n => {
                    var digits = new string(n.TakeWhile(char.IsDigit).ToArray());
                    return int.TryParse(digits, out int num) ? num : 999999;
                })
                .ThenBy(n => n)
                .ToList();

            ViewBag.CursosAsistencia = cursosAsistencia;

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

            string userTarget = $"user_{userId}";

            var notificaciones = _context.Notificaciones
                .Include(n => n.Emisor)
                .Where(n => n.Activa && (n.RolDestino == "todos" || n.RolDestino == rolUser || n.RolDestino == userTarget))
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

                // Crear notificación de actualización de perfil
                var notifPerfil = new Notificacion
                {
                    Titulo = "Perfil Actualizado",
                    Mensaje = "Has actualizado tus datos personales de perfil exitosamente.",
                    RolDestino = $"user_{userId}",
                    FechaCreacion = DateTime.Now,
                    ID_UsuarioEmisor = userId.Value,
                    Activa = true
                };
                _context.Notificaciones.Add(notifPerfil);

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

            if (!VerificarPassword(contrasenaActual, usuario.CONTRASEÑA))
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

            // Crear notificación de cambio de contraseña
            var notifPassword = new Notificacion
            {
                Titulo = "Cambio de Contraseña",
                Mensaje = "Tu contraseña ha sido actualizada correctamente por motivos de seguridad.",
                RolDestino = $"user_{userId}",
                FechaCreacion = DateTime.Now,
                ID_UsuarioEmisor = userId.Value,
                Activa = true
            };
            _context.Notificaciones.Add(notifPassword);

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

            bool valido = VerificarPassword(request.ContrasenaActual, usuario.CONTRASEÑA);
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

            _context.AsegurarEsquemaNotificaciones();

            string rolSesion = (HttpContext.Session.GetString("UserRol") ?? "").Trim().ToLower();
            // Admin solo ve sus propias notificaciones gestionadas en /Admin/Notificaciones
            // Para el navbar mostramos notifs dirigidas al rol del usuario
            string rolNotif = rolSesion;
            if (rolNotif == "profesor") rolNotif = "docente";
            if (rolNotif == "admin") rolNotif = "admin_skip"; // admin no recibe notifs

            string userTarget = $"user_{userId}";

            // Notificaciones leídas por este usuario
            var leidasIds = _context.NotificacionesLeidas
                .Where(nl => nl.ID_Usuario == userId.Value)
                .Select(nl => nl.ID_Notificacion)
                .ToList();

            var now = DateTime.Now;

            var notifs = _context.Notificaciones
                .Where(n => n.Activa
                    && (n.RolDestino == "todos" || n.RolDestino == rolNotif || n.RolDestino == userTarget)
                    && (!leidasIds.Contains(n.ID_Notificacion))
                    && (n.FechaExpiracion == null || n.FechaExpiracion > now))
                .OrderByDescending(n => n.FechaCreacion)
                .Take(20)
                .Select(n => new
                {
                    n.ID_Notificacion,
                    n.Titulo,
                    n.Mensaje,
                    Fecha = n.FechaCreacion.ToString("dd/MM/yyyy HH:mm"),
                    n.RolDestino,
                    Prioridad = string.IsNullOrEmpty(n.Prioridad) ? "leve" : n.Prioridad.ToLower(),
                    FechaExpiracion = n.FechaExpiracion.HasValue ? n.FechaExpiracion.Value.ToString("dd/MM/yyyy HH:mm") : null
                })
                .ToList();

            return Json(new { count = notifs.Count, items = notifs });
        }

        // Endpoint AJAX para descartar / marcar como leída una notificación
        [HttpPost]
        public IActionResult MarcarNotificacionLeida([FromBody] MarcarLeidoRequest req)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || req == null || req.IdNotificacion <= 0)
                return Json(new { success = false, message = "Sesión inválida o datos incorrectos." });

            _context.AsegurarEsquemaNotificaciones();

            var notif = _context.Notificaciones.FirstOrDefault(n => n.ID_Notificacion == req.IdNotificacion);
            if (notif == null)
                return Json(new { success = false, message = "Notificación no encontrada." });

            if ((notif.Prioridad ?? "").Trim().ToLower() == "alta")
            {
                return Json(new { success = false, message = "Las notificaciones de prioridad alta no se pueden descartar." });
            }

            bool yaLeida = _context.NotificacionesLeidas.Any(nl => nl.ID_Notificacion == req.IdNotificacion && nl.ID_Usuario == userId.Value);
            if (!yaLeida)
            {
                _context.NotificacionesLeidas.Add(new NotificacionLeida
                {
                    ID_Notificacion = req.IdNotificacion,
                    ID_Usuario = userId.Value,
                    FechaLeido = DateTime.Now
                });
                _context.SaveChanges();
            }

            return Json(new { success = true });
        }

        // Otros métodos existentes
        public IActionResult Nosotros() => View();
        public IActionResult Contacto() => View();
        public IActionResult Cursos() => View();
        public IActionResult Blog() => View();

        // ─── MÉTODOS PRIVADOS (aporte de Andrés) ────────────────────────────

        // Verifica la contraseña ingresada contra la almacenada.
        // Soporta ambos casos presentes en la base de datos actual:
        // contraseñas hasheadas con BCrypt y contraseñas en texto plano (legado).
        private bool VerificarPassword(string passwordIngresada, string passwordAlmacenada)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(passwordIngresada, passwordAlmacenada);
            }
            catch
            {
                return passwordAlmacenada == passwordIngresada;
            }
        }

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

    public class MarcarLeidoRequest
    {
        public int IdNotificacion { get; set; }
    }
}