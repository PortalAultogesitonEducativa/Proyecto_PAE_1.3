using System;
using System.Collections.Generic;

namespace ProyectoPAE.Models
{
    public class ExtraCursoItemViewModel
    {
        public int IdExtraCurso { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Instructor { get; set; }
        public int CuposTotales { get; set; }
        public int CuposDisponibles { get; set; }
        public int CuposOcupados => CuposTotales - CuposDisponibles;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime FechaInicioInscripcion { get; set; }
        public DateTime FechaFinInscripcion { get; set; }
        public bool Activo { get; set; }
        public int? GradoMin { get; set; }
        public int? GradoMax { get; set; }
        public string? Horario { get; set; }
        public int? IdDocente { get; set; }

        // Estado del estudiante actual respecto a este curso
        public bool EstaInscrito { get; set; }
        public int? IdInscripcion { get; set; }
        public DateTime? FechaInscripcionEstudiante { get; set; }

        // Validación de estado de inscripción
        public bool PeriodoInscripcionAbierto => DateTime.Now.Date >= FechaInicioInscripcion.Date && DateTime.Now.Date <= FechaFinInscripcion.Date;
        public bool TieneCupos => CuposDisponibles > 0;
        public bool EsGradoValido(int? gradoEstudiante)
        {
            if (!gradoEstudiante.HasValue || gradoEstudiante.Value <= 0) return true;
            if (GradoMin.HasValue && gradoEstudiante.Value < GradoMin.Value) return false;
            if (GradoMax.HasValue && gradoEstudiante.Value > GradoMax.Value) return false;
            return true;
        }
    }

    public class ExtraEstudianteInscritoDto
    {
        public int IdInscripcion { get; set; }
        public int IdEstudiante { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public DateTime FechaInscripcion { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
