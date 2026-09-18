using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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
                return View(new List<HorarioDetalleViewModel>());
            }
        }
    }
}

