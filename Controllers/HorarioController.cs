using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
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

            var matricula = await _context.Matriculas
                .FirstOrDefaultAsync(m => m.id_estudiante == estudianteId);

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

            return View(horariosEstudiante);
        }
    }
}