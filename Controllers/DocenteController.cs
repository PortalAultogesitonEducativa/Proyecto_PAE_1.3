using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

namespace ProyectoPAE.Controllers
{
    public class DocenteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GestorDocumentalService _gestorDocumental;

        /// <summary>
        /// Constructor con inyección de contexto de base de datos y gestor documental centralizado.
        /// </summary>
        /// <param name="context">Instancia del DbContext principal</param>
        /// <param name="gestorDocumental">Servicio central de gestión y archivo consecutivo de documentos</param>
        public DocenteController(ApplicationDbContext context, GestorDocumentalService gestorDocumental)
        {
            _context = context;
            _gestorDocumental = gestorDocumental;
        }

        /// <summary>
        /// Muestra la vista de la Planilla de Calificaciones con la lista de estudiantes y la oferta de cursos disponible.
        /// </summary>
        /// <returns>Vista de planilla con la lista de estudiantes con rol 'estudiante'</returns>
        public IActionResult Planilla()
        {
            _context.AsegurarEsquemaEstudiantes();
            _context.AsegurarEsquemaLogros();

            // 1. Verificación de sesión y autorización por rol (docente o admin)
            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();
            if (rol != "docente" && rol != "admin" && rol != "profesor") return RedirectToAction("Index", "Login");

            var userId = HttpContext.Session.GetInt32("UserId");
            var usuarioActual = userId.HasValue ? _context.Usuarios.Find(userId.Value) : null;

            // 2. Obtención de estudiantes registrados en el sistema
            var estudiantes = _context.Usuarios.Where(u => u.ROL == "estudiante" && u.ACTIVO).ToList();

            // 2b. Mapeo de estudiante a su curso/grado asignado vía ESTUDIANTE y MATRICULA
            var estudiantesEst = _context.ESTUDIANTE.ToList();
            var matriculas = _context.Matriculas.ToList();
            var listaCursos = _context.Cursos.ToList();

            var estudianteGrados = new Dictionary<int, string>();
            foreach (var u in estudiantes)
            {
                var est = estudiantesEst.FirstOrDefault(e => (e.id_usuario != null && e.id_usuario == u.ID_Usuario) || (e.email != null && u.CORREO_ELECTRONICO != null && e.email.ToLower() == u.CORREO_ELECTRONICO.ToLower()));
                if (est != null)
                {
                    if (!string.IsNullOrEmpty(est.curso_asignado))
                    {
                        estudianteGrados[u.ID_Usuario] = est.curso_asignado;
                    }
                    else
                    {
                        var mat = matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante && (m.estado == null || m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo" || m.estado == ""));
                        if (mat == null)
                        {
                            mat = matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante);
                        }
                        if (mat != null)
                        {
                            var cursoObj = listaCursos.FirstOrDefault(c => c.id_curso == mat.id_curso);
                            string nombreCurso = cursoObj != null ? cursoObj.nombre_curso : mat.id_curso.ToString();
                            estudianteGrados[u.ID_Usuario] = nombreCurso;
                        }
                    }
                }
            }
            ViewBag.EstudianteGrados = estudianteGrados;

            // 3. Consulta de asignaturas y cursos asignados al docente
            List<string> grados = new List<string>();
            List<string> asignaturas = new List<string>();

            if (rol == "admin")
            {
                // El administrador puede acceder a todos los cursos y materias
                grados = listaCursos
                    .Select(c => c.nombre_curso)
                    .Distinct()
                    .OrderBy(n => {
                        var digits = new string(n.TakeWhile(char.IsDigit).ToArray());
                        return int.TryParse(digits, out int num) ? num : 999999;
                    })
                    .ThenBy(n => n)
                    .ToList();

                asignaturas = _context.MATERIA
                    .Select(m => m.nombre_materia)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            }
            else
            {
                // Buscar profesor vinculado al usuario en sesión
                string correoDoc = (usuarioActual?.CORREO_ELECTRONICO ?? "").Trim().ToLower();
                string nombresDoc = (usuarioActual?.NOMBRES ?? "").Trim().ToLower();
                string apellidosDoc = (usuarioActual?.APELLIDOS ?? "").Trim().ToLower();
                string nombreCompletoDoc = $"{nombresDoc} {apellidosDoc}".Trim();

                var profesoresDoc = _context.PROFESOR.ToList();
                var idsProfesor = profesoresDoc
                    .Where(p => (p.id_usuario != null && p.id_usuario == userId)
                             || (!string.IsNullOrEmpty(p.email) && !string.IsNullOrEmpty(correoDoc) && p.email.Trim().ToLower() == correoDoc)
                             || (!string.IsNullOrEmpty(p.nombre) && !string.IsNullOrEmpty(p.apellido) &&
                                 ($"{p.nombre.Trim()} {p.apellido.Trim()}".ToLower() == nombreCompletoDoc ||
                                  (nombresDoc.Contains(p.nombre.Trim().ToLower()) && apellidosDoc.Contains(p.apellido.Trim().ToLower())))))
                    .Select(p => p.id_profesor)
                    .ToList();

                if (userId.HasValue) idsProfesor.Add(userId.Value);
                idsProfesor = idsProfesor.Distinct().ToList();

                // Cursos asignados en HORARIOS para este profesor
                var cursoIdsHorario = _context.HORARIOS
                    .Where(h => idsProfesor.Contains(h.id_profesor))
                    .Select(h => h.id_curso)
                    .Distinct()
                    .ToList();

                if (cursoIdsHorario.Any())
                {
                    grados = listaCursos
                        .Where(c => cursoIdsHorario.Contains(c.id_curso))
                        .Select(c => c.nombre_curso)
                        .Distinct()
                        .OrderBy(n => {
                            var digits = new string(n.TakeWhile(char.IsDigit).ToArray());
                            return int.TryParse(digits, out int num) ? num : 999999;
                        })
                        .ThenBy(n => n)
                        .ToList();
                }
                else
                {
                    grados = new List<string>();
                }

                // Materias asignadas en HORARIOS y PROFESOR_MATERIA
                var materiaIdsHorario = _context.HORARIOS
                    .Where(h => idsProfesor.Contains(h.id_profesor))
                    .Select(h => h.id_materia)
                    .Distinct()
                    .ToList();

                var materiaIdsProfMateria = _context.PROFESOR_MATERIA
                    .Where(pm => idsProfesor.Contains(pm.id_profesor))
                    .Select(pm => pm.id_materia)
                    .Distinct()
                    .ToList();

                var todasMateriasIds = materiaIdsHorario.Union(materiaIdsProfMateria).Distinct().ToList();

                if (todasMateriasIds.Any())
                {
                    asignaturas = _context.MATERIA
                        .Where(m => todasMateriasIds.Contains(m.id_materia))
                        .Select(m => m.nombre_materia)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();
                }
                else
                {
                    asignaturas = new List<string>();
                }
            }

            _context.AsegurarEsquemaPeriodosSubidaNotas();
            var periodos = _context.PeriodosSubidaNotas.OrderBy(p => p.Periodo).ToList();
            if (!periodos.Any())
            {
                periodos = new List<PeriodoSubidaNota>
                {
                    new PeriodoSubidaNota { Periodo = 1, Nombre = "Primer Periodo Académico", Activo = true },
                    new PeriodoSubidaNota { Periodo = 2, Nombre = "Segundo Periodo Académico", Activo = false },
                    new PeriodoSubidaNota { Periodo = 3, Nombre = "Tercer Periodo Académico", Activo = false },
                    new PeriodoSubidaNota { Periodo = 4, Nombre = "Cuarto Periodo Académico", Activo = false }
                };
            }
            ViewBag.Periodos = periodos;
            ViewBag.PeriodoActivo = (periodos.FirstOrDefault(p => p.Activo) ?? periodos.First()).Periodo;

            _context.AsegurarEsquemaAccionesMejora();
            var mejorasActivas = _context.AccionesMejora.Where(a => a.Activo).ToList();
            ViewBag.AccionesMejora = mejorasActivas;

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
        [RequierePermiso(Permisos.GestionarClases)]
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

                _context.AsegurarEsquemaPeriodosSubidaNotas();
                _context.AsegurarLimpiezaCalificacionesDuplicadas();
                var pConfig = _context.PeriodosSubidaNotas.FirstOrDefault(p => p.Periodo == periodoVal);
                if (pConfig != null && !pConfig.Activo)
                {
                    TempData["Error"] = $"La subida de calificaciones para el Periodo {periodoVal} se encuentra actualmente CERRADA por Coordinación Académica.";
                    return RedirectToAction("Planilla");
                }

                // 1. Procesamiento de nota enviada de manera individual
                if (ID_Estudiante.HasValue && !string.IsNullOrEmpty(Nota))
                {
                    decimal notaDecimal = decimal.Parse(Nota.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                    if (notaDecimal >= 0 && notaDecimal <= 5)
                    {
                        var notaExistente = _context.Calificaciones
                            .FirstOrDefault(c => c.ID_Estudiante == ID_Estudiante.Value && c.Materia == materiaSel && c.Periodo == periodoVal);

                        if (notaExistente != null)
                        {
                            notaExistente.Nota = notaDecimal;
                            notaExistente.FechaRegistro = DateTime.Now;
                        }
                        else
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
                                    var califExistente = _context.Calificaciones
                                        .FirstOrDefault(c => c.ID_Estudiante == estId && c.Materia == materiaSel && c.Periodo == periodoVal);

                                    if (califExistente != null)
                                    {
                                        califExistente.Nota = dNota;
                                        califExistente.FechaRegistro = DateTime.Now;
                                    }
                                    else
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
                }

                // Persistencia de los cambios en la base de datos
                _context.SaveChanges();
                _context.AsegurarLimpiezaCalificacionesDuplicadas();

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
        /// Obtiene los logros activos para un curso/grado, materia y período específico.
        /// </summary>
        [HttpGet]
        public JsonResult ObtenerLogros(string grado, string materia, int periodo = 1)
        {
            _context.AsegurarEsquemaLogros();

            int numGrado = MatriculaAcademicaService.ExtraerGrado(grado, 0);
            string matBase = (materia ?? "").Split(" - ")[0].Trim();

            if (numGrado < 6 || numGrado > 11 || string.IsNullOrEmpty(matBase))
            {
                return Json(new { success = true, grado = numGrado, logros = new List<object>() });
            }

            var logrosBase = _context.Logros
                .Where(l => l.activo && l.grado == numGrado && l.periodo == periodo)
                .ToList();

            // 1. Buscar primero coincidencia exacta con el nombre de la asignatura
            var filtrados = logrosBase
                .Where(l => l.materia.Equals(matBase, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 2. Solo si no hay exactos, buscar por prefijo
            if (!filtrados.Any())
            {
                filtrados = logrosBase
                    .Where(l => l.materia.StartsWith(matBase, StringComparison.OrdinalIgnoreCase) ||
                                matBase.StartsWith(l.materia, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var logros = filtrados
                .OrderBy(l => l.id_logro)
                .Select(l => new
                {
                    id_logro = l.id_logro,
                    grado = l.grado,
                    materia = l.materia,
                    periodo = l.periodo,
                    codigo_logro = l.codigo_logro ?? "",
                    descripcion = l.descripcion
                })
                .ToList();

            return Json(new { success = true, grado = numGrado, logros });
        }

        /// <summary>
        /// Crea o actualiza un logro académico para un grado, materia y período.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public JsonResult GuardarLogro(int? id_logro, string grado, string materia, int periodo, string? codigo_logro, string descripcion)
        {
            try
            {
                _context.AsegurarEsquemaLogros();

                int numGrado = MatriculaAcademicaService.ExtraerGrado(grado, 0);
                string matBase = (materia ?? "").Split(" - ")[0].Trim();

                if (numGrado < 6 || numGrado > 11)
                    return Json(new { success = false, message = "Selecciona primero un curso válido (6° a 11°)." });

                if (string.IsNullOrWhiteSpace(matBase))
                    return Json(new { success = false, message = "Selecciona una asignatura." });

                if (string.IsNullOrWhiteSpace(descripcion))
                    return Json(new { success = false, message = "La descripción del logro es obligatoria." });

                if (id_logro.HasValue && id_logro.Value > 0)
                {
                    var existente = _context.Logros.Find(id_logro.Value);
                    if (existente == null)
                        return Json(new { success = false, message = "No se encontró el logro a editar." });

                    existente.codigo_logro = string.IsNullOrWhiteSpace(codigo_logro) ? existente.codigo_logro : codigo_logro.Trim();
                    existente.descripcion = descripcion.Trim();
                    existente.periodo = periodo;
                }
                else
                {
                    string prefijo = new string(matBase.Where(char.IsLetter).Take(3).ToArray()).ToUpper();
                    var nuevo = new Logro
                    {
                        grado = numGrado,
                        materia = matBase,
                        periodo = periodo,
                        codigo_logro = string.IsNullOrWhiteSpace(codigo_logro) ? $"LOG-{numGrado}-{prefijo}-P{periodo}" : codigo_logro.Trim(),
                        descripcion = descripcion.Trim(),
                        activo = true
                    };
                    _context.Logros.Add(nuevo);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Logro académico guardado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar el logro: " + ex.Message });
            }
        }

        /// <summary>
        /// Desactiva un logro académico.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public JsonResult EliminarLogro(int id_logro)
        {
            try
            {
                var logro = _context.Logros.Find(id_logro);
                if (logro == null)
                    return Json(new { success = false, message = "Logro no encontrado." });

                logro.activo = false;
                _context.SaveChanges();
                return Json(new { success = true, message = "Logro eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "No se pudo eliminar el logro: " + ex.Message });
            }
        }

        /// <summary>
        /// Genera la vista de reporte general de calificaciones por estudiante, permitiendo generar certificados académicos.
        /// </summary>
        [RequierePermiso(Permisos.ConsultarSeguimiento)]
        public IActionResult ReporteGrupal(string? curso, int? periodo, string? busqueda, string? materia, bool buscar = false)
        {
            _context.AsegurarLimpiezaCalificacionesDuplicadas();

            var cursosDb = _context.Cursos.ToList();
            ViewBag.ListaCursos = cursosDb.Select(c => c.nombre_curso).Distinct().OrderBy(c => c).ToList();
            ViewBag.CursoSeleccionado = curso;
            ViewBag.PeriodoSeleccionado = periodo;
            ViewBag.BusquedaSeleccionada = busqueda;
            ViewBag.MateriaSeleccionada = materia;

            // Antes de aplicar los filtros, no se debe mostrar nada en la vista (RF Reportes Generales)
            bool hayFiltro = buscar || !string.IsNullOrWhiteSpace(curso) || periodo.HasValue || !string.IsNullOrWhiteSpace(busqueda) || !string.IsNullOrWhiteSpace(materia);
            ViewBag.FiltroAplicado = hayFiltro;

            if (!hayFiltro)
            {
                return View(new List<dynamic>());
            }

            // Mapear curso asignado a estudiantes
            var estudiantesEst = _context.ESTUDIANTE.ToList();
            var matriculas = _context.Matriculas.ToList();
            var estudianteCursos = new Dictionary<int, string>();

            foreach (var est in estudiantesEst)
            {
                if (est.id_usuario.HasValue)
                {
                    if (!string.IsNullOrEmpty(est.curso_asignado))
                    {
                        estudianteCursos[est.id_usuario.Value] = est.curso_asignado.Trim();
                    }
                    else
                    {
                        var mat = matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante && (m.estado == null || m.estado.ToLower().Contains("act")));
                        if (mat == null) mat = matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante);
                        if (mat != null)
                        {
                            var cObj = cursosDb.FirstOrDefault(c => c.id_curso == mat.id_curso);
                            if (cObj != null) estudianteCursos[est.id_usuario.Value] = cObj.nombre_curso.Trim();
                        }
                    }
                }
            }

            // Consultamos los estudiantes activos
            var estudiantesQuery = _context.Usuarios
                .Where(u => u.ROL != null && u.ROL.Trim().ToLower() == "estudiante" && u.ACTIVO);

            // Filtro por búsqueda (nombre o documento de identidad de algún estudiante)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string b = busqueda.Trim().ToLower();
                estudiantesQuery = estudiantesQuery.Where(u =>
                    (u.NOMBRES != null && u.NOMBRES.ToLower().Contains(b)) ||
                    (u.APELLIDOS != null && u.APELLIDOS.ToLower().Contains(b)) ||
                    (u.NUM_DOCUMENTO != null && u.NUM_DOCUMENTO.ToLower().Contains(b)) ||
                    (u.NOMBRE_USUARIO != null && u.NOMBRE_USUARIO.ToLower().Contains(b))
                );
            }

            var listaEstudiantes = estudiantesQuery.ToList();

            // Filtrar estudiantes por curso si se seleccionó curso
            if (!string.IsNullOrWhiteSpace(curso) && curso.ToLower() != "todos")
            {
                string cFiltro = curso.Trim().ToLower();
                listaEstudiantes = listaEstudiantes.Where(u => {
                    string rc = (estudianteCursos.TryGetValue(u.ID_Usuario, out var cNom) ? cNom : "Sin Curso").Trim().ToLower();
                    return rc == cFiltro || rc.Replace(" ", "") == cFiltro.Replace(" ", "") || rc.Contains(cFiltro) || cFiltro.Contains(rc);
                }).ToList();
            }

            // Obtener todas las calificaciones de los estudiantes seleccionados
            var idsEstudiantes = listaEstudiantes.Select(u => u.ID_Usuario).ToList();
            var calificacionesQuery = _context.Calificaciones
                .Where(c => idsEstudiantes.Contains(c.ID_Estudiante));

            if (periodo.HasValue && periodo.Value > 0)
            {
                calificacionesQuery = calificacionesQuery.Where(c => c.Periodo == periodo.Value);
            }

            if (!string.IsNullOrWhiteSpace(materia))
            {
                calificacionesQuery = calificacionesQuery.Where(c => c.Materia.ToLower().Contains(materia.Trim().ToLower()));
            }

            var todasCalificaciones = calificacionesQuery.ToList();

            // Si se filtró por periodo y no por estudiante/curso específico, filtrar para mostrar estudiantes con calificaciones
            if (periodo.HasValue && periodo.Value > 0 && string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(curso))
            {
                var idsConCalif = todasCalificaciones.Select(c => c.ID_Estudiante).Distinct().ToHashSet();
                listaEstudiantes = listaEstudiantes.Where(u => idsConCalif.Contains(u.ID_Usuario)).ToList();
            }

            // Construir el reporte: EXACTAMENTE UN REGISTRO POR ESTUDIANTE
            var reporte = new List<dynamic>();

            foreach (var est in listaEstudiantes)
            {
                // Agrupar calificaciones por Materia y Periodo para deduplicar y tomar la última nota
                var notasEstudiante = todasCalificaciones
                    .Where(c => c.ID_Estudiante == est.ID_Usuario)
                    .GroupBy(c => new { MateriaNorm = (c.Materia ?? "").Trim().ToLower(), c.Periodo })
                    .Select(g => g.OrderByDescending(x => x.ID_Calificacion).First())
                    .ToList();

                // Si se filtró por materia o periodo y no tiene calificaciones, y no fue búsqueda puntual, omitir
                if ((!string.IsNullOrWhiteSpace(materia) || (periodo.HasValue && periodo.Value > 0 && string.IsNullOrWhiteSpace(curso) && string.IsNullOrWhiteSpace(busqueda))) && !notasEstudiante.Any())
                {
                    continue;
                }

                string cursoEst = estudianteCursos.TryGetValue(est.ID_Usuario, out var cNom) ? cNom : "Sin Curso";
                double promedio = notasEstudiante.Any() ? Math.Round(notasEstudiante.Average(n => (double)n.Nota), 1) : 0.0;
                string periodoTexto = periodo.HasValue && periodo.Value > 0 ? $"{periodo.Value}°" : (notasEstudiante.Any() ? string.Join(", ", notasEstudiante.Select(n => $"{n.Periodo}°").Distinct()) : "1°");

                reporte.Add(new {
                    IdEstudiante = est.ID_Usuario,
                    NombreEstudiante = $"{est.NOMBRES} {est.APELLIDOS}".Trim(),
                    Documento = est.NUM_DOCUMENTO ?? est.NOMBRE_USUARIO,
                    Curso = cursoEst,
                    Periodo = periodoTexto,
                    Promedio = promedio,
                    TotalMaterias = notasEstudiante.Count,
                    TieneNotas = notasEstudiante.Any(),
                    Aprobado = promedio >= 3.0
                });
            }

            return View(reporte.OrderBy(r => r.Curso).ThenBy(r => r.NombreEstudiante).ToList<dynamic>());
        }

        /// <summary>
        /// <summary>
        /// Genera y descarga el reporte del observador en formato PDF usando Rotativa con el formato institucional Sor María Juliana.
        /// </summary>
        [RequierePermiso(Permisos.ConsultarSeguimiento)]
        public IActionResult DescargarObservadorPdf(int? idEstudiante, string? curso)
        {
            try
            {
                if (!idEstudiante.HasValue || idEstudiante.Value <= 0)
                {
                    return Content(@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8' />
    <title>Selección Requerida - Observador</title>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' />
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css' />
</head>
<body class='bg-light d-flex align-items-center justify-content-center' style='min-height: 100vh;'>
    <div class='card shadow-sm border-0 p-4 text-center' style='max-width: 480px; border-radius: 16px; background: white;'>
        <div class='mb-3 text-warning'><i class='bi bi-exclamation-circle-fill' style='font-size: 3.5rem;'></i></div>
        <h4 class='fw-bold text-dark mb-2'>Estudiante No Seleccionado</h4>
        <p class='text-muted small mb-4'>Para generar y descargar el reporte oficial del Observador del Alumno en PDF, primero debes seleccionar un estudiante en el filtro del panel.</p>
        <button class='btn text-white px-4 py-2 rounded-pill shadow-sm' style='background: #0d2a4a;' onclick='window.close()'>
            <i class='bi bi-x-circle me-1'></i> Cerrar Ventana
        </button>
    </div>
</body>
</html>", "text/html");
                }

                _context.AsegurarEsquemaObservador();
                _context.AsegurarEsquemaEstudiantes();

                Usuario? userEst = null;
                Estudiante? estLegacy = null;

                if (idEstudiante.HasValue && idEstudiante.Value > 0)
                {
                    userEst = _context.Usuarios.FirstOrDefault(u => u.ID_Usuario == idEstudiante.Value);
                    estLegacy = _context.ESTUDIANTE.FirstOrDefault(e => e.id_usuario == idEstudiante.Value || e.id_estudiante == idEstudiante.Value);
                    if (userEst == null && estLegacy != null && estLegacy.id_usuario.HasValue)
                    {
                        userEst = _context.Usuarios.Find(estLegacy.id_usuario.Value);
                    }
                }

                if (userEst == null)
                {
                    userEst = _context.Usuarios.FirstOrDefault(u => u.ROL != null && u.ROL.ToLower() == "estudiante" && u.ACTIVO);
                    if (userEst != null)
                    {
                        estLegacy = _context.ESTUDIANTE.FirstOrDefault(e => e.id_usuario == userEst.ID_Usuario || (e.email != null && e.email.ToLower() == (userEst.CORREO_ELECTRONICO ?? "").ToLower()));
                    }
                }

                if (userEst == null && estLegacy == null)
                {
                    return Content(@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8' />
    <title>Estudiante No Encontrado - Observador</title>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' />
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css' />
</head>
<body class='bg-light d-flex align-items-center justify-content-center' style='min-height: 100vh;'>
    <div class='card shadow-sm border-0 p-4 text-center' style='max-width: 480px; border-radius: 16px; background: white;'>
        <div class='mb-3 text-warning'><i class='bi bi-person-x-fill' style='font-size: 3.5rem;'></i></div>
        <h4 class='fw-bold text-dark mb-2'>Estudiante No Encontrado</h4>
        <p class='text-muted small mb-4'>No se encontró la ficha o información del estudiante indicado para generar el reporte.</p>
        <button class='btn text-white px-4 py-2 rounded-pill shadow-sm' style='background: #0d2a4a;' onclick='window.close()'>
            <i class='bi bi-x-circle me-1'></i> Cerrar Ventana
        </button>
    </div>
</body>
</html>", "text/html");
                }

                var vm = new ObservadorPdfViewModel
                {
                    Institucion = "INSTITUCIÓN EDUCATIVA SOR MARÍA JULIANA",
                    Sede = "SEDE: SOR MARÍA JULIANA",
                    Titulo = "OBSERVADOR DEL ALUMNO",
                    Ciudad = "Cartago",
                    Jornada = "Mañana",
                    Grupo = !string.IsNullOrEmpty(curso) ? curso : "601 M",
                    AnoLectivo = DateTime.Now.Year.ToString(),
                    DirGrupo = "Claudia Milena Londoño C.",
                    Calendario = "A",
                    EstudianteNombre = userEst != null ? $"{userEst.NOMBRES} {userEst.APELLIDOS}".Trim() : (estLegacy != null ? $"{estLegacy.nombre} {estLegacy.apellido}".Trim() : "Estudiante Institucional"),
                    Codigo = estLegacy?.codigo_estudiante ?? (userEst != null ? $"EST-{userEst.ID_Usuario:D4}" : "04644"),
                    MatriculaNo = estLegacy != null ? $"{estLegacy.id_estudiante:D4}" : "4644",
                    LugarNacimiento = "Cartago",
                    FechaNacimiento = estLegacy != null && estLegacy.fecha_inscripcion != default ? estLegacy.fecha_inscripcion.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES")) : "14 de Julio de 2008",
                    Identificacion = userEst?.NUM_DOCUMENTO != null ? $"T.I. {userEst.NUM_DOCUMENTO} de Cartago" : "T.I. 9507141911 de Cartago",
                    Acudiente = "LUIS MARTINEZ",
                    IdentificacionAcudiente = "C.C. 10.123.456",
                    Direccion = userEst?.DIRECCION ?? (estLegacy != null && !string.IsNullOrEmpty(estLegacy.email) ? "Carrera 19C No. 14 - 20" : "Carrera 19C No. 14 - 20"),
                    Telefonos = userEst?.TELEFONO ?? "314 41 64"
                };

                // Buscar matrícula para grupo y jornada reales
                if (estLegacy != null)
                {
                    var mat = _context.Matriculas.FirstOrDefault(m => m.id_estudiante == estLegacy.id_estudiante);
                    if (mat != null)
                    {
                        var cursoObj = _context.Cursos.FirstOrDefault(c => c.id_curso == mat.id_curso);
                        if (cursoObj != null)
                        {
                            vm.Grupo = cursoObj.nombre_curso;
                            if (cursoObj.nombre_curso.Contains(" T") || cursoObj.nombre_curso.ToLower().Contains("tarde"))
                                vm.Jornada = "Tarde";
                        }
                        if (mat.ano > 0) vm.AnoLectivo = mat.ano.ToString();
                    }
                }

                // Cargar observaciones registradas para el estudiante
                int estIdQuery = userEst?.ID_Usuario ?? (estLegacy?.id_estudiante ?? 0);
                int estIdLegacy = estLegacy?.id_estudiante ?? 0;

                var listaObs = _context.ObservacionesEstudiante
                    .Where(o => o.id_estudiante == estIdQuery || (estIdLegacy > 0 && o.id_estudiante == estIdLegacy))
                    .OrderBy(o => o.fecha)
                    .ToList();

                var periodosNombres = new[] { "PRIMER PERIODO", "SEGUNDO PERIODO", "TERCER PERIODO", "CUARTO PERIODO" };

                for (int p = 1; p <= 4; p++)
                {
                    var obsPeriodo = listaObs.Where(o => o.periodo == p || (o.periodo == 0 && p == 1)).ToList();
                    var pItem = new ObservadorPeriodoItem
                    {
                        NombrePeriodo = periodosNombres[p - 1]
                    };

                    foreach (var o in obsPeriodo)
                    {
                        pItem.Observaciones.Add(new ObservacionDetalleItem
                        {
                            Fecha = o.fecha.ToString("MMMM dd 'de' yyyy", new System.Globalization.CultureInfo("es-ES")),
                            TipoNota = o.tipo_nota ?? "Observación",
                            Descripcion = o.descripcion,
                            AspectosMejorar = o.aspectos_mejorar,
                            FirmaDocente = !string.IsNullOrEmpty(o.quien_registra) ? o.quien_registra : (!string.IsNullOrEmpty(o.docente) ? o.docente : "cml")
                        });
                    }

                    vm.Periodos.Add(pItem);
                }

                return new ViewAsPdf("ReporteObservadorPdf", vm)
                {
                    FileName = $"Observador_{vm.EstudianteNombre.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                    PageMargins = new Rotativa.AspNetCore.Options.Margins(12, 10, 12, 10)
                };
            }
            catch (Exception)
            {
                return Content(@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8' />
    <title>Aviso - Observador</title>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' />
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css' />
</head>
<body class='bg-light d-flex align-items-center justify-content-center' style='min-height: 100vh;'>
    <div class='card shadow-sm border-0 p-4 text-center' style='max-width: 520px; border-radius: 16px; background: white;'>
        <div class='mb-3 text-danger'><i class='bi bi-file-earmark-x-fill' style='font-size: 3.5rem;'></i></div>
        <h4 class='fw-bold text-dark mb-2'>No se pudo generar el Observador en PDF</h4>
        <p class='text-muted small mb-3'>Ocurrió una eventualidad al compilar el documento del observador institucional.</p>
        <div class='alert alert-light border py-2 px-3 small text-start mb-4' style='font-size: 0.8rem;'>
            <strong>Recomendación:</strong> Verifique que el estudiante seleccionado cuente con matrícula activa o datos básicos registrados en el sistema.
        </div>
        <button class='btn text-white px-4 py-2 rounded-pill shadow-sm' style='background: #0d2a4a;' onclick='window.close()'>
            <i class='bi bi-arrow-left me-1'></i> Regresar y Cerrar
        </button>
    </div>
</body>
</html>", "text/html");
            }
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
        [RequierePermiso(Permisos.ConsultarSeguimiento)]
        public IActionResult ObtenerInscritosCursoExtra(int idCurso)
        {
            _context.AsegurarEsquemaExtracurriculares();

            var inscripciones = _context.ExtraInscripciones
                .Where(i => i.IdExtraCurso == idCurso && (i.Estado == "Inscrito" || i.Estado == "PendienteAprobacion"))
                .ToList();

            var docsSubidos = _context.ExtraDocumentosInscripciones.ToList();

            var inscritos = inscripciones
                .Join(_context.Usuarios,
                      i => i.IdEstudiante,
                      u => u.ID_Usuario,
                      (i, u) => new
                      {
                          idInscripcion = i.IdExtraInscripcion,
                          idEstudiante = u.ID_Usuario,
                          nombreCompleto = (u.NOMBRES + " " + u.APELLIDOS).Trim(),
                          documento = u.NUM_DOCUMENTO ?? u.NOMBRE_USUARIO,
                          correo = u.CORREO_ELECTRONICO ?? "-",
                          grado = u.COLEGIO_PROCEDENCIA ?? "Estudiante",
                          fechaInscripcion = i.FechaInscripcion.ToString("dd/MM/yyyy HH:mm"),
                          estado = i.Estado,
                          documentos = docsSubidos.Where(d => d.IdExtraInscripcion == i.IdExtraInscripcion)
                              .Select(d => new { d.TipoDocumento, d.ArchivoNombre, d.ArchivoRuta, d.Estado, FechaSubida = d.FechaSubida.ToString("dd/MM/yyyy") })
                              .ToList()
                      })
                .OrderBy(e => e.nombreCompleto)
                .ToList();

            var curso = _context.ExtraCursos.Find(idCurso);

            return Json(new
            {
                success = true,
                nombreCurso = curso?.Nombre ?? "Curso Extracurricular",
                tipoCurso = curso?.TipoCurso ?? "Deportivo",
                cuposTotales = curso?.CuposTotales ?? 0,
                cuposDisponibles = curso?.CuposDisponibles ?? 0,
                totalInscritos = inscritos.Count(e => e.estado == "Inscrito"),
                totalPendientes = inscritos.Count(e => e.estado == "PendienteAprobacion"),
                estudiantes = inscritos,
            });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult AprobarInscripcionExtra(int idInscripcion)
        {
            _context.AsegurarEsquemaExtracurriculares();

            var insc = _context.ExtraInscripciones.Find(idInscripcion);
            if (insc == null)
                return Json(new { success = false, message = "Inscripción no encontrada." });

            if (insc.Estado != "PendienteAprobacion")
                return Json(new { success = false, message = "Esta inscripción no está pendiente de aprobación." });

            // Aprobar: cambiar estado y descontar cupo
            var curso = _context.ExtraCursos.Find(insc.IdExtraCurso);
            if (curso != null && curso.CuposDisponibles > 0)
            {
                curso.CuposDisponibles -= 1;
            }

            insc.Estado = "Inscrito";
            _context.SaveChanges();

            return Json(new { success = true, message = "Inscripción aprobada exitosamente. El estudiante ha sido inscrito." });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult RechazarInscripcionExtra(int idInscripcion, string? motivo)
        {
            _context.AsegurarEsquemaExtracurriculares();

            var insc = _context.ExtraInscripciones.Find(idInscripcion);
            if (insc == null)
                return Json(new { success = false, message = "Inscripción no encontrada." });

            // El cupo NO se descuenta en PendienteAprobacion, no hay nada que restituir
            insc.Estado = "Cancelado";
            insc.FechaCancelacion = DateTime.Now;
            insc.MotivoCancelacion = !string.IsNullOrWhiteSpace(motivo) ? motivo : "Rechazado por el docente";
            _context.SaveChanges();

            return Json(new { success = true, message = "Solicitud rechazada. El estudiante ha sido notificado." });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
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
        [RequierePermiso(Permisos.GestionarClases)]
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

        // ==========================================
        // GESTIÓN DE ACTIVIDADES ACADÉMICAS (DOCENTE)
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> CrearActividad(Actividad model, IFormFile? archivo)
        {
            _context.AsegurarEsquemaActividades();

            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();
            if (rol != "docente" && rol != "admin" && rol != "profesor")
            {
                return Json(new { success = false, message = "No tienes permisos para crear actividades académicas." });
            }

            if (string.IsNullOrWhiteSpace(model.Titulo))
            {
                return Json(new { success = false, message = "El título de la actividad es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(model.Materia))
            {
                return Json(new { success = false, message = "La asignatura o materia es obligatoria." });
            }

            if (model.FechaLimite == default)
            {
                model.FechaLimite = DateTime.Now.AddDays(7);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Tu sesión ha expirado. Por favor ingresa nuevamente al sistema." });
            }

            model.Titulo = model.Titulo.Trim();
            model.Materia = model.Materia.Trim();
            model.Grado = string.IsNullOrWhiteSpace(model.Grado) ? "Todos" : model.Grado.Trim();
            model.IdDocente = userId.Value;
            model.FechaCreacion = DateTime.Now;
            model.Activo = true;

            // Procesar archivo adjunto del docente (guía, taller, rúbrica) si se adjunta
            if (archivo != null && archivo.Length > 0)
            {
                var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(ext))
                {
                    return Json(new { success = false, message = "Tipo de archivo no permitido para la guía de la actividad." });
                }

                if (archivo.Length > 25 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "El archivo supera el tamaño máximo permitido de 25 MB." });
                }

                // Guardado consecutivo clasificado mediante GestorDocumentalService en wwwroot/uploads/actividades/
                var docRes = await _gestorDocumental.GuardarArchivoAsync(
                    archivo: archivo,
                    subcarpeta: "actividades",
                    idUsuario: userId ?? 1,
                    esCriticoMenor: false,
                    tipoEtiqueta: "Guía de Actividad"
                );

                model.ArchivoAdjunto = docRes.RutaRelativa;
            }

            _context.Actividades.Add(model);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "¡Actividad académica creada y publicada con éxito!",
                actividad = new
                {
                    idActividad = model.IdActividad,
                    titulo = model.Titulo,
                    descripcion = model.Descripcion,
                    materia = model.Materia,
                    grado = model.Grado,
                    fechaLimite = model.FechaLimite.ToString("dd/MM/yyyy HH:mm"),
                    fechaLimiteIso = model.FechaLimite.ToString("o"),
                    archivoAdjunto = model.ArchivoAdjunto,
                    totalEntregas = 0,
                    calificadas = 0,
                    pendientes = 0,
                    estaVencida = DateTime.Now > model.FechaLimite,
                    esMejora = model.EsMejora
                }
            });
        }

        [HttpGet]
        public IActionResult ObtenerActividadPorId(int idActividad)
        {
            _context.AsegurarEsquemaActividades();
            var act = _context.Actividades.Find(idActividad);
            if (act == null || !act.Activo)
            {
                return Json(new { success = false, message = "Actividad no encontrada." });
            }

            return Json(new
            {
                success = true,
                actividad = new
                {
                    idActividad = act.IdActividad,
                    titulo = act.Titulo,
                    descripcion = act.Descripcion,
                    materia = act.Materia,
                    grado = act.Grado,
                    fechaLimite = act.FechaLimite.ToString("yyyy-MM-ddTHH:mm"),
                    archivoAdjunto = act.ArchivoAdjunto,
                    esMejora = act.EsMejora
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditarActividad(Actividad model, IFormFile? archivo)
        {
            _context.AsegurarEsquemaActividades();

            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();
            if (rol != "docente" && rol != "admin" && rol != "profesor")
            {
                return Json(new { success = false, message = "No tienes permisos para modificar actividades académicas." });
            }

            var actividad = await _context.Actividades.FindAsync(model.IdActividad);
            if (actividad == null || !actividad.Activo)
            {
                return Json(new { success = false, message = "La actividad no existe o fue eliminada." });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (rol != "admin" && actividad.IdDocente.HasValue && actividad.IdDocente.Value != userId)
            {
                return Json(new { success = false, message = "Solo el docente creador o un administrador puede editar esta actividad." });
            }

            if (string.IsNullOrWhiteSpace(model.Titulo))
            {
                return Json(new { success = false, message = "El título de la actividad es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(model.Materia))
            {
                return Json(new { success = false, message = "La asignatura o materia es obligatoria." });
            }

            actividad.Titulo = model.Titulo.Trim();
            actividad.Descripcion = model.Descripcion;
            actividad.Materia = model.Materia.Trim();
            actividad.Grado = string.IsNullOrWhiteSpace(model.Grado) ? "Todos" : model.Grado.Trim();
            if (model.FechaLimite != default)
            {
                actividad.FechaLimite = model.FechaLimite;
            }
            actividad.EsMejora = model.EsMejora;

            if (archivo != null && archivo.Length > 0)
            {
                var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(ext))
                {
                    return Json(new { success = false, message = "Tipo de archivo no permitido para la guía de la actividad." });
                }

                if (archivo.Length > 25 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "El archivo supera el tamaño máximo permitido de 25 MB." });
                }

                // Guardado consecutivo clasificado mediante GestorDocumentalService en wwwroot/uploads/actividades/
                var docRes = await _gestorDocumental.GuardarArchivoAsync(
                    archivo: archivo,
                    subcarpeta: "actividades",
                    idUsuario: userId ?? 1,
                    esCriticoMenor: false,
                    tipoEtiqueta: "Guía de Actividad (Actualización)"
                );

                actividad.ArchivoAdjunto = docRes.RutaRelativa;
            }

            await _context.SaveChangesAsync();

            var entregas = _context.EntregasActividades.Where(e => e.IdActividad == actividad.IdActividad).ToList();
            int totalEntregas = entregas.Count;
            int calificadas = entregas.Count(e => e.Calificacion.HasValue);
            int pendientes = totalEntregas - calificadas;

            return Json(new
            {
                success = true,
                message = "¡Actividad académica actualizada con éxito!",
                actividad = new
                {
                    idActividad = actividad.IdActividad,
                    titulo = actividad.Titulo,
                    descripcion = actividad.Descripcion,
                    materia = actividad.Materia,
                    grado = actividad.Grado,
                    fechaLimite = actividad.FechaLimite.ToString("dd/MM/yyyy HH:mm"),
                    fechaLimiteIso = actividad.FechaLimite.ToString("o"),
                    archivoAdjunto = actividad.ArchivoAdjunto,
                    totalEntregas = totalEntregas,
                    calificadas = calificadas,
                    pendientes = pendientes,
                    estaVencida = DateTime.Now > actividad.FechaLimite,
                    esMejora = actividad.EsMejora
                }
            });
        }

        [HttpGet]
        public IActionResult ObtenerActividadesDocente()
        {
            _context.AsegurarEsquemaActividades();

            var userId = HttpContext.Session.GetInt32("UserId");
            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();

            var usr = _context.Usuarios.Find(userId);
            var prof = _context.PROFESOR.FirstOrDefault(p => (p.id_usuario != null && p.id_usuario == userId) ||
                (usr != null && !string.IsNullOrEmpty(usr.CORREO_ELECTRONICO) && p.email != null && p.email.ToLower() == usr.CORREO_ELECTRONICO.ToLower()));
            int? profId = prof?.id_profesor;

            var actividades = _context.Actividades
                .Where(a => a.Activo && (rol == "admin" || a.IdDocente == userId || (profId != null && a.IdDocente == profId) || a.IdDocente == null))
                .OrderByDescending(a => a.FechaCreacion)
                .ToList();

            var todasEntregas = _context.EntregasActividades.ToList();

            var resultado = actividades.Select(a =>
            {
                var entregas = todasEntregas.Where(e => e.IdActividad == a.IdActividad).ToList();
                int totalEntregas = entregas.Count;
                int calificadas = entregas.Count(e => e.Calificacion.HasValue);
                int pendientes = totalEntregas - calificadas;

                return new
                {
                    idActividad = a.IdActividad,
                    titulo = a.Titulo,
                    descripcion = a.Descripcion,
                    materia = a.Materia,
                    grado = string.IsNullOrWhiteSpace(a.Grado) ? "Todos" : a.Grado.Trim(),
                    fechaLimite = a.FechaLimite.ToString("dd/MM/yyyy HH:mm"),
                    fechaLimiteIso = a.FechaLimite.ToString("o"),
                    archivoAdjunto = a.ArchivoAdjunto,
                    totalEntregas = totalEntregas,
                    calificadas = calificadas,
                    pendientes = pendientes,
                    estaVencida = DateTime.Now > a.FechaLimite,
                    esMejora = a.EsMejora
                };
            }).ToList();

            return Json(new { success = true, actividades = resultado });
        }

        [HttpGet]
        [RequierePermiso(Permisos.ConsultarSeguimiento)]
        public IActionResult ObtenerEntregasActividad(int idActividad)
        {
            _context.AsegurarEsquemaActividades();

            var actividad = _context.Actividades.Find(idActividad);
            if (actividad == null)
            {
                return Json(new { success = false, message = "Actividad no encontrada." });
            }

            var estudiantes = _context.Usuarios.Where(u => u.ROL == "estudiante" && u.ACTIVO).ToList();
            var estudiantesEst = _context.ESTUDIANTE.ToList();
            var matriculas = _context.Matriculas.ToList();
            var listaCursos = _context.Cursos.ToList();

            var estudianteGrados = new Dictionary<int, string>();
            foreach (var u in estudiantes)
            {
                var est = estudiantesEst.FirstOrDefault(e => (e.id_usuario != null && e.id_usuario == u.ID_Usuario) ||
                    (e.email != null && u.CORREO_ELECTRONICO != null && e.email.ToLower() == u.CORREO_ELECTRONICO.ToLower()));
                if (est != null)
                {
                    if (!string.IsNullOrEmpty(est.curso_asignado))
                    {
                        estudianteGrados[u.ID_Usuario] = est.curso_asignado.Trim();
                    }
                    else
                    {
                        var mat = matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante && (m.estado == null || m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo" || m.estado == ""));
                        if (mat == null) mat = matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante);
                        if (mat != null)
                        {
                            var cursoObj = listaCursos.FirstOrDefault(c => c.id_curso == mat.id_curso);
                            estudianteGrados[u.ID_Usuario] = (cursoObj != null ? cursoObj.nombre_curso : mat.id_curso.ToString()).Trim();
                        }
                    }
                }
            }

            // Filtrar estudiantes correspondientes al grado de la actividad (si no es 'Todos')
            string gradoAct = (actividad.Grado ?? "").Trim();
            bool esParaTodos = string.IsNullOrEmpty(gradoAct) || gradoAct.Equals("Todos", StringComparison.OrdinalIgnoreCase);

            var estudiantesCurso = estudiantes.Where(u =>
            {
                if (esParaTodos) return true;
                if (estudianteGrados.TryGetValue(u.ID_Usuario, out var cursoEst))
                {
                    if (string.Equals(cursoEst, gradoAct, StringComparison.OrdinalIgnoreCase)) return true;
                    string c1 = cursoEst.Replace(" ", "").ToLowerInvariant();
                    string c2 = gradoAct.Replace(" ", "").ToLowerInvariant();
                    if (c1 == c2 || c1.Contains(c2) || c2.Contains(c1)) return true;
                    var d1 = new string(c1.Where(char.IsDigit).ToArray());
                    var d2 = new string(c2.Where(char.IsDigit).ToArray());
                    if (!string.IsNullOrEmpty(d1) && !string.IsNullOrEmpty(d2) && d1 == d2) return true;
                }
                return false;
            }).ToList();

            var entregasExistentes = _context.EntregasActividades.Where(e => e.IdActividad == idActividad).ToList();
            var idsEstudiantesConEntrega = entregasExistentes.Select(e => e.IdEstudiante).Distinct().ToList();

            foreach (var idEstConEntrega in idsEstudiantesConEntrega)
            {
                if (!estudiantesCurso.Any(e => e.ID_Usuario == idEstConEntrega))
                {
                    var u = estudiantes.FirstOrDefault(x => x.ID_Usuario == idEstConEntrega);
                    if (u != null) estudiantesCurso.Add(u);
                }
            }

            if (!estudiantesCurso.Any())
            {
                estudiantesCurso = estudiantes;
            }

            var listaResultado = estudiantesCurso
                .OrderBy(u => u.APELLIDOS)
                .ThenBy(u => u.NOMBRES)
                .Select(u =>
                {
                    var entrega = entregasExistentes.FirstOrDefault(e => e.IdEstudiante == u.ID_Usuario);
                    bool haEntregado = (entrega != null);
                    bool calificado = (entrega != null && entrega.Calificacion.HasValue);
                    string cursoEst = estudianteGrados.ContainsKey(u.ID_Usuario) ? estudianteGrados[u.ID_Usuario] : (u.COLEGIO_PROCEDENCIA ?? "Estudiante");

                    return new
                    {
                        idEstudiante = u.ID_Usuario,
                        nombreEstudiante = $"{u.NOMBRES} {u.APELLIDOS}".Trim(),
                        documento = u.NUM_DOCUMENTO ?? u.NOMBRE_USUARIO,
                        correo = u.CORREO_ELECTRONICO ?? "-",
                        curso = cursoEst,
                        haEntregado = haEntregado,
                        idEntrega = entrega?.IdEntrega ?? 0,
                        fechaEntrega = entrega != null ? entrega.FechaEntrega.ToString("dd/MM/yyyy HH:mm") : null,
                        archivoRuta = entrega?.ArchivoRuta,
                        archivoNombre = entrega?.ArchivoNombre,
                        comentario = entrega?.Comentario,
                        calificacion = entrega?.Calificacion,
                        retroalimentacion = entrega?.Retroalimentacion,
                        estado = haEntregado ? (calificado ? "Calificado" : "Entregado") : "Pendiente"
                    };
                }).ToList();

            int totalAlumnos = listaResultado.Count;
            int totalEntregados = listaResultado.Count(x => x.haEntregado);
            int totalSinEntregar = totalAlumnos - totalEntregados;
            int totalCalificados = listaResultado.Count(x => x.calificacion.HasValue);
            int totalPorCalificar = totalEntregados - totalCalificados;

            return Json(new
            {
                success = true,
                actividad = new
                {
                    idActividad = actividad.IdActividad,
                    titulo = actividad.Titulo,
                    descripcion = actividad.Descripcion,
                    materia = actividad.Materia,
                    grado = string.IsNullOrWhiteSpace(actividad.Grado) ? "Todos" : actividad.Grado,
                    fechaLimite = actividad.FechaLimite.ToString("dd/MM/yyyy HH:mm"),
                    estaVencida = DateTime.Now > actividad.FechaLimite,
                    archivoAdjunto = actividad.ArchivoAdjunto
                },
                resumen = new
                {
                    totalAlumnos,
                    totalEntregados,
                    totalSinEntregar,
                    totalCalificados,
                    totalPorCalificar
                },
                alumnos = listaResultado,
                tituloActividad = actividad.Titulo,
                materia = actividad.Materia,
                grado = actividad.Grado ?? "Todos",
                totalEntregas = totalEntregados,
                entregas = listaResultado.Where(x => x.haEntregado).ToList()
            });
        }

        [HttpPost]
        public IActionResult CalificarEntrega(int idEntrega, string nota, string? retroalimentacion, int? periodo, int? idActividad, int? idEstudiante)
        {
            _context.AsegurarEsquemaActividades();

            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();
            if (rol != "docente" && rol != "admin" && rol != "profesor")
            {
                return Json(new { success = false, message = "No tienes permisos para calificar actividades." });
            }

            if (!decimal.TryParse((nota ?? "").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal notaDecimal))
            {
                return Json(new { success = false, message = "El valor de la calificación no es válido. Debe ser un número entre 0.0 y 5.0." });
            }

            if (notaDecimal < 0m || notaDecimal > 5.0m)
            {
                return Json(new { success = false, message = "La calificación debe estar entre 0.0 y 5.0." });
            }

            EntregaActividad? entrega = null;
            if (idEntrega > 0)
            {
                entrega = _context.EntregasActividades.Find(idEntrega);
            }
            else if (idActividad.HasValue && idEstudiante.HasValue)
            {
                entrega = _context.EntregasActividades.FirstOrDefault(e => e.IdActividad == idActividad.Value && e.IdEstudiante == idEstudiante.Value);
                if (entrega == null)
                {
                    entrega = new EntregaActividad
                    {
                        IdActividad = idActividad.Value,
                        IdEstudiante = idEstudiante.Value,
                        FechaEntrega = DateTime.Now,
                        Comentario = "Calificado directamente por el docente",
                        Estado = "Calificado"
                    };
                    _context.EntregasActividades.Add(entrega);
                    _context.SaveChanges();
                }
            }

            if (entrega == null)
            {
                return Json(new { success = false, message = "Entrega de actividad no encontrada." });
            }

            entrega.Calificacion = notaDecimal;
            entrega.Retroalimentacion = retroalimentacion?.Trim();
            entrega.Estado = "Calificado";

            // Reflejar la calificación automáticamente en la tabla oficial de Calificaciones (Planilla de Notas)
            var actividad = _context.Actividades.Find(entrega.IdActividad);
            if (actividad != null)
            {
                int periodoVal = periodo ?? 1;
                string materiaNombre = actividad.Materia;
                if (!string.IsNullOrEmpty(actividad.Grado) && actividad.Grado != "Todos")
                {
                    materiaNombre = $"{actividad.Materia} - {actividad.Grado}";
                }

                // Buscar si ya existe una calificación previa para ese estudiante, materia y periodo
                var calificacionExistente = _context.Calificaciones
                    .FirstOrDefault(c => c.ID_Estudiante == entrega.IdEstudiante &&
                                        (c.Materia == materiaNombre || c.Materia == actividad.Materia || c.Materia.StartsWith(actividad.Materia)) &&
                                        c.Periodo == periodoVal);

                if (calificacionExistente != null)
                {
                    calificacionExistente.Nota = notaDecimal;
                    calificacionExistente.FechaRegistro = DateTime.Now;
                }
                else
                {
                    var nuevaCalif = new Calificacion
                    {
                        ID_Estudiante = entrega.IdEstudiante,
                        Materia = materiaNombre,
                        Nota = notaDecimal,
                        Periodo = periodoVal,
                        FechaRegistro = DateTime.Now
                    };
                    _context.Calificaciones.Add(nuevaCalif);
                }
            }

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                message = "¡Calificación guardada y sincronizada correctamente con la planilla de notas!",
                idEntrega = entrega.IdEntrega,
                calificacion = notaDecimal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                estado = "Calificado"
            });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult EliminarActividad(int idActividad)
        {
            _context.AsegurarEsquemaActividades();

            var actividad = _context.Actividades.Find(idActividad);
            if (actividad == null)
            {
                return Json(new { success = false, message = "Actividad no encontrada." });
            }

            actividad.Activo = false;
            _context.SaveChanges();

            return Json(new { success = true, message = "Actividad académica eliminada correctamente." });
        }

        /// <summary>
        /// Consulta comentarios de clase o privados para el docente.
        /// </summary>
        [HttpGet]
        public IActionResult ObtenerComentariosActividadDocente(int idActividad, int? idEstudiantePrivado)
        {
            _context.AsegurarEsquemaActividades();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Sesión inválida." });

            var comentarios = _context.ComentariosActividades
                .Where(c => c.IdActividad == idActividad && (!c.EsPrivado || (idEstudiantePrivado.HasValue && c.IdEstudiantePrivado == idEstudiantePrivado.Value)))
                .OrderBy(c => c.Fecha)
                .Select(c => new
                {
                    idComentario = c.IdComentario,
                    idActividad = c.IdActividad,
                    idUsuario = c.IdUsuario,
                    nombreUsuario = c.NombreUsuario,
                    rolUsuario = c.RolUsuario,
                    mensaje = c.Mensaje,
                    fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    esPrivado = c.EsPrivado,
                    idEstudiantePrivado = c.IdEstudiantePrivado,
                    esMio = c.IdUsuario == userId.Value
                })
                .ToList();

            return Json(new { success = true, comentarios = comentarios });
        }

        /// <summary>
        /// Registra respuesta o comentario de clase/privado del docente.
        /// </summary>
        [HttpPost]
        public IActionResult GuardarComentarioActividadDocente(int idActividad, string mensaje, bool esPrivado = false, int? idEstudiantePrivado = null)
        {
            _context.AsegurarEsquemaActividades();

            var userId = HttpContext.Session.GetInt32("UserId");
            var nombreUsuario = HttpContext.Session.GetString("NombreUsuario") ?? "Docente";
            if (userId == null)
                return Json(new { success = false, message = "Sesión inválida." });

            if (string.IsNullOrWhiteSpace(mensaje))
                return Json(new { success = false, message = "El mensaje no puede estar vacío." });

            var actividad = _context.Actividades.Find(idActividad);
            if (actividad == null || !actividad.Activo)
                return Json(new { success = false, message = "Actividad no encontrada." });

            var nuevo = new ComentarioActividad
            {
                IdActividad = idActividad,
                IdUsuario = userId.Value,
                NombreUsuario = nombreUsuario,
                RolUsuario = "Docente",
                Mensaje = mensaje.Trim(),
                Fecha = DateTime.Now,
                EsPrivado = esPrivado,
                IdEstudiantePrivado = esPrivado ? idEstudiantePrivado : null
            };

            _context.ComentariosActividades.Add(nuevo);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                mensaje = "Comentario registrado correctamente.",
                comentario = new
                {
                    idComentario = nuevo.IdComentario,
                    idActividad = nuevo.IdActividad,
                    idUsuario = nuevo.IdUsuario,
                    nombreUsuario = nuevo.NombreUsuario,
                    rolUsuario = nuevo.RolUsuario,
                    mensaje = nuevo.Mensaje,
                    fecha = nuevo.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    esPrivado = nuevo.EsPrivado,
                    idEstudiantePrivado = nuevo.IdEstudiantePrivado,
                    esMio = true
                }
            });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult GuardarControlAsistencia([FromBody] List<AsistenciaItemDto> registros, DateTime? fecha)
        {
            _context.AsegurarEsquemaObservador();
            if (registros == null || !registros.Any())
            {
                return Json(new { success = false, message = "No se recibieron registros de asistencia." });
            }

            var fechaAsistencia = fecha ?? DateTime.Today;

            foreach (var r in registros)
            {
                string estadoTexto = "Presente";
                if (r.Estado == "F" || (r.Estado != null && r.Estado.ToLower() == "falla")) estadoTexto = "Falla";
                else if (r.Estado == "R" || (r.Estado != null && r.Estado.ToLower() == "retardo")) estadoTexto = "Retardo";

                var existente = _context.ASISTENCIAS
                    .FirstOrDefault(a => a.id_estudiante == r.IdEstudiante && a.fecha.Date == fechaAsistencia.Date);

                if (existente != null)
                {
                    existente.estado = estadoTexto;
                    existente.justificacion = r.Observacion;
                }
                else
                {
                    _context.ASISTENCIAS.Add(new Asistencia
                    {
                        id_estudiante = r.IdEstudiante,
                        fecha = fechaAsistencia,
                        estado = estadoTexto,
                        justificacion = r.Observacion
                    });
                }
            }

            _context.SaveChanges();
            return Json(new { success = true, message = "¡Control de asistencia guardado exitosamente!" });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult RegistrarObservacion([FromBody] ObservacionRegistroDto dto)
        {
            _context.AsegurarEsquemaObservador();
            if (dto == null || dto.IdEstudiante <= 0 || string.IsNullOrWhiteSpace(dto.Descripcion))
            {
                return Json(new { success = false, message = "Debe seleccionar un estudiante e ingresar la descripción de la observación." });
            }

            var nombreDocente = HttpContext.Session.GetString("NombreUsuario") ?? "Docente";
            var autor = !string.IsNullOrWhiteSpace(dto.QuienRegistra) ? dto.QuienRegistra.Trim() : nombreDocente;

            _context.ObservacionesEstudiante.Add(new ObservacionEstudiante
            {
                id_estudiante = dto.IdEstudiante,
                tipo_nota = string.IsNullOrWhiteSpace(dto.TipoNota) ? "Seguimiento Académico" : dto.TipoNota,
                descripcion = dto.Descripcion.Trim(),
                aspectos_mejorar = !string.IsNullOrWhiteSpace(dto.AspectosMejorar) ? dto.AspectosMejorar.Trim() : null,
                quien_registra = autor,
                docente = autor,
                periodo = dto.Periodo ?? 1,
                fecha = DateTime.Now
            });

            _context.SaveChanges();
            return Json(new { success = true, message = "¡Observación registrada en el observador del alumno exitosamente!" });
        }

        [HttpGet]
        [RequierePermiso(Permisos.ConsultarSeguimiento)]
        public IActionResult ObtenerObservacionesEstudiante(int idEstudiante)
        {
            _context.AsegurarEsquemaObservador();

            var observaciones = _context.ObservacionesEstudiante
                .Where(o => o.id_estudiante == idEstudiante)
                .OrderByDescending(o => o.fecha)
                .Select(o => new
                {
                    idObservacion = o.id_observacion,
                    idEstudiante = o.id_estudiante,
                    tipoNota = o.tipo_nota,
                    descripcion = o.descripcion,
                    aspectosMejorar = o.aspectos_mejorar,
                    quienRegistra = o.quien_registra ?? o.docente,
                    periodo = o.periodo,
                    fecha = o.fecha.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            return Json(new { success = true, observaciones = observaciones });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult RegistrarCitacion([FromBody] CitacionRegistroDto dto)
        {
            _context.AsegurarEsquemaObservador();
            if (dto == null || dto.IdEstudiante <= 0 || string.IsNullOrWhiteSpace(dto.Asunto) || string.IsNullOrWhiteSpace(dto.Mensaje))
            {
                return Json(new { success = false, message = "El asunto y mensaje de la citación son obligatorios." });
            }

            var nombreDocente = HttpContext.Session.GetString("NombreUsuario") ?? "Docente";

            _context.CITACIONES.Add(new Citacion
            {
                id_estudiante = dto.IdEstudiante,
                asunto = dto.Asunto.Trim(),
                mensaje = dto.Mensaje.Trim(),
                remitente = nombreDocente,
                fecha = dto.Fecha ?? DateTime.Now
            });

            // En lugar de enviar un correo, se genera un aviso formal de notificación al acudiente
            var idDocenteSesion = HttpContext.Session.GetInt32("UserId") ?? 1;
            var estUsuario = _context.Usuarios.Find(dto.IdEstudiante);
            string nombreEstudiante = estUsuario != null ? $"{estUsuario.NOMBRES} {estUsuario.APELLIDOS}".Trim() : "el estudiante";

            _context.Notificaciones.Add(new Notificacion
            {
                Titulo = $"Aviso de Citación: {dto.Asunto.Trim()}",
                Mensaje = $"El docente {nombreDocente} ha emitido un aviso/citación para {nombreEstudiante}: {dto.Mensaje.Trim()}",
                RolDestino = "acudiente",
                FechaCreacion = DateTime.Now,
                ID_UsuarioEmisor = idDocenteSesion,
                Activa = true,
                Prioridad = "alta"
            });

            _context.SaveChanges();
            return Json(new { success = true, message = "¡Aviso de notificación enviado al acudiente exitosamente!" });
        }

        // ===============================================
        // GESTIÓN DE ACCIONES DE MEJORA Y SEGUIMIENTO (DOCENTE)
        // ===============================================

        [HttpGet]
        [RequierePermiso(Permisos.ConsultarSeguimiento)]
        public IActionResult ObtenerAccionesMejoraDocente(string? curso, string? materia)
        {
            _context.AsegurarEsquemaAccionesMejora();

            var query = _context.AccionesMejora.Where(a => a.Activo);

            if (!string.IsNullOrWhiteSpace(curso) && curso.ToLower() != "todos")
            {
                query = query.Where(a => a.Grado == curso);
            }

            if (!string.IsNullOrWhiteSpace(materia) && materia.ToLower() != "todas")
            {
                query = query.Where(a => a.Materia == materia);
            }

            var mejoras = query
                .OrderByDescending(a => a.FechaRegistro)
                .Select(a => new
                {
                    idMejora = a.IdMejora,
                    idEstudiante = a.IdEstudiante,
                    nombreEstudiante = a.NombreEstudiante,
                    materia = a.Materia,
                    grado = a.Grado,
                    periodo = a.Periodo,
                    aspectoMejorar = a.AspectoMejorar,
                    compromisoEstudiante = a.CompromisoEstudiante,
                    fechaRegistro = a.FechaRegistro.ToString("dd/MM/yyyy"),
                    fechaCompromiso = a.FechaCompromiso.HasValue ? a.FechaCompromiso.Value.ToString("dd/MM/yyyy") : null,
                    estadoSeguimiento = a.EstadoSeguimiento,
                    observacionSeguimiento = a.ObservacionSeguimiento,
                    fechaSeguimiento = a.FechaSeguimiento.HasValue ? a.FechaSeguimiento.Value.ToString("dd/MM/yyyy HH:mm") : null,
                    respuestaEstudiante = a.RespuestaEstudiante,
                    fechaRespuestaEstudiante = a.FechaRespuestaEstudiante.HasValue ? a.FechaRespuestaEstudiante.Value.ToString("dd/MM/yyyy HH:mm") : null,
                    docente = a.Docente
                })
                .ToList();

            return Json(new { success = true, mejoras = mejoras });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult GuardarAccionMejora([FromBody] AccionMejoraRegistroDto dto)
        {
            _context.AsegurarEsquemaAccionesMejora();

            if (dto == null || dto.IdEstudiante <= 0 || string.IsNullOrWhiteSpace(dto.Materia) || string.IsNullOrWhiteSpace(dto.AspectoMejorar) || string.IsNullOrWhiteSpace(dto.CompromisoEstudiante))
            {
                return Json(new { success = false, message = "Por favor complete todos los campos obligatorios: Estudiante, Asignatura, Aspecto a Mejorar y Compromiso." });
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var nombreDocente = HttpContext.Session.GetString("NombreUsuario") ?? "Docente";

            // Obtener nombre del estudiante si no viene en dto
            string nombreEst = dto.NombreEstudiante ?? "";
            if (string.IsNullOrWhiteSpace(nombreEst))
            {
                var est = _context.Usuarios.Find(dto.IdEstudiante);
                if (est != null)
                {
                    nombreEst = $"{est.NOMBRES} {est.APELLIDOS}".Trim();
                }
            }

            var nuevaMejora = new AccionMejora
            {
                IdEstudiante = dto.IdEstudiante,
                NombreEstudiante = nombreEst,
                IdDocente = userId,
                Docente = nombreDocente,
                Materia = dto.Materia.Trim(),
                Grado = dto.Grado?.Trim(),
                Periodo = dto.Periodo > 0 ? dto.Periodo : 1,
                AspectoMejorar = dto.AspectoMejorar.Trim(),
                CompromisoEstudiante = dto.CompromisoEstudiante.Trim(),
                FechaRegistro = DateTime.Now,
                FechaCompromiso = dto.FechaCompromiso,
                EstadoSeguimiento = "En Proceso",
                Activo = true
            };

            _context.AccionesMejora.Add(nuevaMejora);
            _context.SaveChanges();

            return Json(new { success = true, message = "¡Acción de mejora y compromiso registrados exitosamente!" });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult ActualizarSeguimientoMejora([FromBody] SeguimientoMejoraDto dto)
        {
            _context.AsegurarEsquemaAccionesMejora();

            if (dto == null || dto.IdMejora <= 0)
            {
                return Json(new { success = false, message = "Registro no válido." });
            }

            var mejora = _context.AccionesMejora.Find(dto.IdMejora);
            if (mejora == null || !mejora.Activo)
            {
                return Json(new { success = false, message = "La acción de mejora no existe o fue eliminada." });
            }

            mejora.EstadoSeguimiento = string.IsNullOrWhiteSpace(dto.EstadoSeguimiento) ? "Cumplido" : dto.EstadoSeguimiento.Trim();
            mejora.ObservacionSeguimiento = dto.ObservacionSeguimiento?.Trim();
            mejora.FechaSeguimiento = DateTime.Now;
            _context.SaveChanges();

            return Json(new { success = true, message = "¡Seguimiento actualizado correctamente!" });
        }

        [HttpPost]
        [RequierePermiso(Permisos.GestionarClases)]
        public IActionResult EliminarAccionMejora(int idMejora)
        {
            _context.AsegurarEsquemaAccionesMejora();

            var mejora = _context.AccionesMejora.Find(idMejora);
            if (mejora == null)
            {
                return Json(new { success = false, message = "Registro no encontrado." });
            }

            mejora.Activo = false;
            _context.SaveChanges();

            return Json(new { success = true, message = "Acción de mejora eliminada exitosamente." });
        }
    }
}