using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.Controllers
{
    /// <summary>
    /// Módulo del coordinador y administración académica.
    /// Supervisión docente, control y coordinación de periodos de subida de notas,
    /// plazos y seguimiento académico.
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
        /// clases, actividades vigentes y seguimiento de docentes con calificaciones pendientes.
        /// </summary>
        [Route("")]
        [Route("Supervision")]
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult Supervision()
        {
            _context.AsegurarEsquemaActividades();
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
            List<ProfesorMateria> materiasHabilitadas = new List<ProfesorMateria>();
            try
            {
                materiasHabilitadas = _context.PROFESOR_MATERIA
                    .Select(pm => new ProfesorMateria { id_profesor = pm.id_profesor, id_materia = pm.id_materia })
                    .ToList();
            }
            catch { }

            var nombresMaterias = _context.MATERIA
                .AsEnumerable()
                .GroupBy(m => m.id_materia)
                .ToDictionary(g => g.Key, g => g.First().nombre_materia);

            var nombresCursos = _context.Cursos
                .AsEnumerable()
                .GroupBy(c => c.id_curso)
                .ToDictionary(g => g.Key, g => g.First().nombre_curso);

            var actividades = _context.Actividades.Where(a => a.Activo && a.IdDocente != null).ToList();
            var entregasSinCalificar = _context.EntregasActividades
                .Where(e => e.Calificacion == null)
                .Select(e => e.IdActividad)
                .ToList();

            // Calificaciones del periodo activo para monitorear subida de notas
            var calificacionesPeriodoActivo = _context.Calificaciones
                .Where(c => c.Periodo == modelo.PeriodoActivo)
                .ToList();

            var ahora = DateTime.Now;

            foreach (var docente in docentes)
            {
                string correoDoc = (docente.CORREO_ELECTRONICO ?? "").Trim().ToLower();
                string nombresDoc = (docente.NOMBRES ?? "").Trim().ToLower();
                string apellidosDoc = (docente.APELLIDOS ?? "").Trim().ToLower();
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

                var clases = horarios.Where(h => idsProfesor.Contains(h.id_profesor)).ToList();

                var idsMaterias = clases.Select(h => h.id_materia)
                    .Concat(materiasHabilitadas.Where(pm => idsProfesor.Contains(pm.id_profesor)).Select(pm => pm.id_materia))
                    .Distinct()
                    .ToList();

                var materiasDocenteNombres = idsMaterias
                    .Where(id => nombresMaterias.ContainsKey(id))
                    .Select(id => nombresMaterias[id].ToLower().Trim())
                    .ToList();

                var actividadesDocente = actividades.Where(a => a.IdDocente == docente.ID_Usuario).ToList();
                var idsActividades = actividadesDocente.Select(a => a.IdActividad).ToList();

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
                    Nombre = $"{docente.NOMBRES} {docente.APELLIDOS}".Trim(),
                    Correo = docente.CORREO_ELECTRONICO ?? string.Empty,
                    Clases = clases.Count,
                    Materias = idsMaterias
                        .Where(id => nombresMaterias.ContainsKey(id))
                        .Select(id => nombresMaterias[id])
                        .OrderBy(n => n)
                        .ToList(),
                    Cursos = cursosDocente,
                    ActividadesVigentes = actividadesDocente.Count(a => a.FechaLimite >= ahora),
                    ActividadesVencidas = actividadesDocente.Count(a => a.FechaLimite < ahora),
                    EntregasSinCalificar = entregasSinCalificar.Count(id => idsActividades.Contains(id)),
                    UltimaActividad = actividadesDocente.Any() ? actividadesDocente.Max(a => a.FechaCreacion) : (DateTime?)null,
                    NotasRegistradasPeriodoActivo = notasDocentePeriodo
                });
            }

            modelo.TotalEstudiantes = _context.Usuarios
                .Count(u => u.ROL != null && u.ROL.ToLower() == "estudiante" && u.ACTIVO);

            return View(modelo);
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
    }
}
