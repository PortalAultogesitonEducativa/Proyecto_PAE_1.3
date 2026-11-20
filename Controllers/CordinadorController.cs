using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.Controllers
{
    /// <summary>
    /// Módulo del coordinador y administración académica.
    /// Supervisión docente, control y coordinación de periodos de subida de notas,
    /// plazos, seguimiento académico y cobertura institucional.
    /// </summary>
    [Route("Coordinador")]
    [Route("Cordinador")]
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoordinadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Supervisión docente y coordinación de periodos de subida de notas:
        /// Control de fechas de apertura y cierre de notas por periodo, carga académica,
        /// horas semanales, choques de horario, cobertura de materias, actividades vigentes,
        /// entregas sin calificar con atraso, planes de mejora y seguimiento de docentes.
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("Supervision")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult Supervision()
        {
            return View(ConstruirSupervision());
        }

        private SupervisionDocentesViewModel ConstruirSupervision()
        {
            _context.AsegurarEsquemaActividades();
            _context.AsegurarEsquemaAccionesMejora();
            _context.AsegurarEsquemaPeriodosSubidaNotas();
            _context.AsegurarLimpiezaCalificacionesDuplicadas();

            // 1. Cargar Periodos de Subida de Notas
            var periodos = _context.PeriodosSubidaNotas.OrderBy(p => p.Periodo).ToList();
            if (!periodos.Any())
            {
                periodos = new List<PeriodoSubidaNota>
                {
                    new PeriodoSubidaNota { Periodo = 1, Nombre = "Primer Periodo Académico", FechaInicio = DateTime.Now.AddDays(-15), FechaCierre = DateTime.Now.AddDays(30), Activo = true, Porcentaje = 25, Observaciones = "Subida ordinaria de notas Periodo 1" },
                    new PeriodoSubidaNota { Periodo = 2, Nombre = "Segundo Periodo Académico", FechaInicio = DateTime.Now.AddDays(31), FechaCierre = DateTime.Now.AddDays(60), Activo = false, Porcentaje = 25, Observaciones = "Pendiente de apertura Periodo 2" },
                    new PeriodoSubidaNota { Periodo = 3, Nombre = "Tercer Periodo Académico", FechaInicio = DateTime.Now.AddDays(61), FechaCierre = DateTime.Now.AddDays(90), Activo = false, Porcentaje = 25, Observaciones = "Pendiente de apertura Periodo 3" },
                    new PeriodoSubidaNota { Periodo = 4, Nombre = "Cuarto Periodo Académico", FechaInicio = DateTime.Now.AddDays(91), FechaCierre = DateTime.Now.AddDays(120), Activo = false, Porcentaje = 25, Observaciones = "Pendiente de apertura Periodo 4" }
                };
                _context.PeriodosSubidaNotas.AddRange(periodos);
                _context.SaveChanges();
            }

            var modelo = new SupervisionDocentesViewModel
            {
                Periodos = periodos,
                PeriodoActivo = (periodos.FirstOrDefault(p => p.Activo) ?? periodos.First()).Periodo
            };

            // 2. Cargar Docentes y Estructura Académica
            var docentes = _context.Usuarios
                .Where(u => u.ROL != null && (u.ROL.ToLower() == "docente" || u.ROL.ToLower() == "profesor") && u.ACTIVO)
                .OrderBy(u => u.APELLIDOS)
                .ThenBy(u => u.NOMBRES)
                .ToList();

            var profesores = _context.PROFESOR.Where(p => p.id_usuario != null).ToList();
            var horarios = _context.HORARIOS.ToList();

            // El año escolar de referencia es el más reciente que tenga horarios cargados.
            int? anioEscolar = horarios.Any() ? horarios.Max(h => h.anio_escolar) : (int?)null;
            modelo.AnioEscolar = anioEscolar;

            var horariosVigentes = anioEscolar.HasValue
                ? horarios.Where(h => h.anio_escolar == anioEscolar.Value).ToList()
                : horarios;

            // Solo se piden las columnas necesarias de PROFESOR_MATERIA
            var materiasHabilitadas = _context.PROFESOR_MATERIA
                .Select(pm => new { pm.id_profesor, pm.id_materia })
                .ToList();

            var nombresMaterias = _context.MATERIA
                .Select(m => new { m.id_materia, m.nombre_materia })
                .ToDictionary(m => m.id_materia, m => m.nombre_materia);

            var nombresCursos = _context.Cursos
                .Select(c => new { c.id_curso, c.nombre_curso })
                .ToDictionary(c => c.id_curso, c => c.nombre_curso);

            var actividades = _context.Actividades.Where(a => a.Activo && a.IdDocente != null).ToList();
            var entregasPendientes = _context.EntregasActividades
                .Where(e => e.Calificacion == null)
                .Select(e => new { e.IdActividad, e.FechaEntrega })
                .ToList();

            var planes = _context.AccionesMejora.Where(a => a.Activo && a.IdDocente != null).ToList();

            // Calificaciones del periodo activo para monitorear subida de notas
            var calificacionesPeriodoActivo = _context.Calificaciones
                .Where(c => c.Periodo == modelo.PeriodoActivo)
                .ToList();

            var ahora = DateTime.Now;
            var limiteAtraso = ahora.Date.AddDays(-SupervisionDocentesViewModel.DiasEntregaAtrasada);

            foreach (var docente in docentes)
            {
                string correoDoc = (docente.CORREO_ELECTRONICO ?? "").Trim().ToLower();
                string nombresDoc = (docente.NOMBRES ?? "").Trim().ToLower();
                string apellidosDoc = (docente.APELLIDOS ?? "").Trim().ToLower();
                string nombreDocente = $"{docente.NOMBRES} {docente.APELLIDOS}".Trim();
                string nombreCompletoDoc = $"{nombresDoc} {apellidosDoc}".Trim();

                var idsProfesor = profesores
                    .Where(p => (p.id_usuario != null && p.id_usuario == docente.ID_Usuario)
                             || (!string.IsNullOrEmpty(p.email) && !string.IsNullOrEmpty(correoDoc) && p.email.Trim().ToLower() == correoDoc)
                             || (!string.IsNullOrEmpty(p.nombre) && !string.IsNullOrEmpty(p.apellido) &&
                                 ($"{p.nombre.Trim()} {p.apellido.Trim()}".ToLower() == nombreCompletoDoc ||
                                  (nombresDoc.Contains(p.nombre.Trim().ToLower()) && apellidosDoc.Contains(p.apellido.Trim().ToLower())))))
                    .Select(p => p.id_profesor)
                    .ToList();

                idsProfesor.Add(docente.ID_Usuario);
                idsProfesor = idsProfesor.Distinct().ToList();

                var clases = horariosVigentes.Where(h => idsProfesor.Contains(h.id_profesor)).ToList();

                var idsMaterias = clases.Select(h => h.id_materia)
                    .Concat(materiasHabilitadas.Where(pm => idsProfesor.Contains(pm.id_profesor)).Select(pm => pm.id_materia))
                    .Distinct()
                    .ToList();

                var materiasDocenteNombres = idsMaterias
                    .Where(id => nombresMaterias.ContainsKey(id))
                    .Select(id => nombresMaterias[id].ToLower().Trim())
                    .ToList();

                // Choques de horario del docente
                var choques = BuscarChoques(clases);
                foreach (var (a, b) in choques)
                {
                    modelo.Choques.Add(new ChoqueHorarioItem
                    {
                        Docente = nombreDocente,
                        Dia = (a.dia ?? string.Empty).Trim(),
                        Detalle = $"{DescribirClase(a, nombresCursos, nombresMaterias)}   ⟷   {DescribirClase(b, nombresCursos, nombresMaterias)}"
                    });
                }

                var actividadesDocente = actividades.Where(a => a.IdDocente == docente.ID_Usuario).ToList();
                var idsActividades = actividadesDocente.Select(a => a.IdActividad).ToList();
                var entregasDocente = entregasPendientes.Where(e => idsActividades.Contains(e.IdActividad)).ToList();

                var planesDocente = planes.Where(p => p.IdDocente == docente.ID_Usuario).ToList();
                var planesEnProceso = planesDocente.Where(p => EsEnProceso(p.EstadoSeguimiento)).ToList();

                // Calcular cuántas notas ha subido en el periodo activo
                int notasDocentePeriodo = 0;
                if (materiasDocenteNombres.Any())
                {
                    notasDocentePeriodo = calificacionesPeriodoActivo.Count(c => {
                        string mCalif = (c.Materia ?? "").ToLower();
                        return materiasDocenteNombres.Any(mat => mCalif.Contains(mat));
                    });
                }

                var idsCursos = clases.Select(h => h.id_curso).Distinct().ToList();
                var cursosDocente = idsCursos
                    .Where(id => nombresCursos.ContainsKey(id))
                    .Select(id => nombresCursos[id])
                    .OrderBy(n => {
                        var digits = new string(n.TakeWhile(char.IsDigit).ToArray());
                        return int.TryParse(digits, out int num) ? num : 999999;
                    })
                    .ThenBy(n => n)
                    .ToList();

                modelo.Docentes.Add(new SupervisionDocenteItem
                {
                    IdUsuario = docente.ID_Usuario,
                    Nombre = nombreDocente,
                    Correo = docente.CORREO_ELECTRONICO ?? string.Empty,
                    Clases = clases.Count,
                    HorasSemanales = Math.Round(clases.Sum(h => (h.hora_fin - h.hora_inicio).TotalHours), 1),
                    ChoquesHorario = choques.Count,
                    Materias = idsMaterias
                        .Where(id => nombresMaterias.ContainsKey(id))
                        .Select(id => nombresMaterias[id])
                        .OrderBy(n => n)
                        .ToList(),
                    Cursos = cursosDocente,
                    ActividadesVigentes = actividadesDocente.Count(a => a.FechaLimite >= ahora),
                    ActividadesVencidas = actividadesDocente.Count(a => a.FechaLimite < ahora),
                    EntregasSinCalificar = entregasDocente.Count,
                    EntregasSinCalificarAtrasadas = entregasDocente.Count(e => e.FechaEntrega.Date <= limiteAtraso),
                    PlanesMejoraActivos = planesDocente.Count,
                    PlanesMejoraEnProceso = planesEnProceso.Count,
                    PlanesMejoraVencidos = planesEnProceso.Count(p => p.FechaCompromiso.HasValue && p.FechaCompromiso.Value.Date < ahora.Date),
                    UltimaActividad = actividadesDocente.Any() ? actividadesDocente.Max(a => a.FechaCreacion) : (DateTime?)null,
                    NotasRegistradasPeriodoActivo = notasDocentePeriodo
                });
            }

            // Materias del plan de cada curso que no tienen ningún horario (y por tanto ningún docente)
            var idsCursosDelAnio = anioEscolar.HasValue
                ? _context.Cursos.Where(c => c.ano_escolar == anioEscolar.Value).Select(c => c.id_curso).ToHashSet()
                : new HashSet<int>();

            if (idsCursosDelAnio.Count == 0)
                idsCursosDelAnio = horariosVigentes.Select(h => h.id_curso).ToHashSet();

            var materiasCubiertas = new HashSet<(int Curso, int Materia)>(
                horariosVigentes.Select(h => (h.id_curso, h.id_materia)));

            modelo.MateriasSinDocente = _context.CURSO_MATERIA
                .Select(cm => new { cm.id_curso, cm.id_materia })
                .ToList()
                .Where(cm => idsCursosDelAnio.Contains(cm.id_curso) && !materiasCubiertas.Contains((cm.id_curso, cm.id_materia)))
                .Select(cm => new MateriaSinDocenteItem
                {
                    Curso = nombresCursos.ContainsKey(cm.id_curso) ? nombresCursos[cm.id_curso] : $"Curso {cm.id_curso}",
                    Materia = nombresMaterias.ContainsKey(cm.id_materia) ? nombresMaterias[cm.id_materia] : $"Materia {cm.id_materia}"
                })
                .OrderBy(x => x.Curso)
                .ThenBy(x => x.Materia)
                .ToList();

            modelo.TotalEstudiantes = _context.Usuarios
                .Count(u => u.ROL != null && u.ROL.ToLower() == "estudiante" && u.ACTIVO);

            return modelo;
        }

        /// <summary>
        /// Habilita o bloquea la subida de calificaciones para un periodo académico.
        /// </summary>
        [HttpPost]
        [Route("TogglePeriodoSubida")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult TogglePeriodoSubida(int periodo, bool activo)
        {
            try
            {
                _context.AsegurarEsquemaPeriodosSubidaNotas();
                var p = _context.PeriodosSubidaNotas.FirstOrDefault(x => x.Periodo == periodo);
                if (p == null)
                {
                    p = new PeriodoSubidaNota
                    {
                        Periodo = periodo,
                        Nombre = $"{periodo}° Periodo Académico",
                        Activo = activo,
                        FechaInicio = DateTime.Now,
                        FechaCierre = DateTime.Now.AddDays(30)
                    };
                    _context.PeriodosSubidaNotas.Add(p);
                }
                else
                {
                    p.Activo = activo;
                }
                _context.SaveChanges();

                string estadoTexto = activo ? "HABILITADO para la subida de calificaciones" : "CERRADO (subida de notas bloqueada)";
                return Json(new { success = true, message = $"El Periodo {periodo} ahora se encuentra {estadoTexto}.", activo = p.Activo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar estado del periodo: " + ex.Message });
            }
        }

        /// <summary>
        /// Modifica las fechas de inicio, cierre y ponderación para la subida de notas de un periodo.
        /// </summary>
        [HttpPost]
        [Route("GuardarFechasPeriodo")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult GuardarFechasPeriodo(int periodo, DateTime fechaInicio, DateTime fechaCierre, int porcentaje = 25, string? observaciones = null)
        {
            try
            {
                if (fechaCierre < fechaInicio)
                {
                    return Json(new { success = false, message = "La fecha de cierre no puede ser anterior a la fecha de inicio." });
                }

                _context.AsegurarEsquemaPeriodosSubidaNotas();
                var p = _context.PeriodosSubidaNotas.FirstOrDefault(x => x.Periodo == periodo);
                if (p == null)
                {
                    p = new PeriodoSubidaNota
                    {
                        Periodo = periodo,
                        Nombre = $"{periodo}° Periodo Académico",
                        FechaInicio = fechaInicio,
                        FechaCierre = fechaCierre,
                        Porcentaje = porcentaje > 0 ? porcentaje : 25,
                        Observaciones = observaciones,
                        Activo = true
                    };
                    _context.PeriodosSubidaNotas.Add(p);
                }
                else
                {
                    p.FechaInicio = fechaInicio;
                    p.FechaCierre = fechaCierre;
                    p.Porcentaje = porcentaje > 0 ? porcentaje : 25;
                    p.Observaciones = observaciones;
                }
                _context.SaveChanges();

                return Json(new { success = true, message = $"Fechas y plazos del Periodo {periodo} configurados exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar configuración de fechas: " + ex.Message });
            }
        }

        /// <summary>
        /// Envía una notificación masiva institucional a todos los docentes recordando el plazo de subida de notas.
        /// </summary>
        [HttpPost]
        [Route("NotificarDocentesPendientes")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult NotificarDocentesPendientes(int periodo)
        {
            try
            {
                _context.AsegurarEsquemaNotificaciones();
                _context.AsegurarEsquemaPeriodosSubidaNotas();

                var pObj = _context.PeriodosSubidaNotas.FirstOrDefault(p => p.Periodo == periodo);
                string fechaLimiteStr = pObj != null ? pObj.FechaCierre.ToString("dd/MM/yyyy") : "la fecha programada";

                var emisorIdStr = HttpContext.Session.GetString("UserIdStr");
                int emisorId = int.TryParse(emisorIdStr, out int uid) ? uid : 1;

                var notificacion = new Notificacion
                {
                    Titulo = $"Aviso de Coordinación: Subida de Calificaciones Periodo {periodo}",
                    Mensaje = $"Se recuerda a los docentes que el plazo institucional para el registro y cierre de calificaciones del Periodo {periodo} vence el {fechaLimiteStr}. Por favor asegurarse de completar las planillas a tiempo en la plataforma.",
                    RolDestino = "docente",
                    Prioridad = "alta",
                    ID_UsuarioEmisor = emisorId,
                    FechaCreacion = DateTime.Now,
                    Activa = true
                };

                _context.Notificaciones.Add(notificacion);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Se ha emitido el recordatorio oficial del Periodo {periodo} a todos los docentes." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al emitir notificación: " + ex.Message });
            }
        }

        /// <summary>
        /// Seguimiento estudiantil de un periodo: estudiantes con nota baja y si tienen plan de mejora,
        /// cuántos estudiantes de cada curso ya tienen nota en cada materia, y la última asistencia por curso.
        /// </summary>
        [HttpGet]
        [Route("Seguimiento")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult Seguimiento(int? periodo)
        {
            return View(ConstruirSeguimiento(periodo));
        }

        private SeguimientoEstudiantilViewModel ConstruirSeguimiento(int? periodo)
        {
            _context.AsegurarEsquemaAccionesMejora();

            // Estudiantes activos y su curso
            var estudiantes = _context.Usuarios
                .Where(u => u.ROL != null && u.ROL.ToLower() == "estudiante" && u.ACTIVO)
                .Select(u => new { u.ID_Usuario, u.NOMBRES, u.APELLIDOS, u.CORREO_ELECTRONICO })
                .ToList();

            var estudiantesEst = _context.ESTUDIANTE
                .Select(e => new { e.id_estudiante, e.id_usuario, e.email, e.curso_asignado })
                .ToList();
            var matriculas = _context.Matriculas
                .Select(m => new { m.id_estudiante, m.id_curso, m.estado })
                .ToList();
            var cursos = _context.Cursos
                .Select(c => new { c.id_curso, c.nombre_curso, c.ano_escolar })
                .ToList();

            var estPorUsuario = estudiantesEst
                .Where(e => e.id_usuario != null)
                .GroupBy(e => e.id_usuario!.Value)
                .ToDictionary(g => g.Key, g => g.First());
            var estPorCorreo = estudiantesEst
                .Where(e => !string.IsNullOrWhiteSpace(e.email))
                .GroupBy(e => e.email!.Trim().ToLower())
                .ToDictionary(g => g.Key, g => g.First());
            var nombresCursos = cursos.ToDictionary(c => c.id_curso, c => c.nombre_curso);

            string? CursoDe(int idUsuario, string? correo)
            {
                var est = estPorUsuario.ContainsKey(idUsuario)
                    ? estPorUsuario[idUsuario]
                    : (!string.IsNullOrWhiteSpace(correo) && estPorCorreo.ContainsKey(correo!.Trim().ToLower()) ? estPorCorreo[correo.Trim().ToLower()] : null);
                if (est == null) return null;

                if (!string.IsNullOrWhiteSpace(est.curso_asignado)) return est.curso_asignado;

                var mat = matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante && MatriculaActiva(m.estado))
                          ?? matriculas.FirstOrDefault(m => m.id_estudiante == est.id_estudiante);
                if (mat == null || !mat.id_curso.HasValue) return null;

                return nombresCursos.ContainsKey(mat.id_curso.Value) ? nombresCursos[mat.id_curso.Value] : mat.id_curso.Value.ToString();
            }

            var infoEstudiante = estudiantes.ToDictionary(
                u => u.ID_Usuario,
                u => new { Nombre = $"{u.NOMBRES} {u.APELLIDOS}".Trim(), Curso = CursoDe(u.ID_Usuario, u.CORREO_ELECTRONICO) });

            // Estudiantes por curso (clave normalizada del nombre del curso)
            var estudiantesPorCurso = infoEstudiante
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Curso))
                .GroupBy(kv => Normalizar(kv.Value.Curso))
                .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

            // Notas del periodo
            var notasTodas = _context.Calificaciones
                .Select(c => new { c.ID_Estudiante, c.Materia, c.Nota, c.Periodo, c.FechaRegistro })
                .ToList();

            int? anioNotas = notasTodas.Any() ? notasTodas.Max(n => n.FechaRegistro.Year) : (int?)null;
            var notasAnio = anioNotas.HasValue
                ? notasTodas.Where(n => n.FechaRegistro.Year == anioNotas.Value).ToList()
                : notasTodas;

            int periodoSel = periodo ?? (notasAnio.Any() ? notasAnio.Max(n => n.Periodo) : 1);
            if (periodoSel < 1 || periodoSel > 4) periodoSel = 1;

            // Una sola nota por estudiante y materia: la más reciente del periodo
            var notasPeriodo = notasAnio
                .Where(n => n.Periodo == periodoSel)
                .GroupBy(n => (n.ID_Estudiante, Materia: Normalizar(n.Materia)))
                .Select(g => g.OrderByDescending(n => n.FechaRegistro).First())
                .ToList();

            // Docente responsable por clase (curso + materia), según el horario vigente
            var materias = _context.MATERIA
                .Select(m => new { m.id_materia, m.nombre_materia })
                .ToList();
            var idMateriaPorNombre = materias
                .GroupBy(m => Normalizar(m.nombre_materia))
                .ToDictionary(g => g.Key, g => g.First().id_materia);
            var idCursoPorNombre = cursos
                .GroupBy(c => Normalizar(c.nombre_curso))
                .ToDictionary(g => g.Key, g => g.First().id_curso);

            var horarios = _context.HORARIOS
                .Select(h => new { h.id_profesor, h.id_curso, h.id_materia, h.anio_escolar })
                .ToList();
            int? anioEscolar = horarios.Any() ? horarios.Max(h => h.anio_escolar) : (int?)null;
            var horariosVigentes = anioEscolar.HasValue
                ? horarios.Where(h => h.anio_escolar == anioEscolar.Value).ToList()
                : horarios;

            var usuarioDeProfesor = _context.PROFESOR
                .Where(p => p.id_usuario != null)
                .Select(p => new { p.id_profesor, p.id_usuario })
                .ToList()
                .ToDictionary(p => p.id_profesor, p => p.id_usuario!.Value);
            var nombreUsuarios = _context.Usuarios
                .Select(u => new { u.ID_Usuario, u.NOMBRES, u.APELLIDOS })
                .ToList()
                .ToDictionary(u => u.ID_Usuario, u => $"{u.NOMBRES} {u.APELLIDOS}".Trim());

            var docentesPorClase = horariosVigentes
                .GroupBy(h => (h.id_curso, h.id_materia))
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(h => h.id_profesor)
                        .Distinct()
                        .Where(idp => usuarioDeProfesor.ContainsKey(idp) && nombreUsuarios.ContainsKey(usuarioDeProfesor[idp]))
                        .Select(idp => nombreUsuarios[usuarioDeProfesor[idp]])
                        .OrderBy(n => n)));

            string DocenteDeClase(string? cursoNombre, string? materiaNombre)
            {
                if (string.IsNullOrWhiteSpace(cursoNombre) || string.IsNullOrWhiteSpace(materiaNombre)) return string.Empty;
                if (!idCursoPorNombre.TryGetValue(Normalizar(cursoNombre), out var idCurso)) return string.Empty;
                if (!idMateriaPorNombre.TryGetValue(Normalizar(materiaNombre), out var idMateria)) return string.Empty;
                return docentesPorClase.TryGetValue((idCurso, idMateria), out var nombres) ? nombres : string.Empty;
            }

            // Planes de mejora existentes
            var planes = _context.AccionesMejora
                .Where(a => a.Activo)
                .Select(a => new { a.IdEstudiante, a.Materia, a.Periodo, a.EstadoSeguimiento, a.FechaCompromiso, a.FechaRegistro })
                .ToList()
                .GroupBy(a => (a.IdEstudiante, Materia: Normalizar(a.Materia), a.Periodo))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.FechaRegistro).First());

            var hoy = DateTime.Now.Date;
            var modelo = new SeguimientoEstudiantilViewModel
            {
                Periodo = periodoSel,
                AnioNotas = anioNotas,
                TotalNotasPeriodo = notasPeriodo.Count
            };

            // 1. Estudiantes en riesgo
            foreach (var n in notasPeriodo.Where(n => n.Nota < SeguimientoEstudiantilViewModel.NotaMinimaAprobar))
            {
                if (!infoEstudiante.TryGetValue(n.ID_Estudiante, out var info))
                    continue;

                planes.TryGetValue((n.ID_Estudiante, Normalizar(n.Materia), periodoSel), out var plan);
                bool enProceso = plan != null && string.Equals((plan.EstadoSeguimiento ?? string.Empty).Trim(), "En Proceso", StringComparison.OrdinalIgnoreCase);

                modelo.EstudiantesEnRiesgo.Add(new EstudianteRiesgoItem
                {
                    IdEstudiante = n.ID_Estudiante,
                    Estudiante = info.Nombre,
                    Curso = info.Curso ?? "Sin curso",
                    Materia = n.Materia,
                    Nota = n.Nota,
                    TienePlan = plan != null,
                    EstadoPlan = plan?.EstadoSeguimiento ?? string.Empty,
                    PlanVencido = enProceso && plan!.FechaCompromiso.HasValue && plan.FechaCompromiso.Value.Date < hoy,
                    Docente = DocenteDeClase(info.Curso, n.Materia)
                });
            }

            modelo.EstudiantesEnRiesgo = modelo.EstudiantesEnRiesgo
                .OrderBy(e => e.Curso)
                .ThenBy(e => e.Materia)
                .ThenBy(e => e.Nota)
                .ToList();

            // 2. Cobertura de notas por clase
            var notasPorClase = notasPeriodo
                .Where(n => infoEstudiante.ContainsKey(n.ID_Estudiante) && !string.IsNullOrWhiteSpace(infoEstudiante[n.ID_Estudiante].Curso))
                .GroupBy(n => (Curso: Normalizar(infoEstudiante[n.ID_Estudiante].Curso), Materia: Normalizar(n.Materia)))
                .ToDictionary(g => g.Key, g => g.Select(n => n.ID_Estudiante).Distinct().Count());

            var cursoMaterias = _context.CURSO_MATERIA
                .Select(cm => new { cm.id_curso, cm.id_materia })
                .ToList();

            var idsCursosDelAnio = anioEscolar.HasValue
                ? cursos.Where(c => c.ano_escolar == anioEscolar.Value).Select(c => c.id_curso).ToHashSet()
                : new HashSet<int>();

            foreach (var cm in cursoMaterias)
            {
                if (idsCursosDelAnio.Count > 0 && !idsCursosDelAnio.Contains(cm.id_curso))
                    continue;

                var cursoObj = cursos.FirstOrDefault(c => c.id_curso == cm.id_curso);
                var matObj = materias.FirstOrDefault(m => m.id_materia == cm.id_materia);
                if (cursoObj == null || matObj == null) continue;

                var claveCurso = Normalizar(cursoObj.nombre_curso);
                int totalEstudiantesCurso = estudiantesPorCurso.TryGetValue(claveCurso, out var ests) ? ests.Count : 0;
                if (totalEstudiantesCurso == 0) continue;

                var clave = (Curso: claveCurso, Materia: Normalizar(matObj.nombre_materia));
                int conNota = notasPorClase.TryGetValue(clave, out var c) ? c : 0;

                modelo.CoberturaNotas.Add(new CoberturaNotasItem
                {
                    Curso = cursoObj.nombre_curso,
                    Materia = matObj.nombre_materia,
                    Estudiantes = totalEstudiantesCurso,
                    ConNota = conNota,
                    Docente = DocenteDeClase(cursoObj.nombre_curso, matObj.nombre_materia)
                });
            }

            modelo.CoberturaNotas = modelo.CoberturaNotas
                .OrderBy(c => c.Porcentaje)
                .ThenBy(c => c.Curso)
                .ThenBy(c => c.Materia)
                .ToList();

            // 3. Última asistencia por curso
            var ultimaAsistencia = _context.Asistencias
                .GroupBy(a => a.id_estudiante)
                .Select(g => new { IdEstudiante = g.Key, Ultima = g.Max(a => a.fecha) })
                .ToDictionary(x => x.IdEstudiante, x => x.Ultima);

            foreach (var curso in cursos.OrderBy(c => c.nombre_curso))
            {
                if (!estudiantesPorCurso.TryGetValue(Normalizar(curso.nombre_curso), out var idsEstudiantes) || idsEstudiantes.Count == 0)
                    continue;

                var fechas = idsEstudiantes
                    .Where(id => ultimaAsistencia.ContainsKey(id))
                    .Select(id => ultimaAsistencia[id])
                    .ToList();

                modelo.AsistenciaPorCurso.Add(new AsistenciaCursoItem
                {
                    Curso = curso.nombre_curso,
                    Estudiantes = idsEstudiantes.Count,
                    ConRegistro = fechas.Count,
                    UltimaFecha = fechas.Any() ? fechas.Max() : (DateTime?)null
                });
            }

            modelo.AsistenciaPorCurso = modelo.AsistenciaPorCurso
                .OrderByDescending(a => a.UltimaFecha.HasValue ? 0 : 1)
                .ThenBy(a => a.UltimaFecha)
                .ThenBy(a => a.Curso)
                .ToList();

            return modelo;
        }

        private static bool MatriculaActiva(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return true;
            var e = estado.Trim().ToLower();
            return e == "activa" || e == "activo" || e == "vigente";
        }

        private static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>Detalle de un docente: horario, choques, actividades con sus entregas y planes de mejora.</summary>
        [HttpGet]
        [Route("DetalleDocente/{id}")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult DetalleDocente(int id)
        {
            _context.AsegurarEsquemaActividades();
            _context.AsegurarEsquemaAccionesMejora();

            var docente = _context.Usuarios.FirstOrDefault(u => u.ID_Usuario == id
                && u.ROL != null && (u.ROL.ToLower() == "docente" || u.ROL.ToLower() == "profesor"));

            if (docente == null)
            {
                TempData["Error"] = "No se encontró al docente solicitado.";
                return RedirectToAction(nameof(Supervision));
            }

            var idsProfesor = _context.PROFESOR
                .Where(p => p.id_usuario == id)
                .Select(p => p.id_profesor)
                .ToList();

            int? anioEscolar = _context.HORARIOS.Max(h => (int?)h.anio_escolar);
            var clases = anioEscolar.HasValue
                ? _context.HORARIOS.Where(h => idsProfesor.Contains(h.id_profesor) && h.anio_escolar == anioEscolar.Value).ToList()
                : new List<Horario>();

            var nombresMaterias = _context.MATERIA
                .Select(m => new { m.id_materia, m.nombre_materia })
                .ToDictionary(m => m.id_materia, m => m.nombre_materia);
            var nombresCursos = _context.Cursos
                .Select(c => new { c.id_curso, c.nombre_curso })
                .ToDictionary(c => c.id_curso, c => c.nombre_curso);
            var codigosAulas = _context.AULA
                .Select(a => new { a.id_aula, a.codigo_aula })
                .ToDictionary(a => a.id_aula, a => a.codigo_aula);

            var modelo = new DetalleDocenteViewModel
            {
                IdUsuario = docente.ID_Usuario,
                Nombre = $"{docente.NOMBRES} {docente.APELLIDOS}".Trim(),
                Correo = docente.CORREO_ELECTRONICO ?? string.Empty,
                AnioEscolar = anioEscolar,
                HorasSemanales = Math.Round(clases.Sum(h => (h.hora_fin - h.hora_inicio).TotalHours), 1)
            };

            modelo.Clases = clases
                .OrderBy(h => OrdenDia(h.dia))
                .ThenBy(h => h.hora_inicio)
                .Select(h => new ClaseDocenteItem
                {
                    Dia = (h.dia ?? string.Empty).Trim(),
                    Inicio = h.hora_inicio.ToString(@"hh\:mm"),
                    Fin = h.hora_fin.ToString(@"hh\:mm"),
                    Curso = nombresCursos.ContainsKey(h.id_curso) ? nombresCursos[h.id_curso] : $"Curso {h.id_curso}",
                    Materia = nombresMaterias.ContainsKey(h.id_materia) ? nombresMaterias[h.id_materia] : $"Materia {h.id_materia}",
                    Aula = codigosAulas.ContainsKey(h.id_aula) ? codigosAulas[h.id_aula] : string.Empty
                })
                .ToList();

            foreach (var (a, b) in BuscarChoques(clases))
            {
                modelo.Choques.Add(new ChoqueHorarioItem
                {
                    Docente = modelo.Nombre,
                    Dia = (a.dia ?? string.Empty).Trim(),
                    Detalle = $"{DescribirClase(a, nombresCursos, nombresMaterias)}   ⟷   {DescribirClase(b, nombresCursos, nombresMaterias)}"
                });
            }

            // Actividades del docente y estado de sus entregas
            var ahora = DateTime.Now;
            var limiteAtraso = ahora.Date.AddDays(-SupervisionDocentesViewModel.DiasEntregaAtrasada);

            var actividades = _context.Actividades
                .Where(a => a.Activo && a.IdDocente == id)
                .OrderByDescending(a => a.FechaCreacion)
                .ToList();
            var idsActividades = actividades.Select(a => a.IdActividad).ToList();
            var entregas = _context.EntregasActividades
                .Where(e => idsActividades.Contains(e.IdActividad))
                .Select(e => new { e.IdActividad, e.Calificacion, e.FechaEntrega })
                .ToList();

            modelo.Actividades = actividades.Select(a =>
            {
                var deLaActividad = entregas.Where(e => e.IdActividad == a.IdActividad).ToList();
                var pendientes = deLaActividad.Where(e => e.Calificacion == null).ToList();
                return new ActividadDocenteItem
                {
                    Titulo = a.Titulo,
                    Materia = a.Materia,
                    Grado = a.Grado ?? string.Empty,
                    FechaLimite = a.FechaLimite,
                    Vigente = a.FechaLimite >= ahora,
                    Entregas = deLaActividad.Count,
                    SinCalificar = pendientes.Count,
                    SinCalificarAtrasadas = pendientes.Count(e => e.FechaEntrega.Date <= limiteAtraso)
                };
            }).ToList();

            // Planes de mejora que registró el docente
            modelo.Planes = _context.AccionesMejora
                .Where(a => a.Activo && a.IdDocente == id)
                .OrderByDescending(a => a.FechaRegistro)
                .ToList()
                .Select(a => new PlanMejoraDocenteItem
                {
                    Estudiante = a.NombreEstudiante ?? $"Estudiante {a.IdEstudiante}",
                    Materia = a.Materia,
                    Grado = a.Grado ?? string.Empty,
                    Periodo = a.Periodo,
                    Estado = a.EstadoSeguimiento,
                    FechaCompromiso = a.FechaCompromiso,
                    Vencido = EsEnProceso(a.EstadoSeguimiento) && a.FechaCompromiso.HasValue && a.FechaCompromiso.Value.Date < ahora.Date
                })
                .ToList();

            return View(modelo);
        }

        // ────────────────────────────────────────────────────────────
        // EXPORTACIÓN (CSV compatible con Microsoft Excel)
        // ────────────────────────────────────────────────────────────

        [HttpGet]
        [Route("ExportarSupervision")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult ExportarSupervision()
        {
            var modelo = ConstruirSupervision();
            var filas = new List<string[]>
            {
                new[] { "Docente", "Correo", "Clases", "Horas por semana", "Choques de horario", "Materias", "Cursos",
                        "Actividades vigentes", "Actividades vencidas", "Entregas sin calificar",
                        $"Entregas sin calificar con más de {SupervisionDocentesViewModel.DiasEntregaAtrasada} días",
                        $"Notas registradas P{modelo.PeriodoActivo}",
                        "Planes de mejora registrados", "Planes de mejora vencidos", "Estado" }
            };

            foreach (var d in modelo.Docentes)
            {
                filas.Add(new[]
                {
                    d.Nombre, d.Correo, d.Clases.ToString(), Decimal1(d.HorasSemanales), d.ChoquesHorario.ToString(),
                    string.Join(", ", d.Materias), string.Join(", ", d.Cursos),
                    d.ActividadesVigentes.ToString(), d.ActividadesVencidas.ToString(),
                    d.EntregasSinCalificar.ToString(), d.EntregasSinCalificarAtrasadas.ToString(),
                    d.NotasRegistradasPeriodoActivo.ToString(),
                    d.PlanesMejoraActivos.ToString(), d.PlanesMejoraVencidos.ToString(), d.EstadoTexto
                });
            }

            return Csv($"supervision_docentes_{DateTime.Now:yyyyMMdd}.csv", filas);
        }

        [HttpGet]
        [Route("ExportarEstudiantesRiesgo")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult ExportarEstudiantesRiesgo(int? periodo)
        {
            var modelo = ConstruirSeguimiento(periodo);
            var filas = new List<string[]>
            {
                new[] { "Período", "Estudiante", "Curso", "Materia", "Nota", "Plan de mejora", "Docente" }
            };

            foreach (var e in modelo.EstudiantesEnRiesgo)
            {
                filas.Add(new[]
                {
                    modelo.Periodo.ToString(), e.Estudiante, e.Curso, e.Materia,
                    e.Nota.ToString("0.0", CultureInfo.GetCultureInfo("es-CO")), TextoPlan(e), e.Docente
                });
            }

            return Csv($"estudiantes_nota_baja_periodo{modelo.Periodo}_{DateTime.Now:yyyyMMdd}.csv", filas);
        }

        [HttpGet]
        [Route("ExportarCoberturaNotas")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult ExportarCoberturaNotas(int? periodo)
        {
            var modelo = ConstruirSeguimiento(periodo);
            var filas = new List<string[]>
            {
                new[] { "Período", "Curso", "Materia", "Docente", "Estudiantes", "Con nota", "Faltan", "Cobertura (%)" }
            };

            foreach (var c in modelo.CoberturaNotas)
            {
                filas.Add(new[]
                {
                    modelo.Periodo.ToString(), c.Curso, c.Materia, c.Docente,
                    c.Estudiantes.ToString(), c.ConNota.ToString(), c.Faltan.ToString(), c.Porcentaje.ToString()
                });
            }

            return Csv($"cobertura_notas_periodo{modelo.Periodo}_{DateTime.Now:yyyyMMdd}.csv", filas);
        }

        private FileContentResult Csv(string nombreArchivo, IEnumerable<string[]> filas)
        {
            var sb = new StringBuilder();
            foreach (var fila in filas)
                sb.AppendLine(string.Join(";", fila.Select(CeldaCsv)));

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
        }

        private static string CeldaCsv(string? valor)
        {
            valor ??= string.Empty;

            if (valor.Length > 0 && "=+-@".IndexOf(valor[0]) >= 0)
                valor = "'" + valor;

            if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
                valor = "\"" + valor.Replace("\"", "\"\"") + "\"";

            return valor;
        }

        private static string Decimal1(double valor)
        {
            return valor.ToString("0.0", CultureInfo.GetCultureInfo("es-CO"));
        }

        private static string TextoPlan(EstudianteRiesgoItem e)
        {
            if (!e.TienePlan) return "Sin plan";
            if (e.PlanVencido) return "Plan vencido";
            return string.IsNullOrWhiteSpace(e.EstadoPlan) ? "Con plan" : e.EstadoPlan;
        }

        private static int OrdenDia(string? dia)
        {
            switch (Normalizar(dia))
            {
                case "lunes": return 1;
                case "martes": return 2;
                case "miercoles": return 3;
                case "jueves": return 4;
                case "viernes": return 5;
                case "sabado": return 6;
                case "domingo": return 7;
                default: return 9;
            }
        }

        private static bool EsEnProceso(string? estado)
        {
            return string.Equals((estado ?? string.Empty).Trim(), "En Proceso", StringComparison.OrdinalIgnoreCase);
        }

        private static List<(Horario A, Horario B)> BuscarChoques(List<Horario> clases)
        {
            var choques = new List<(Horario A, Horario B)>();

            foreach (var grupo in clases.GroupBy(h => (h.dia ?? string.Empty).Trim().ToLower()))
            {
                var lista = grupo.OrderBy(h => h.hora_inicio).ToList();

                for (int i = 0; i < lista.Count; i++)
                {
                    for (int j = i + 1; j < lista.Count; j++)
                    {
                        var a = lista[i];
                        var b = lista[j];

                        if (b.hora_inicio >= a.hora_fin)
                            break;

                        if (a.periodo_academico.HasValue && b.periodo_academico.HasValue && a.periodo_academico != b.periodo_academico)
                            continue;

                        choques.Add((a, b));
                    }
                }
            }

            return choques;
        }

        private static string DescribirClase(Horario h, Dictionary<int, string> cursos, Dictionary<int, string> materias)
        {
            string curso = cursos.ContainsKey(h.id_curso) ? cursos[h.id_curso] : $"Curso {h.id_curso}";
            string materia = materias.ContainsKey(h.id_materia) ? materias[h.id_materia] : $"Materia {h.id_materia}";
            return $"{curso} · {materia} ({h.hora_inicio:hh\\:mm}-{h.hora_fin:hh\\:mm})";
        }
    }
}
