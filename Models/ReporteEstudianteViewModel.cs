using System.Collections.Generic;

namespace ProyectoPAE.Models
{
    public class ReporteEstudianteViewModel
    {
        public int IdEstudiante { get; set; }
        public string NombreCompleto { get; set; }
        public string Codigo { get; set; }
        // Esta lista guardará cada fila de la tabla (Materia + Notas)
        public List<FilaNota> NotasPorMateria { get; set; } = new List<FilaNota>();
    }

    public class FilaNota
    {
        public string NombreMateria { get; set; }
        public double Periodo1 { get; set; }
        public double Periodo2 { get; set; }
        public double Periodo3 { get; set; }
        public double Periodo4 { get; set; }
        // Cálculo automático de la definitiva
        public double Definitiva => (Periodo1 + Periodo2 + Periodo3 + Periodo4) / 4;
    }
}