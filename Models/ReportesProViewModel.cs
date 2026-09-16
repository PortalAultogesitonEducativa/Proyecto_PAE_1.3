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

    public class ObservadorPdfViewModel
    {
        public string Institucion { get; set; } = "INSTITUCIÓN EDUCATIVA SOR MARÍA JULIANA";
        public string Sede { get; set; } = "SEDE: SOR MARÍA JULIANA";
        public string Titulo { get; set; } = "HOJA DE VIDA OBSERVADOR DEL ALUMNO";
        public string Ciudad { get; set; } = "Cartago";
        public string Jornada { get; set; } = "Mañana";
        public string Grupo { get; set; } = "601 M";
        public string AnoLectivo { get; set; } = DateTime.Now.Year.ToString();
        public string DirGrupo { get; set; } = "Claudia Milena Londoño C.";
        public string Calendario { get; set; } = "A";
        public string EstudianteNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string MatriculaNo { get; set; } = string.Empty;
        public string LugarNacimiento { get; set; } = "Cartago";
        public string FechaNacimiento { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string Acudiente { get; set; } = string.Empty;
        public string IdentificacionAcudiente { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefonos { get; set; } = string.Empty;
        public List<ObservadorPeriodoItem> Periodos { get; set; } = new List<ObservadorPeriodoItem>();
    }

    public class ObservadorPeriodoItem
    {
        public string NombrePeriodo { get; set; } = string.Empty; // "PRIMER PERIODO", "SEGUNDO PERIODO", etc.
        public List<ObservacionDetalleItem> Observaciones { get; set; } = new List<ObservacionDetalleItem>();
    }

    public class ObservacionDetalleItem
    {
        public string Fecha { get; set; } = string.Empty;
        public string TipoNota { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? AspectosMejorar { get; set; }
        public string? FirmaDocente { get; set; }
    }
}
