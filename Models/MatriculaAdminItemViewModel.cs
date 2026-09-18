using System;

namespace ProyectoPAE.Models
{
    public class MatriculaAdminItemViewModel
    {
        public int IdMatricula { get; set; }
        public int IdEstudiante { get; set; }
        public string NombreEstudiante { get; set; } = "";
        public string CodigoEstudiante { get; set; } = "";
        public int Grado { get; set; }
        public DateTime FechaMatricula { get; set; }
        public string PeriodoAcademico { get; set; } = "";
        public string Estado { get; set; } = "";
    }
}

