using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

namespace ProyectoPAE.Controllers
{
    public class DocenteController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor con inyección de contexto de base de datos.
        /// </summary>
        /// <param name="context">Instancia del DbContext principal</param>
        public DocenteController(ApplicationDbContext context) { _context = context; }

        /// <summary>
        /// Muestra la vista de la Planilla de Calificaciones con la lista de estudiantes y la oferta de cursos disponible.
        /// </summary>
        /// <returns>Vista de planilla con la lista de estudiantes con rol 'estudiante'</returns>
        public IActionResult Planilla()
        {
            // 1. Verificación de sesión y autorización por rol (docente o admin)
            var rol = HttpContext.Session.GetString("UserRol");
            if (rol != "docente" && rol != "admin") return RedirectToAction("Index", "Login");

            // 2. Obtención de estudiantes registrados en el sistema
            var estudiantes = _context.Usuarios.Where(u => u.ROL == "estudiante").ToList();

            // 3. Consulta de grados desde la tabla CURSO (601 M - 1103 M, 601 T - 1103 T)
            var listaCursos = _context.Cursos.ToList();
            var grados = listaCursos
                .Select(c => c.nombre_curso)
                .Distinct()
                .OrderBy(n => {
                    var digits = new string(n.TakeWhile(char.IsDigit).ToArray());
                    return int.TryParse(digits, out int num) ? num : 999999;
                })
                .ThenBy(n => n)
                .ToList();

            // 4. Consulta de asignaturas/materias académicas desde la tabla MATERIA
            var asignaturas = _context.MATERIA
                .Select(m => m.nombre_materia)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            ViewBag.Grados = grados;
            ViewBag.Asignaturas = asignaturas;

            return View(estudiantes);
        }

        /// <summary>
        /// Registra y almacena las calificaciones individuales o masivas asignadas a los estudiantes.
        /// </summary>
        /// <param name="ID_Estudiante">ID del estudiante (en guardado individual)</param>
        /// <param name="Materia">Nombre de la materia</param>
        /// <param name="Nota">Valor de la nota registrada</param>
        /// <param name="Periodo">Número de periodo lectivo</param>
        /// <returns>Redirección a la vista de Planilla con mensaje de éxito o error</returns>
        [HttpPost]
        public IActionResult GuardarNota(int? ID_Estudiante, string Materia, string Nota, int? Periodo)
        {
            try
            {
                // Lectura de selecciones del formulario (Grado, Asignatura y Jornada)
                string gradoSel = Request.Form["selectGrado"].ToString();
                string asignaturaSel = Request.Form["selectAsignatura"].ToString();
                string jornadaSel = Request.Form["selectJornada"].ToString();

                // Combinación de la materia, grado y jornada (ejemplo: "Álgebra Lineal - 601 (Mañana)")
                string materiaSel = (!string.IsNullOrEmpty(asignaturaSel) && !string.IsNullOrEmpty(gradoSel))
                    ? (!string.IsNullOrEmpty(jornadaSel) ? $"{asignaturaSel} - {gradoSel} ({jornadaSel})" : $"{asignaturaSel} - {gradoSel}")
                    : "Matemáticas - 601 (Mañana)";

                int periodoVal = Periodo ?? 1;
                if (Request.Form.ContainsKey("selectPeriodo") && int.TryParse(Request.Form["selectPeriodo"], out int pParsed))
                {
                    periodoVal = pParsed;
                }

                // 1. Procesamiento de nota enviada de manera individual
                if (ID_Estudiante.HasValue && !string.IsNullOrEmpty(Nota))
                {
                    decimal notaDecimal = decimal.Parse(Nota.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                    if (notaDecimal >= 0 && notaDecimal <= 5)
                    {
                        var nuevaNota = new Calificacion
                        {
                            ID_Estudiante = ID_Estudiante.Value,
                            Materia = materiaSel,
                            Nota = notaDecimal,
                            Periodo = periodoVal,
                            FechaRegistro = DateTime.Now
                        };
                        _context.Calificaciones.Add(nuevaNota);
                    }
                }
                // 2. Procesamiento masivo de planilla de notas
                else
                {
                    var estudianteIds = Request.Form["estudiante_id"];
                    var notasDefinitivas = Request.Form["nota_valor"];

                    if (estudianteIds.Count > 0)
                    {
                        for (int i = 0; i < estudianteIds.Count; i++)
                        {
                            if (int.TryParse(estudianteIds[i], out int estId))
                            {
                                string strNota = (notasDefinitivas.Count > i) ? notasDefinitivas[i] : "0.0";
                                if (decimal.TryParse(strNota.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture, out decimal dNota))
                                {
                                    var calif = new Calificacion
                                    {
                                        ID_Estudiante = estId,
                                        Materia = materiaSel,
                                        Nota = dNota,
                                        Periodo = periodoVal,
                                        FechaRegistro = DateTime.Now
                                    };
                                    _context.Calificaciones.Add(calif);
                                }
                            }
                        }
                    }
                }

                // Persistencia de los cambios en la base de datos
                _context.SaveChanges();

                // Notificación visual de éxito para la vista
                TempData["MensajeNota"] = "¡Las calificaciones han sido guardadas exitosamente!";
            }
            catch (Exception ex)
            {
                // Notificación visual en caso de fallo
                TempData["ErrorNota"] = "Ocurrió un error al guardar las calificaciones: " + ex.Message;
            }

            return RedirectToAction("Planilla");
        }

        /// <summary>
        /// Genera la vista de reporte grupal con el listado de calificaciones por materia.
        /// </summary>
        public IActionResult ReporteGrupal(string materia, int? cursoId)
        {
            // Consultamos las notas incluyendo los datos del estudiante
            var reporte = _context.Calificaciones
                .Join(_context.Usuarios,
                      c => c.ID_Estudiante,
                      u => u.ID_Usuario,
                      (c, u) => new { c, u })
                .Select(res => new {
                    IdEstudiante = res.u.ID_Usuario,
                    NombreEstudiante = res.u.NOMBRES + " " + res.u.APELLIDOS,
                    Materia = res.c.Materia,
                    Nota = res.c.Nota,
                    Periodo = res.c.Periodo
                }).ToList();

            // Filtros opcionales (RF 5.5)
            if (!string.IsNullOrEmpty(materia))
            {
                reporte = reporte.Where(r => r.Materia == materia).ToList();
            }

            return View(reporte);
        }

        /// <summary>
        /// Genera y descarga el reporte del observador en formato PDF usando Rotativa.
        /// </summary>
        public IActionResult DescargarObservadorPdf()
        {
            return new ViewAsPdf("ReporteObservadorPdf")
            {
                FileName = $"Reporte_Observador_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
            };
        }


        // ==========================================
        // GESTIÓN DE CURSOS EXTRACURRICULARES (DOCENTE)
        // ==========================================

        [HttpPost]
        public IActionResult CrearCursoExtra(ExtraCurso model)
        {
            _context.AsegurarEsquemaExtracurriculares();

            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();
            if (rol != "docente" && rol != "admin" && rol != "profesor")
            {
                return Json(new { success = false, message = "No tienes permisos para crear cursos extracurriculares." });
            }

            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                return Json(new { success = false, message = "El nombre del curso es obligatorio." });
            }

            if (model.CuposTotales <= 0)
            {
                return Json(new { success = false, message = "Los cupos totales deben ser mayores a cero." });
            }

            if (model.FechaFinInscripcion < model.FechaInicioInscripcion)
            {
                return Json(new { success = false, message = "La fecha de cierre de inscripción no puede ser menor a la de inicio." });
            }

            if (model.FechaFin < model.FechaInicio)
            {
                return Json(new { success = false, message = "La fecha de finalización del curso no puede ser menor a la de inicio." });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var nombreDocente = HttpContext.Session.GetString("NombreUsuario");

            if (string.IsNullOrWhiteSpace(model.Instructor))
            {
                model.Instructor = nombreDocente ?? "Docente PAE";
            }

            model.IdDocente = userId;
            model.CuposDisponibles = model.CuposTotales;
            model.Activo = true;

            _context.ExtraCursos.Add(model);
            _context.SaveChanges();

            return Json(new { success = true, message = "¡Curso extracurricular publicado exitosamente!" });
        }

        [HttpGet]
        public IActionResult ObtenerInscritosCursoExtra(int idCurso)
        {
            _context.AsegurarEsquemaExtracurriculares();

            var inscritos = _context.ExtraInscripciones
                .Where(i => i.IdExtraCurso == idCurso && i.Estado == "Inscrito")
                .Join(_context.Usuarios,
                      i => i.IdEstudiante,
                      u => u.ID_Usuario,
                      (i, u) => new ExtraEstudianteInscritoDto
                      {
                          IdInscripcion = i.IdExtraInscripcion,
                          IdEstudiante = u.ID_Usuario,
                          NombreCompleto = (u.NOMBRES + " " + u.APELLIDOS).Trim(),
                          Documento = u.NUM_DOCUMENTO ?? u.NOMBRE_USUARIO,
                          Correo = u.CORREO_ELECTRONICO ?? "-",
                          Grado = u.COLEGIO_PROCEDENCIA ?? "Estudiante",
                          FechaInscripcion = i.FechaInscripcion,
                          Estado = i.Estado
                      })
                .OrderBy(e => e.NombreCompleto)
                .ToList();

            var curso = _context.ExtraCursos.Find(idCurso);

            return Json(new
            {
                success = true,
                nombreCurso = curso?.Nombre ?? "Curso Extracurricular",
                cuposTotales = curso?.CuposTotales ?? 0,
                cuposDisponibles = curso?.CuposDisponibles ?? 0,
                totalInscritos = inscritos.Count,
                estudiantes = inscritos
            });
        }

        [HttpPost]
        public IActionResult CambiarEstadoCursoExtra(int idCurso, bool activo)
        {
            _context.AsegurarEsquemaExtracurriculares();

            var curso = _context.ExtraCursos.Find(idCurso);
            if (curso == null)
            {
                return Json(new { success = false, message = "Curso no encontrado." });
            }

            curso.Activo = activo;
            _context.SaveChanges();

            return Json(new { success = true, message = activo ? "Curso activado correctamente." : "Curso pausado/inactivado." });
        }

        [HttpPost]
        public IActionResult EliminarCursoExtra(int idCurso)
        {
            _context.AsegurarEsquemaExtracurriculares();

            var curso = _context.ExtraCursos.Find(idCurso);
            if (curso == null)
            {
                return Json(new { success = false, message = "Curso no encontrado." });
            }

            bool tieneInscritos = _context.ExtraInscripciones.Any(i => i.IdExtraCurso == idCurso && i.Estado == "Inscrito");
            if (tieneInscritos)
            {
                // Si tiene inscritos, solo lo desactivamos para no perder historial
                curso.Activo = false;
                _context.SaveChanges();
                return Json(new { success = true, message = "El curso tiene alumnos inscritos, por lo cual ha sido desactivado en lugar de eliminarse." });
            }

            _context.ExtraCursos.Remove(curso);
            _context.SaveChanges();

            return Json(new { success = true, message = "Curso extracurricular eliminado con éxito." });
        }
    }
}

