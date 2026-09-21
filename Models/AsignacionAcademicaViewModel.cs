using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace ProyectoPAE.Models
{
    public class AsignacionAcademicaViewModel
    {
        // Propiedad para identificar qué pestaña estuvo activa
        public string TabActiva { get; set; } = "docentes";

        // ── CAMPOS PARA ASIGNACIÓN DE DOCENTES ──
        public int IdProfesor { get; set; }
        public int IdMateria { get; set; }
        public int IdCurso { get; set; }
        public int IdAula { get; set; }
        public string DiaSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }

        public List<SelectListItem> ListadoDocentes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListadoAulas { get; set; } = new List<SelectListItem>();

        // ── CAMPOS PARA ASIGNACIÓN DE ALUMNOS ──
        public int IdEstudiante { get; set; }
        public int IdCursoEstudiante { get; set; }

        public List<SelectListItem> ListadoEstudiantes { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ListadoCursos { get; set; } = new List<SelectListItem>();
    }
}