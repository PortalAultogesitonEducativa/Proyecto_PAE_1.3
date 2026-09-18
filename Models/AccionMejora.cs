using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("ACCION_MEJORA")]
    public class AccionMejora
    {
        [Key]
        [Column("id_mejora")]
        public int IdMejora { get; set; }

        [Column("id_estudiante")]
        public int IdEstudiante { get; set; }

        [Column("nombre_estudiante")]
        public string? NombreEstudiante { get; set; }

        [Column("id_docente")]
        public int? IdDocente { get; set; }

        [Column("docente")]
        public string? Docente { get; set; }

        [Column("materia")]
        public string Materia { get; set; } = string.Empty;

        [Column("grado")]
        public string? Grado { get; set; }

        [Column("periodo")]
        public int Periodo { get; set; } = 1;

        [Column("aspecto_mejorar")]
        public string AspectoMejorar { get; set; } = string.Empty;

        [Column("compromiso_estudiante")]
        public string CompromisoEstudiante { get; set; } = string.Empty;

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("fecha_compromiso")]
        public DateTime? FechaCompromiso { get; set; }

        [Column("estado_seguimiento")]
        public string EstadoSeguimiento { get; set; } = "En Proceso"; // "En Proceso", "Cumplido", "No Cumplido"

        [Column("observacion_seguimiento")]
        public string? ObservacionSeguimiento { get; set; }

        [Column("fecha_seguimiento")]
        public DateTime? FechaSeguimiento { get; set; }

        [Column("respuesta_estudiante")]
        public string? RespuestaEstudiante { get; set; }

        [Column("fecha_respuesta_estudiante")]
        public DateTime? FechaRespuestaEstudiante { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }

    public class AccionMejoraRegistroDto
    {
        public int IdEstudiante { get; set; }
        public string? NombreEstudiante { get; set; }
        public string Materia { get; set; } = string.Empty;
        public string? Grado { get; set; }
        public int Periodo { get; set; } = 1;
        public string AspectoMejorar { get; set; } = string.Empty;
        public string CompromisoEstudiante { get; set; } = string.Empty;
        public DateTime? FechaCompromiso { get; set; }
    }

    public class SeguimientoMejoraDto
    {
        public int IdMejora { get; set; }
        public string EstadoSeguimiento { get; set; } = "Cumplido"; // Cumplido, No Cumplido, En Proceso
        public string? ObservacionSeguimiento { get; set; }
    }

    public class AvanceEstudianteMejoraDto
    {
        public int IdMejora { get; set; }
        public string RespuestaEstudiante { get; set; } = string.Empty;
    }
}
