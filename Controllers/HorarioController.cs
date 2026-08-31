using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoPAE.Controllers
{
    public class HorarioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HorarioController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult MiHorario()
        {
            try
            {
                // 1. Obtener materias y profesores desde la base de datos
                var relacionProfesorMateria = (from pm in _context.PROFESOR_MATERIA
                                               join m in _context.MATERIA on pm.id_materia equals m.id_materia into mGroup
                                               from materia in mGroup.DefaultIfEmpty()
                                               join p in _context.PROFESOR on pm.id_profesor equals p.id_profesor into pGroup
                                               from profesor in pGroup.DefaultIfEmpty()
                                               select new
                                               {
                                                   NombreMateria = materia != null ? materia.nombre_materia : "Asignatura",
                                                   NombreProfesor = profesor != null ? $"{profesor.nombre} {profesor.apellido}" : "Docente Asignado"
                                               }).ToList();

                // 2. Obtener lista de aulas
                var listaAulas = _context.AULA.ToList();

                // 3. Definición de bloques incluyendo la franja de descanso
                var bloques = new[]
                {
                    new { Inicio = new TimeSpan(6, 0, 0), Fin = new TimeSpan(7, 30, 0), EsDescanso = false },
                    new { Inicio = new TimeSpan(7, 30, 0), Fin = new TimeSpan(9, 0, 0), EsDescanso = false },
                    new { Inicio = new TimeSpan(9, 0, 0), Fin = new TimeSpan(9, 30, 0), EsDescanso = true }, // Receso
                    new { Inicio = new TimeSpan(9, 30, 0), Fin = new TimeSpan(10, 45, 0), EsDescanso = false },
                    new { Inicio = new TimeSpan(10, 45, 0), Fin = new TimeSpan(12, 0, 0), EsDescanso = false }
                };

                var dias = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };
                var resultadoViewModel = new List<HorarioDetalleViewModel>();
                int contador = 0;

                // 4. Construir el modelo
                foreach (var bloque in bloques)
                {
                    foreach (var dia in dias)
                    {
                        if (bloque.EsDescanso)
                        {
                            resultadoViewModel.Add(new HorarioDetalleViewModel
                            {
                                DiaSemana = dia,
                                HoraInicio = bloque.Inicio,
                                HoraFin = bloque.Fin,
                                NombreMateria = "RECESO / DESCANSO",
                                NombreProfesor = string.Empty,
                                NombreSalon = string.Empty,
                                EsDescanso = true
                            });
                        }
                        else
                        {
                            var pmInfo = relacionProfesorMateria.Count > 0
                                ? relacionProfesorMateria[contador % relacionProfesorMateria.Count]
                                : null;

                            var aulaInfo = listaAulas.Count > 0
                                ? listaAulas[contador % listaAulas.Count]
                                : null;

                            string nombreSalon = aulaInfo != null
                                ? $"{aulaInfo.codigo_aula} ({aulaInfo.tipo})"
                                : "Aula General";

                            resultadoViewModel.Add(new HorarioDetalleViewModel
                            {
                                DiaSemana = dia,
                                HoraInicio = bloque.Inicio,
                                HoraFin = bloque.Fin,
                                NombreMateria = pmInfo != null ? pmInfo.NombreMateria : "Materia General",
                                NombreProfesor = pmInfo != null ? pmInfo.NombreProfesor : "Docente por Asignar",
                                NombreSalon = nombreSalon,
                                EsDescanso = false
                            });

                            contador++;
                        }
                    }
                }

                return View(resultadoViewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "No se pudo cargar el horario: " + ex.Message;
                return View(new List<HorarioDetalleViewModel>());
            }
        }
    }
}

