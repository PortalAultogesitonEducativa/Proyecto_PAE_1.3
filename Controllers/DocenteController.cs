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
            _context.AsegurarEsquemaEstudiantes();

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
                var prof = _context.PROFESOR.FirstOrDefault(p => (p.id_usuario != null && p.id_usuario == userId) ||
                                                               (usuarioActual != null && !string.IsNullOrEmpty(p.email) && p.email.ToLower() == usuarioActual.CORREO_ELECTRONICO.ToLower()));

                if (prof != null)
                {
                    // Cursos asignados en HORARIOS para este profesor
                    var cursoIdsHorario = _context.HORARIOS
                        .Where(h => h.id_profesor == prof.id_profesor)
                        .Select(h => h.id_curso)
                        .Distinct()
                        .ToList();

                    if (cursoIdsHorario.Any())
                    {
                        grados = listaCursos
                            .Where(c => cursoIdsHorario.Contains(c.id_curso))
                            .Select(c => c.nombre_curso)
                            .Distinct()
                            .OrderBy(n => n)
                            .ToList();
                    }

                    // Materias asignadas en PROFESOR_MATERIA
                    var materiaIds = _context.PROFESOR_MATERIA
                        .Where(pm => pm.id_profesor == prof.id_profesor)
                        .Select(pm => pm.id_materia)
                        .Distinct()
                        .ToList();

                    if (materiaIds.Any())
                    {
                        asignaturas = _context.MATERIA
                            .Where(m => materiaIds.Contains(m.id_materia))
                            .Select(m => m.nombre_materia)
                            .Distinct()
                            .OrderBy(n => n)
                            .ToList();
                    }
                }

                // Fallback seguro si no hay asignaciones explícitas en base de datos
                if (!grados.Any())
                {
                    grados = listaCursos.Select(c => c.nombre_curso).Distinct().OrderBy(n => n).ToList();
                }

                if (!asignaturas.Any())
                {
                    asignaturas = _context.MATERIA.Select(m => m.nombre_materia).Distinct().OrderBy(n => n).ToList();
                }
            }

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
        /// Genera y descarga el reporte del observador en formato PDF usando Rotativa con el formato institucional Sor María Juliana.
        /// </summary>
        public IActionResult DescargarObservadorPdf(int? idEstudiante, string? curso)
        {
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

            var inscripciones = _context.ExtraInscripciones
                .Where(i => i.IdExtraCurso == idCurso && i.Estado == "Inscrito")
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
                totalInscritos = inscritos.Count,
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
            model.IdDocente = userId;
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

                var carpeta = Path.Combine("wwwroot", "uploads", "guias");
                Directory.CreateDirectory(carpeta);

                var nombreGuardado = $"guia_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(archivo.FileName)}";
                var rutaFisica = Path.Combine(carpeta, nombreGuardado);
                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                model.ArchivoAdjunto = $"/uploads/guias/{nombreGuardado}";
            }

            _context.Actividades.Add(model);
            _context.SaveChanges();

            return Json(new { success = true, message = "¡Actividad académica creada y publicada con éxito!" });
        }

        [HttpGet]
        public IActionResult ObtenerActividadesDocente()
        {
            _context.AsegurarEsquemaActividades();

            var userId = HttpContext.Session.GetInt32("UserId");
            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();

            var actividades = _context.Actividades
                .Where(a => a.Activo && (rol == "admin" || a.IdDocente == userId || a.IdDocente == null))
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
                    grado = a.Grado,
                    fechaLimite = a.FechaLimite.ToString("dd/MM/yyyy HH:mm"),
                    fechaLimiteIso = a.FechaLimite.ToString("o"),
                    archivoAdjunto = a.ArchivoAdjunto,
                    totalEntregas = totalEntregas,
                    calificadas = calificadas,
                    pendientes = pendientes,
                    estaVencida = DateTime.Now > a.FechaLimite
                };
            }).ToList();

            return Json(new { success = true, actividades = resultado });
        }

        [HttpGet]
        public IActionResult ObtenerEntregasActividad(int idActividad)
        {
            _context.AsegurarEsquemaActividades();

            var actividad = _context.Actividades.Find(idActividad);
            if (actividad == null)
            {
                return Json(new { success = false, message = "Actividad no encontrada." });
            }

            var entregas = _context.EntregasActividades
                .Where(e => e.IdActividad == idActividad)
                .Join(_context.Usuarios,
                      e => e.IdEstudiante,
                      u => u.ID_Usuario,
                      (e, u) => new
                      {
                          idEntrega = e.IdEntrega,
                          idEstudiante = u.ID_Usuario,
                          nombreEstudiante = (u.NOMBRES + " " + u.APELLIDOS).Trim(),
                          documento = u.NUM_DOCUMENTO ?? u.NOMBRE_USUARIO,
                          correo = u.CORREO_ELECTRONICO ?? "-",
                          grado = u.COLEGIO_PROCEDENCIA ?? "Estudiante",
                          fechaEntrega = e.FechaEntrega.ToString("dd/MM/yyyy HH:mm"),
                          archivoRuta = e.ArchivoRuta,
                          archivoNombre = e.ArchivoNombre,
                          comentario = e.Comentario,
                          calificacion = e.Calificacion,
                          retroalimentacion = e.Retroalimentacion,
                          estado = e.Estado
                      })
                .OrderBy(e => e.nombreEstudiante)
                .ToList();

            return Json(new
            {
                success = true,
                tituloActividad = actividad.Titulo,
                materia = actividad.Materia,
                grado = actividad.Grado,
                totalEntregas = entregas.Count,
                entregas = entregas
            });
        }

        [HttpPost]
        public IActionResult CalificarEntrega(int idEntrega, string nota, string? retroalimentacion, int? periodo)
        {
            _context.AsegurarEsquemaActividades();

            var rol = (HttpContext.Session.GetString("UserRol") ?? "").ToLower().Trim();
            if (rol != "docente" && rol != "admin" && rol != "profesor")
            {
                return Json(new { success = false, message = "No tienes permisos para calificar actividades." });
            }

            var entrega = _context.EntregasActividades.Find(idEntrega);
            if (entrega == null)
            {
                return Json(new { success = false, message = "Entrega de actividad no encontrada." });
            }

            if (!decimal.TryParse((nota ?? "").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal notaDecimal))
            {
                return Json(new { success = false, message = "El valor de la calificación no es válido." });
            }

            if (notaDecimal < 0m || notaDecimal > 5.0m)
            {
                return Json(new { success = false, message = "La calificación debe estar entre 0.0 y 5.0." });
            }

            entrega.Calificacion = notaDecimal;
            entrega.Retroalimentacion = retroalimentacion;
            entrega.Estado = "Calificado";

            // Reflejar la calificación automáticamente en la tabla oficial de Calificaciones (Planilla de Notas)
            var actividad = _context.Actividades.Find(entrega.IdActividad);
            if (actividad != null)
            {
                int periodoVal = periodo ?? 1;
                string materiaNombre = actividad.Materia;
                if (!string.IsNullOrEmpty(actividad.Grado))
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
                message = "¡Calificación guardada exitosamente y sincronizada en la Planilla de Notas!",
                calificacion = notaDecimal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                estado = "Calificado"
            });
        }

        [HttpPost]
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

        [HttpPost]
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

            _context.SaveChanges();
            return Json(new { success = true, message = "¡Citación enviada al padre/acudiente exitosamente!" });
        }

        // ===============================================
        // GESTIÓN DE ACCIONES DE MEJORA Y SEGUIMIENTO (DOCENTE)
        // ===============================================

        [HttpGet]
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
