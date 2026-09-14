using System.Collections.Generic;

namespace ProyectoPAE.Models
{
    public class ReportesProViewModel
    {
        // 1. Rendimiento Académico
        public int TotalEstudiantes { get; set; }
        public double PromedioInstitucional { get; set; }
        public List<MateriaPromedioItem> PromediosPorMateria { get; set; } = new List<MateriaPromedioItem>();
        public int TotalCalificaciones { get; set; }
        public int Aprobados { get; set; }
        public int Reprobados { get; set; }
        public double PorcentajeAprobacion { get; set; }
        public double PorcentajeReprobacion { get; set; }

        // 2. Convivencia y Permanencia
        public int TotalAsistencias { get; set; }
        public int AsistenciasPresentes { get; set; }
        public double PorcentajeAsistencia { get; set; }
        public int TotalMatriculas { get; set; }
        public int Desertores { get; set; }
        public double PorcentajeDesercion { get; set; }

        // 3. Matrícula por Grado y Género
        public Dictionary<int, int> MatriculasPorGrado { get; set; } = new Dictionary<int, int>();
        public int TotalHombres { get; set; }
        public int TotalMujeres { get; set; }
        public int TotalSinGenero { get; set; }
    }

    public class MateriaPromedioItem
    {
        public string Materia { get; set; } = string.Empty;
        public double Promedio { get; set; }
    }
}
