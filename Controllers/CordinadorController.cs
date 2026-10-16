using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.Controllers
{
    /// <summary>
    /// Módulo del coordinador. Todo lo que hay aquí es de consulta: el coordinador supervisa,
    /// no escribe notas, asistencia ni observaciones (eso sigue siendo del docente).
    /// </summary>
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoordinadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Carga académica y trabajo registrado de cada docente: clases, materias, cursos,
        /// actividades vigentes o vencidas y entregas que todavía no han sido calificadas.
        /// </summary>
        [RequierePermiso(Permisos.SupervisarDocentes)]
        public IActionResult Supervision()
        {
            _context.AsegurarEsquemaActividades();

            var docentes = _context.Usuarios
                .Where(u => u.ROL != null && (u.ROL.ToLower() == "docente" || u.ROL.ToLower() == "profesor") && u.ACTIVO)
                .OrderBy(u => u.APELLIDOS)
                .ThenBy(u => u.NOMBRES)
                .ToList();

            var profesores = _context.PROFESOR.Where(p => p.id_usuario != null).ToList();
            var horarios = _context.HORARIOS.ToList();
            var materiasHabilitadas = _context.PROFESOR_MATERIA.ToList();
            var nombresMaterias = _context.MATERIA.ToDictionary(m => m.id_materia, m => m.nombre_materia);
            var nombresCursos = _context.Cursos.ToDictionary(c => c.id_curso, c => c.nombre_curso);

            var actividades = _context.Actividades.Where(a => a.Activo && a.IdDocente != null).ToList();
            var entregasSinCalificar = _context.EntregasActividades
                .Where(e => e.Calificacion == null)
                .Select(e => e.IdActividad)
                .ToList();

            var ahora = DateTime.Now;
            var modelo = new SupervisionDocentesViewModel();

            foreach (var docente in docentes)
            {
                var idsProfesor = profesores
                    .Where(p => p.id_usuario == docente.ID_Usuario)
                    .Select(p => p.id_profesor)
                    .ToList();

                var clases = horarios.Where(h => idsProfesor.Contains(h.id_profesor)).ToList();

                var idsMaterias = clases.Select(h => h.id_materia)
                    .Concat(materiasHabilitadas.Where(pm => idsProfesor.Contains(pm.id_profesor)).Select(pm => pm.id_materia))
                    .Distinct()
                    .ToList();

                var actividadesDocente = actividades.Where(a => a.IdDocente == docente.ID_Usuario).ToList();
                var idsActividades = actividadesDocente.Select(a => a.IdActividad).ToList();

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
                    Cursos = clases.Select(h => h.id_curso)
                        .Distinct()
                        .Where(id => nombresCursos.ContainsKey(id))
                        .Select(id => nombresCursos[id])
                        .OrderBy(n => n)
                        .ToList(),
                    ActividadesVigentes = actividadesDocente.Count(a => a.FechaLimite >= ahora),
                    ActividadesVencidas = actividadesDocente.Count(a => a.FechaLimite < ahora),
                    EntregasSinCalificar = entregasSinCalificar.Count(id => idsActividades.Contains(id)),
                    UltimaActividad = actividadesDocente.Any() ? actividadesDocente.Max(a => a.FechaCreacion) : (DateTime?)null
                });
            }

            modelo.TotalEstudiantes = _context.Usuarios
                .Count(u => u.ROL != null && u.ROL.ToLower() == "estudiante" && u.ACTIVO);

            return View(modelo);
        }
    }
}