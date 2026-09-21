using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;

namespace ProyectoPAE.Controllers
{
    /// <summary>
    /// Controlador responsable de la consulta y despliegue de los horarios académicos de docentes y alumnos.
    /// </summary>
    public class HorarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor con inyección del contexto de base de datos Entity Framework Core.
        /// </summary>
        /// <param name="context">Instancia del DbContext principal de la aplicación</param>
        public HorarioController(ApplicationDbContext context)
        {
            _context = context;
        }

<<<<<<< Updated upstream
        /// <summary>
        /// Obtiene y presenta la vista "Mi Horario" para el docente autenticado en sesión,
        /// aplicando los filtros seleccionados (materia, mes, año, periodo).
        /// </summary>
        /// <param name="materia_filtro">Filtro por asignatura o nombre del curso</param>
        /// <param name="mes_filtro">Filtro por mes numérico (1-12)</param>
        /// <param name="ano_filtro">Filtro por año lectivo</param>
        /// <param name="periodo_filtro">Filtro por periodo académico (1-4)</param>
        /// <returns>Vista de la matriz de horario con la lista de clases asignadas</returns>
        public IActionResult MiHorario(string materia_filtro, int? mes_filtro, int? ano_filtro, int? periodo_filtro)
        {
            try
            {
                // 1. Asignación de valores por defecto en ViewBag para preservar los filtros en el formulario
                ViewBag.MesSeleccionado = mes_filtro ?? DateTime.Now.Month;
                ViewBag.AnoSeleccionado = ano_filtro ?? DateTime.Now.Year;
                ViewBag.PeriodoSeleccionado = periodo_filtro ?? 1;
                ViewBag.MateriaSeleccionada = materia_filtro;

                int? idProfesorConsulta = null;
                string nombreProfesorActual = "";

                // 2. Identificación del profesor autenticado mediante la variable de sesión "UserId"
                var idUsuario = HttpContext.Session.GetInt32("UserId");
                if (idUsuario.HasValue)
                {
                    var usuarioActual = _context.Usuarios.Find(idUsuario.Value);

                    // Se busca el perfil de Profesor por ID de usuario o coincidencia de correo electrónico
                    var profesor = _context.PROFESOR.FirstOrDefault(p =>
                        p.id_usuario == idUsuario.Value ||
                        (usuarioActual != null && !string.IsNullOrEmpty(usuarioActual.CORREO_ELECTRONICO) && p.email == usuarioActual.CORREO_ELECTRONICO));

                    if (profesor != null)
                    {
                        idProfesorConsulta = profesor.id_profesor;
                        nombreProfesorActual = profesor.nombre + " " + profesor.apellido;
                    }
                }

                // Respaldo de nombre en caso de no encontrar registro explícito en tabla PROFESOR
                if (string.IsNullOrEmpty(nombreProfesorActual))
                {
                    nombreProfesorActual = HttpContext.Session.GetString("NombreUsuario") ?? "Docente";
                }

                ViewBag.NombreProfesor = nombreProfesorActual;

                // 3. Cargar la lista de materias/cursos asignados para poblar el menú desplegable del filtro
                var materiasList = (from pm in _context.PROFESOR_MATERIA
                                    join m in _context.MATERIA on pm.id_materia equals m.id_materia
                                    where idProfesorConsulta == null || pm.id_profesor == idProfesorConsulta
                                    select m.nombre_materia)
                                   .Distinct()
                                   .OrderBy(n => n)
                                   .ToList();

                // Si no hay materias asignadas en PROFESOR_MATERIA, usar los cursos disponibles de la tabla CURSO
                if (!materiasList.Any())
                {
                    materiasList = _context.Cursos.Select(c => c.nombre_curso).Distinct().OrderBy(n => n).ToList();
                }

                ViewBag.Materias = materiasList;

                // 4. Si no se identificó ningún profesor válido en la sesión actual
                if (idProfesorConsulta == null)
                {
                    ViewBag.MensajeSinHorario = "Aún no hay horarios disponibles";
                    return View(new List<HorarioDetalleViewModel>());
                }

                // 5. Consulta LINQ a la tabla HORARIO realizando LEFT JOINs con CURSO, AULA y PROFESOR
                var query = from h in _context.HORARIOS
                            join c in _context.Cursos on h.id_curso equals c.id_curso into cGroup
                            from curso in cGroup.DefaultIfEmpty()
                            join a in _context.AULA on h.id_aula equals a.id_aula into aGroup
                            from aula in aGroup.DefaultIfEmpty()
                            join p in _context.PROFESOR on h.id_profesor equals p.id_profesor into pGroup
                            from profesor in pGroup.DefaultIfEmpty()
                            where h.id_profesor == idProfesorConsulta
                            select new HorarioDetalleViewModel
                            {
                                DiaSemana = h.dia,
                                HoraInicio = h.hora_inicio,
                                HoraFin = h.hora_fin,
                                NombreMateria = curso != null ? curso.nombre_curso : "Sin Asignatura",
                                NombreCurso = curso != null ? curso.nombre_curso : "General",
                                NombreProfesor = profesor != null ? profesor.nombre + " " + profesor.apellido : nombreProfesorActual,
                                NombreSalon = aula != null ? aula.codigo_aula + " (" + aula.tipo + ")" : "Aula General",
                                EsDescanso = false
                            };

                var horariosDB = query.ToList();

                // 6. Aplicar el filtro opcional por materia seleccionada en la interfaz
                if (!string.IsNullOrEmpty(materia_filtro))
                {
                    horariosDB = horariosDB
                        .Where(h => h.NombreMateria.Equals(materia_filtro, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // 7. Retornar vista vacía si la consulta no arrojó franjas asignadas
                if (!horariosDB.Any())
                {
                    ViewBag.MensajeSinHorario = "Aún no hay horarios disponibles";
                    return View(new List<HorarioDetalleViewModel>());
                }

                return View(horariosDB);
            }
            catch (Exception ex)
            {
                // Captura de errores y retorno seguro con mensaje para el usuario
                ViewBag.Error = "No se pudo cargar el horario: " + ex.Message;
                ViewBag.MensajeSinHorario = "Aún no hay horarios disponibles";
=======
        // =========================================================================
        // VISTA: ASIGNACIÓN ACADÉMICA
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> AsignacionAcademica()
        {
            try
            {
                ViewBag.Profesores = await _context.Set<Profesor>().ToListAsync();
                ViewBag.Cursos = await _context.Set<Course>().ToListAsync();
                ViewBag.Aulas = await _context.Set<Aula>().ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"---> Error al cargar asignación académica: {ex.Message}");

                ViewBag.Profesores = new List<Profesor>();
                ViewBag.Cursos = new List<Course>();
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
            var query = from pm in _context.Set<ProfesorMateria>()
                        join m in _context.Set<Materia>() on pm.id_materia equals m.id_materia
                        where pm.id_profesor == idProfesor
                        select new { m.id_materia, m.nombre_materia };

            // Si ya se eligió curso, cruzamos con el plan de estudios de ese curso
            if (idCurso > 0)
            {
                var idsDelPlan = await _context.Set<CursoMateria>()
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
            var materia = _context.Set<Materia>().Find(idMateria);
            var todasLasAulas = _context.Set<Aula>().ToList();

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
                    // Materias "normales" (matemáticas, español, sociales, etc.) van a aulas comunes
                    aulasFiltradas = todasLasAulas.Where(a => a.tipo == "Aula").ToList();
                }

                // Colchón de seguridad: si el filtro no encontró nada, mostramos todas
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
            var estudiantes = await (from m in _context.Set<Matricula>()
                                     join e in _context.Set<Estudiante>() on m.id_estudiante equals e.id_estudiante
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
            var materias = await (from cm in _context.Set<CursoMateria>()
                                  join m in _context.Set<Materia>() on cm.id_materia equals m.id_materia
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
            bool docenteDictaMateria = await _context.Set<ProfesorMateria>()
                .AnyAsync(pm => pm.id_profesor == id_profesor && pm.id_materia == id_materia);

            if (!docenteDictaMateria)
            {
                TempData["Error"] = "Ese docente no está habilitado para dictar esa materia.";
                return RedirectToAction("AsignacionAcademica");
            }

            // 2. Validamos choques: mismo profesor, mismo curso o misma aula, mismo día y hora
            bool hayChoqueDocente = await _context.Set<Horario>().AnyAsync(h =>
                h.id_profesor == id_profesor && h.dia == dia &&
                h.hora_inicio < hora_fin && hora_inicio < h.hora_fin);

            bool hayChoqueCurso = await _context.Set<Horario>().AnyAsync(h =>
                h.id_curso == id_curso && h.dia == dia &&
                h.hora_inicio < hora_fin && hora_inicio < h.hora_fin);

            bool hayChoqueAula = await _context.Set<Horario>().AnyAsync(h =>
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

            _context.Set<Horario>().Add(nuevoHorario);
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
            var docentes = await (from pm in _context.Set<ProfesorMateria>()
                                  join p in _context.Set<Profesor>() on pm.id_profesor equals p.id_profesor
                                  where pm.id_materia == idMateria
                                  select new
                                  {
                                      id_profesor = p.id_profesor,
                                      nombreCompleto = p.nombre + " " + p.apellido
                                  })
                                 .Distinct()
                                 .OrderBy(x => x.nombreCompleto)
                                 .ToListAsync();

            return Json(docentes);
        }
        // =========================================================================
        // 7. ENDPOINT AJAX: MI HORARIO (DINÁMICO SEGÚN EL ESTUDIANTE LOGUEADO)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> MiHorario()
        {
            // 1. Obtener el ID del estudiante autenticado desde la sesión
            // Nota: Asegúrate de que la clave en la sesión coincida con la que usas en el LoginController (ej. "IdEstudiante", "UsuarioId", etc.)
            int? estudianteIdSession = HttpContext.Session.GetInt32("IdEstudiante");

            // Si por alguna razón no hay sesión activa, puedes manejar un respaldo o redirigir al login
            if (!estudianteIdSession.HasValue)
            {
                // Valor de prueba temporal o redirección si lo prefieres
                estudianteIdSession = 1;
            }

            int estudianteId = estudianteIdSession.Value;

            // 2. Buscar en la base de datos a qué curso está matriculado este estudiante específico
            var matricula = await _context.Set<Matricula>()
                .FirstOrDefaultAsync(m => m.id_estudiante == estudianteId);

            if (matricula == null)
            {
                ViewBag.Mensaje = "No se encontró una matrícula activa para tu usuario.";
                ViewBag.NombreCurso = "Sin Curso Asignado";
                ViewBag.TotalMaterias = 0;
>>>>>>> Stashed changes
                return View(new List<HorarioDetalleViewModel>());
            }

            // 3. Consultar los datos del curso asignado (ej. 601 M, 1102 T, etc.)
            var curso = await _context.Set<Course>().FindAsync(matricula.id_curso);
            ViewBag.NombreCurso = curso?.nombre_curso ?? "Curso Desconocido";

            // 4. Consultar los horarios correspondientes a ese curso exacto con sus detalles
            var horariosDb = await (from h in _context.Set<Horario>()
                                    join m in _context.Set<Materia>() on h.id_materia equals m.id_materia
                                    join p in _context.Set<Profesor>() on h.id_profesor equals p.id_profesor
                                    join a in _context.Set<Aula>() on h.id_aula equals a.id_aula
                                    where h.id_curso == matricula.id_curso
                                    select new HorarioDetalleViewModel
                                    {
                                        DiaSemana = h.dia,
                                        HoraInicio = h.hora_inicio,
                                        HoraFin = h.hora_fin,
                                        NombreMateria = m.nombre_materia,
                                        NombreProfesor = p.nombre + " " + (p.apellido ?? ""),
                                        NombreSalon = a.codigo_aula
                                    }).ToListAsync();

            ViewBag.TotalMaterias = horariosDb.Select(x => x.NombreMateria).Distinct().Count();

            return View(horariosDb);
        }
    }
}