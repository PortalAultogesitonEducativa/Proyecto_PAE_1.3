using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoPAE.Controllers
{
    /// <summary>
    /// Controlador responsable de la consulta y despliegue de los horarios académicos de docentes y alumnos,
    /// así como de la asignación académica de materias, aulas y horarios.
    /// </summary>
    public class HorarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HorarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // VISTA: ASIGNACIÓN ACADÉMICA
        // =========================================================================
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<IActionResult> AsignacionAcademica()
        {
            try
            {
                ViewBag.Profesores = await _context.PROFESOR.ToListAsync();
                ViewBag.Cursos = await _context.Cursos.ToListAsync();
                ViewBag.Aulas = await _context.AULA.ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"---> Error al cargar asignación académica: {ex.Message}");

                ViewBag.Profesores = new List<Profesor>();
                ViewBag.Cursos = new List<Curso>();
                ViewBag.Aulas = new List<Aula>();
            }

            return View();
        }

        // =========================================================================
        // 1. ENDPOINT AJAX: MATERIAS QUE DICTA EL DOCENTE (filtradas por su curso)
        // =========================================================================
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<JsonResult> ObtenerMateriasPorDocente(int idProfesor, int idCurso)
        {
            var query = from pm in _context.PROFESOR_MATERIA
                        join m in _context.MATERIA on pm.id_materia equals m.id_materia
                        where pm.id_profesor == idProfesor
                        select new { m.id_materia, m.nombre_materia };

            // Si ya se eligió curso, cruzamos con el plan de estudios de ese curso
            if (idCurso > 0)
            {
                var idsDelPlan = await _context.CURSO_MATERIA
                    .Where(cm => cm.id_curso == idCurso)
                    .Select(cm => cm.id_materia)
                    .ToListAsync();

                query = query.Where(x => idsDelPlan.Contains(x.id_materia));
            }

            var materias = await query.Distinct()
                                       .OrderBy(x => x.nombre_materia)
                                       .ToListAsync();

            return Json(materias);
        }

        // =========================================================================
        // 2. ENDPOINT AJAX: AULAS RECOMENDADAS SEGÚN LA MATERIA
        // =========================================================================
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public JsonResult ObtenerAulasPorMateria(int idMateria)
        {
            var materia = _context.MATERIA.Find(idMateria);
            var todasLasAulas = _context.AULA.ToList();

            List<Aula> aulasFiltradas;

            if (materia == null)
            {
                aulasFiltradas = todasLasAulas;
            }
            else
            {
                string nombre = materia.nombre_materia.ToLower();

                if (nombre.Contains("física") || nombre.Contains("química") ||
                    nombre.Contains("biología") || nombre.Contains("ciencias naturales"))
                {
                    aulasFiltradas = todasLasAulas.Where(a => a.tipo == "Laboratorio").ToList();
                }
                else if (nombre.Contains("tecnología") || nombre.Contains("informática"))
                {
                    aulasFiltradas = todasLasAulas
                        .Where(a => a.tipo == "Sala de Sistemas" || a.codigo_aula.Contains("SAL-SIS"))
                        .ToList();
                }
                else if (nombre.Contains("artística"))
                {
                    aulasFiltradas = todasLasAulas.Where(a => a.codigo_aula.Contains("SAL-ART")).ToList();
                }
                else if (nombre.Contains("musical"))
                {
                    aulasFiltradas = todasLasAulas.Where(a => a.codigo_aula.Contains("SAL-MUS")).ToList();
                }
                else
                {
                    // Materias comunes van a aulas tipo Aula
                    aulasFiltradas = todasLasAulas.Where(a => a.tipo == "Aula").ToList();
                }

                // Si el filtro no encontró nada específico, mostrar todas
                if (aulasFiltradas.Count == 0)
                {
                    aulasFiltradas = todasLasAulas;
                }
            }

            var resultado = aulasFiltradas.Select(a => new {
                id_aula = a.id_aula,
                codigo_aula = a.codigo_aula,
                tipo = a.tipo,
                capacidad = a.capacidad
            });

            return Json(resultado);
        }

        // ============================================================================
        // 3. ENDPOINT AJAX: ESTUDIANTES POR CURSO
        // ============================================================================
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<JsonResult> ObtenerEstudiantesPorCurso(int idCurso)
        {
            var estudiantes = await (from m in _context.Matriculas
                                     join e in _context.ESTUDIANTE on m.id_estudiante equals e.id_estudiante
                                     where m.id_curso == idCurso
                                     select new
                                     {
                                         idEstudiante = e.id_estudiante,
                                         nombreCompleto = e.nombre + " " + (e.apellido ?? "")
                                     })
                                    .Distinct()
                                    .ToListAsync();

            return Json(estudiantes);
        }

        // ============================================================================
        // 4. ENDPOINT AJAX: MATERIAS POR CURSO (usado en la pestaña Alumnos)
        // ============================================================================
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<JsonResult> ObtenerMateriasPorCurso(int idCurso)
        {
            var materias = await (from cm in _context.CURSO_MATERIA
                                  join m in _context.MATERIA on cm.id_materia equals m.id_materia
                                  where cm.id_curso == idCurso
                                  select new
                                  {
                                      id_materia = m.id_materia,
                                      nombre_materia = m.nombre_materia
                                  })
                                 .ToListAsync();

            return Json(materias);
        }

        // =========================================================================
        // 5. GUARDAR ASIGNACIÓN DE DOCENTE (con validación de choques)
        // =========================================================================
        [HttpPost]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<IActionResult> GuardarAsignacionDocente(
            int id_profesor, int id_curso, int id_materia, int id_aula,
            string dia, TimeSpan hora_inicio, TimeSpan hora_fin)
        {
            if (id_profesor == 0 || id_curso == 0 || id_materia == 0 || id_aula == 0 || string.IsNullOrEmpty(dia))
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction("AsignacionAcademica");
            }

            // 1. Confirmamos que el docente sí dicte esa materia (blindaje en servidor)
            bool docenteDictaMateria = await _context.PROFESOR_MATERIA
                .AnyAsync(pm => pm.id_profesor == id_profesor && pm.id_materia == id_materia);

            if (!docenteDictaMateria)
            {
                TempData["Error"] = "Ese docente no está habilitado para dictar esa materia.";
                return RedirectToAction("AsignacionAcademica");
            }

            // 2. Validamos choques: mismo profesor, mismo curso o misma aula, mismo día y hora
            bool hayChoqueDocente = await _context.HORARIOS.AnyAsync(h =>
                h.id_profesor == id_profesor && h.dia == dia &&
                h.hora_inicio < hora_fin && hora_inicio < h.hora_fin);

            bool hayChoqueCurso = await _context.HORARIOS.AnyAsync(h =>
                h.id_curso == id_curso && h.dia == dia &&
                h.hora_inicio < hora_fin && hora_inicio < h.hora_fin);

            bool hayChoqueAula = await _context.HORARIOS.AnyAsync(h =>
                h.id_aula == id_aula && h.dia == dia &&
                h.hora_inicio < hora_fin && hora_inicio < h.hora_fin);

            if (hayChoqueDocente)
            {
                TempData["Error"] = "Ese docente ya tiene clase asignada en ese día y horario.";
                return RedirectToAction("AsignacionAcademica");
            }
            if (hayChoqueCurso)
            {
                TempData["Error"] = "Ese curso ya tiene otra clase asignada en ese día y horario.";
                return RedirectToAction("AsignacionAcademica");
            }
            if (hayChoqueAula)
            {
                TempData["Error"] = "Esa aula ya está ocupada en ese día y horario.";
                return RedirectToAction("AsignacionAcademica");
            }

            // 3. Si todo está limpio, guardamos
            var nuevoHorario = new Horario
            {
                id_profesor = id_profesor,
                id_curso = id_curso,
                id_materia = id_materia,
                id_aula = id_aula,
                dia = dia,
                hora_inicio = hora_inicio,
                hora_fin = hora_fin
            };

            _context.HORARIOS.Add(nuevoHorario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Asignación guardada correctamente.";
            return RedirectToAction("AsignacionAcademica");
        }

        // =========================================================================
        // 6. ENDPOINT AJAX: DOCENTES QUE DICTAN UNA MATERIA (para la pestaña Alumnos)
        // =========================================================================
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<JsonResult> ObtenerDocentesPorMateria(int idMateria)
        {
            var docentes = await (from pm in _context.PROFESOR_MATERIA
                                  join p in _context.PROFESOR on pm.id_profesor equals p.id_profesor
                                  where pm.id_materia == idMateria
                                  select new
                                  {
                                      id_profesor = p.id_profesor,
                                      nombreCompleto = p.nombre + " " + (p.apellido ?? "")
                                  })
                                 .Distinct()
                                 .OrderBy(x => x.nombreCompleto)
                                 .ToListAsync();

            return Json(docentes);
        }

        // =========================================================================
        // 7. MI HORARIO (DINÁMICO PARA ESTUDIANTES Y DOCENTES)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> MiHorario(string? materia_filtro = null)
        {
            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();
            var idUsuario = HttpContext.Session.GetInt32("UserId");

            // ── CASO A: DOCENTE / PROFESOR ──
            if (rol == "docente" || rol == "profesor")
            {
                int? idProfesorConsulta = null;
                string nombreProfesorActual = "";

                if (idUsuario.HasValue)
                {
                    var usuarioActual = await _context.Usuarios.FindAsync(idUsuario.Value);
                    var profesor = await _context.PROFESOR.FirstOrDefaultAsync(p =>
                        p.id_usuario == idUsuario.Value ||
                        (usuarioActual != null && !string.IsNullOrEmpty(usuarioActual.CORREO_ELECTRONICO) && p.email == usuarioActual.CORREO_ELECTRONICO));

                    if (profesor != null)
                    {
                        idProfesorConsulta = profesor.id_profesor;
                        nombreProfesorActual = profesor.nombre + " " + (profesor.apellido ?? "");
                    }
                }

                if (string.IsNullOrEmpty(nombreProfesorActual))
                {
                    nombreProfesorActual = HttpContext.Session.GetString("NombreUsuario") ?? "Docente";
                }

                ViewBag.EsDocente = true;
                ViewBag.NombreProfesor = nombreProfesorActual;
                ViewBag.NombreCurso = "Prof. " + nombreProfesorActual;

                if (idProfesorConsulta == null)
                {
                    ViewBag.Mensaje = "Aún no tienes horarios o clases asignadas.";
                    ViewBag.TotalMaterias = 0;
                    return View(new List<HorarioDetalleViewModel>());
                }

                var query = from h in _context.HORARIOS
                            join c in _context.Cursos on h.id_curso equals c.id_curso into cGroup
                            from curso in cGroup.DefaultIfEmpty()
                            join m in _context.MATERIA on h.id_materia equals m.id_materia into mGroup
                            from materia in mGroup.DefaultIfEmpty()
                            join a in _context.AULA on h.id_aula equals a.id_aula into aGroup
                            from aula in aGroup.DefaultIfEmpty()
                            where h.id_profesor == idProfesorConsulta.Value
                            select new HorarioDetalleViewModel
                            {
                                DiaSemana = h.dia,
                                HoraInicio = h.hora_inicio,
                                HoraFin = h.hora_fin,
                                PeriodoAcademico = h.periodo_academico,
                                NombreMateria = materia != null ? materia.nombre_materia : (curso != null ? curso.nombre_curso : "Asignatura"),
                                NombreCurso = curso != null ? curso.nombre_curso : "General",
                                NombreProfesor = nombreProfesorActual,
                                NombreSalon = aula != null ? aula.codigo_aula : "Aula General",
                                EsDescanso = false
                            };

                var horariosDocente = await query.ToListAsync();

                if (!string.IsNullOrEmpty(materia_filtro))
                {
                    horariosDocente = horariosDocente
                        .Where(h => h.NombreMateria.Equals(materia_filtro, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                ViewBag.TotalMaterias = horariosDocente.Select(x => x.NombreMateria).Distinct().Count();
                if (!horariosDocente.Any())
                {
                    ViewBag.Mensaje = "Aún no hay horarios registrados para tu perfil de docente.";
                }

                ViewBag.PeriodoAcademico = horariosDocente.Select(x => x.PeriodoAcademico).FirstOrDefault(p => p != null) ?? 1;
                return View(horariosDocente);
            }

            // ── CASO B: ESTUDIANTE ──
            ViewBag.EsDocente = false;

            int? estudianteIdSession = HttpContext.Session.GetInt32("IdEstudiante");
            if (!estudianteIdSession.HasValue && idUsuario.HasValue)
            {
                var estudiantePerfil = await _context.ESTUDIANTE
                    .FirstOrDefaultAsync(e => e.id_usuario == idUsuario.Value);
                if (estudiantePerfil != null)
                {
                    estudianteIdSession = estudiantePerfil.id_estudiante;
                }
            }

            if (!estudianteIdSession.HasValue)
            {
                estudianteIdSession = 1;
            }

            int estudianteId = estudianteIdSession.Value;

            // Tomamos la matrícula ACTIVA más reciente (por año y, si empatan, por id)
            var matricula = await _context.Matriculas
                .Where(m => m.id_estudiante == estudianteId &&
                            m.estado != null &&
                            (m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo"))
                .OrderByDescending(m => m.ano)
                .ThenByDescending(m => m.id_matricula)
                .FirstOrDefaultAsync();

            if (matricula == null)
            {
                ViewBag.Mensaje = "No se encontró una matrícula activa para tu usuario.";
                ViewBag.NombreCurso = "Sin Curso Asignado";
                ViewBag.TotalMaterias = 0;
                return View(new List<HorarioDetalleViewModel>());
            }

            var cursoObj = await _context.Cursos.FindAsync(matricula.id_curso);
            ViewBag.NombreCurso = cursoObj?.nombre_curso ?? "Curso Desconocido";

            var horariosEstudiante = await (from h in _context.HORARIOS
                                            join m in _context.MATERIA on h.id_materia equals m.id_materia into mGroup
                                            from mat in mGroup.DefaultIfEmpty()
                                            join p in _context.PROFESOR on h.id_profesor equals p.id_profesor into pGroup
                                            from prof in pGroup.DefaultIfEmpty()
                                            join a in _context.AULA on h.id_aula equals a.id_aula into aGroup
                                            from aul in aGroup.DefaultIfEmpty()
                                            where h.id_curso == matricula.id_curso
                                            select new HorarioDetalleViewModel
                                            {
                                                DiaSemana = h.dia,
                                                HoraInicio = h.hora_inicio,
                                                HoraFin = h.hora_fin,
                                                PeriodoAcademico = h.periodo_academico,
                                                NombreMateria = mat != null ? mat.nombre_materia : "Clase",
                                                NombreProfesor = prof != null ? (prof.nombre + " " + (prof.apellido ?? "")) : "Docente Asignado",
                                                NombreSalon = aul != null ? aul.codigo_aula : "Aula",
                                                NombreCurso = cursoObj != null ? cursoObj.nombre_curso : "",
                                                EsDescanso = false
                                            }).ToListAsync();

            ViewBag.TotalMaterias = horariosEstudiante.Select(x => x.NombreMateria).Distinct().Count();
            if (!horariosEstudiante.Any())
            {
                ViewBag.Mensaje = "Aún no hay horarios registrados para tu curso (" + ViewBag.NombreCurso + ").";
            }

            ViewBag.PeriodoAcademico = horariosEstudiante.Select(x => x.PeriodoAcademico).FirstOrDefault(p => p != null) ?? 1;
            return View(horariosEstudiante);
        }
        // Obtener horario filtrado por Docente o por Curso usando .Include()
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<IActionResult> ObtenerHorario(int? idDocente, int? idCurso)
        {
            try
            {
                // Construimos la consulta base incluyendo las relaciones
                var query = _context.HORARIOS
                    .Include(h => h.Materia)
                    .Include(h => h.Curso)
                    .Include(h => h.Profesor)
                    .AsQueryable();

                // Filtramos según lo seleccionado por el usuario
                if (idDocente.HasValue && idDocente > 0)
                {
                    query = query.Where(h => h.id_profesor == idDocente.Value);
                }
                else if (idCurso.HasValue && idCurso > 0)
                {
                    query = query.Where(h => h.id_curso == idCurso.Value);
                }
                else
                {
                    // Si no hay docente ni curso seleccionado, retornamos lista vacía
                    return Json(new List<object>());
                }

                var lista = await query.ToListAsync();

                // Proyectamos el resultado para evitar ciclos y asegurar formato seguro
                var resultado = lista.Select(h => new
                {
                    id = h.id_horario,
                    bloque = h.hora_inicio.ToString(@"hh\:mm") + " - " + h.hora_fin.ToString(@"hh\:mm"),
                    dia = h.dia,
                    materia = h.Materia != null ? h.Materia.nombre_materia : "Sin Materia",
                    curso = h.Curso != null ? h.Curso.nombre_curso : "",
                    docente = h.Profesor != null ? (h.Profesor.nombre + " " + h.Profesor.apellido) : ""
                });

                return Json(resultado);
            }
            catch (Exception ex)
            {
                // En caso de error, devuelves un objeto indicando el fallo para no romper la vista
                return StatusCode(500, new { message = ex.Message });
            }
        }
        // Acción HTTP POST para eliminar una asignación de horario específica
        [HttpPost]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<IActionResult> EliminarHorario(int id)
        {
            try
            {
                // Buscamos el registro en la base de datos por su ID principal
                var horario = await _context.HORARIOS.FindAsync(id);

                if (horario == null)
                {
                    return Json(new { success = false, message = "No se encontró el registro de horario a eliminar." });
                }

                // Removemos la entidad del contexto de la base de datos
                _context.HORARIOS.Remove(horario);

                // Guardamos los cambios de forma asíncrona
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "La asignación ha sido eliminada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }
        // =========================================================================
        // 9. LISTAS PARA EL FORMULARIO
        // =========================================================================
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public JsonResult ObtenerCursosPorJornada(string jornada)
        {
            // Se ordena por el número del curso (601, 602... 1001, 1101), no como texto,
            // porque como texto "1001" quedaría antes que "601".
            var cursos = _context.Cursos
                .Where(c => c.jornada == jornada)
                .Select(c => new { c.id_curso, c.nombre_curso })
                .AsEnumerable()
                .OrderBy(c => OrdenNumericoCurso(c.nombre_curso))
                .ThenBy(c => c.nombre_curso)
                .ToList();

            return Json(cursos);
        }

        // Extrae el número inicial del nombre del curso ("601 M" -> 601). Si no tiene, va al final.
        private static int OrdenNumericoCurso(string? nombre)
        {
            var digitos = new string((nombre ?? "").TrimStart().TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digitos, out var n) ? n : int.MaxValue;
        }

        // Docentes de la jornada. Si un docente aún no tiene jornada registrada (NULL o vacía),
        // también se muestra para que no quede fuera de la lista.
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public JsonResult ObtenerDocentesPorJornada(string jornada)
        {
            var docentes = _context.PROFESOR
                .Where(p => p.jornada == jornada || p.jornada == null || p.jornada == "")
                .OrderBy(p => p.nombre).ThenBy(p => p.apellido)
                .Select(p => new
                {
                    id_profesor = p.id_profesor,
                    nombreCompleto = p.nombre + " " + p.apellido
                })
                .ToList();

            return Json(docentes);
        }

        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public JsonResult ObtenerAulas()
        {
            var aulas = _context.AULA
                .OrderBy(a => a.codigo_aula)
                .Select(a => new
                {
                    id_aula = a.id_aula,
                    nombre = a.codigo_aula + " - " + a.tipo + " (Capacidad: " + a.capacidad + ")"
                })
                .ToList();

            return Json(aulas);
        }

        // Franjas fijas de la jornada (incluye el descanso, que el formulario deja bloqueado)
        // Franjas de 2 horas seguidas (clase doble) válidas para la jornada.
        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public JsonResult ObtenerBloquesDobles(string jornada)
        {
            var dobles = BloquesHorarios.DoblesPorJornada(jornada).Select(b => new
            {
                inicio = b.Inicio.ToString(@"hh\:mm"),
                fin = b.Fin.ToString(@"hh\:mm"),
                etiqueta = b.Etiqueta,
                nombre = b.Nombre,
                esReceso = false
            });

            return Json(dobles);
        }

        [HttpGet]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public JsonResult ObtenerBloques(string jornada)
        {
            var bloques = BloquesHorarios.PorJornada(jornada).Select(b => new
            {
                inicio = b.Inicio.ToString(@"hh\:mm"),
                fin = b.Fin.ToString(@"hh\:mm"),
                etiqueta = b.Etiqueta,
                nombre = b.Nombre,
                esReceso = b.EsReceso
            });

            return Json(bloques);
        }
        // =========================================================================
        // 8. GUARDAR HORARIO (valida período, jornada, franja fija, materia del docente y choques)
        // =========================================================================
        [HttpPost]
        [RequierePermiso(Permisos.AsignarAcademica)]
        public async Task<JsonResult> GuardarHorario(int idCurso, int idProfesor, int idAula, int idMateria, string dia, string horaInicio, string horaFin, int periodoAcademico)
        {
            const int anioEscolar = 2026;

            if (idCurso == 0 || idProfesor == 0 || idAula == 0 || idMateria == 0 || string.IsNullOrWhiteSpace(dia)
                || !TimeSpan.TryParse(horaInicio, out var inicio) || !TimeSpan.TryParse(horaFin, out var fin) || periodoAcademico == 0)
            {
                return Json(new { success = false, message = "Completa todos los campos (incluyendo el período académico) antes de guardar." });
            }

            var curso = await _context.Cursos.FindAsync(idCurso);
            var profesor = await _context.PROFESOR.FindAsync(idProfesor);

            if (curso == null || profesor == null)
            {
                return Json(new { success = false, message = "El curso o el profesor seleccionado no existen." });
            }

            // 1. Jornada
            if (!string.IsNullOrWhiteSpace(profesor.jornada) &&
                !string.Equals(curso.jornada, profesor.jornada, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = $"Conflicto de jornada: el curso '{curso.nombre_curso}' es de jornada '{curso.jornada}', pero {profesor.nombre} {profesor.apellido} labora en la jornada '{profesor.jornada}'."
                });
            }

            // 2. Franja fija
            var bloque = BloquesHorarios.BuscarFranjaValida(curso.jornada, inicio, fin);
            if (bloque == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Ese horario no es una franja de clase válida para la jornada del curso."
                });
            }

            // 3. El docente debe dictar esa materia
            bool docenteDictaMateria = await _context.Set<ProfesorMateria>()
                .AnyAsync(pm => pm.id_profesor == idProfesor && pm.id_materia == idMateria);

            if (!docenteDictaMateria)
            {
                return Json(new { success = false, message = "Ese docente no está habilitado para dictar esa materia." });
            }

            // 4. Choques (mismo período, anio, día y franja)
            bool choqueDocente = await _context.HORARIOS.AnyAsync(h =>
                h.anio_escolar == anioEscolar && h.periodo_academico == periodoAcademico && h.dia == dia && h.id_profesor == idProfesor &&
                h.hora_inicio < fin && inicio < h.hora_fin);
            if (choqueDocente)
            {
                return Json(new { success = false, message = "Ese docente ya tiene una clase asignada ese día en esa franja para este período." });
            }

            bool choqueCurso = await _context.HORARIOS.AnyAsync(h =>
                h.anio_escolar == anioEscolar && h.periodo_academico == periodoAcademico && h.dia == dia && h.id_curso == idCurso &&
                h.hora_inicio < fin && inicio < h.hora_fin);
            if (choqueCurso)
            {
                return Json(new { success = false, message = $"El curso {curso.nombre_curso} ya tiene una clase asignada ese día en esa franja para este período." });
            }

            bool choqueAula = await _context.HORARIOS.AnyAsync(h =>
                h.anio_escolar == anioEscolar && h.periodo_academico == periodoAcademico && h.dia == dia && h.id_aula == idAula &&
                h.hora_inicio < fin && inicio < h.hora_fin);
            if (choqueAula)
            {
                return Json(new { success = false, message = "Esa aula ya está ocupada ese día en esa franja para este período." });
            }

            // 5. Guardado incluyendo el período académico
            try
            {
                _context.HORARIOS.Add(new Horario
                {
                    id_curso = idCurso,
                    id_profesor = idProfesor,
                    id_aula = idAula,
                    id_materia = idMateria,
                    dia = dia,
                    hora_inicio = inicio,
                    hora_fin = fin,
                    anio_escolar = anioEscolar,
                    periodo_academico = periodoAcademico // <--- Asignación correcta
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "No se pudo guardar: " + (ex.InnerException?.Message ?? ex.Message) });
            }

            return Json(new { success = true, message = $"Horario guardado: Período {periodoAcademico}, {curso.nombre_curso}, {dia} {bloque.Etiqueta}." });
        }
    }
}
