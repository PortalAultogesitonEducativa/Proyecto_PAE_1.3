using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("EXTRA_CURSO")]
    public class ExtraCurso
    {
        [Key]
        [Column("id_extra_curso")]
        public int IdExtraCurso { get; set; }

        [Column("nombre")]
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        [StringLength(500)]
        public string? Descripcion { get; set; } // Qué se va a aprender / Temario / Objetivos

        [Column("instructor")]
        [StringLength(150)]
        public string? Instructor { get; set; }

        [Column("cupos_totales")]
        public int CuposTotales { get; set; }

        [Column("cupos_disponibles")]
        public int CuposDisponibles { get; set; }

        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateTime FechaFin { get; set; }

        [Column("fecha_inicio_inscripcion")]
        public DateTime FechaInicioInscripcion { get; set; }

        [Column("fecha_fin_inscripcion")]
        public DateTime FechaFinInscripcion { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("id_periodo")]
        public int? IdPeriodo { get; set; }

        [Column("id_aula")]
        public int? IdAula { get; set; }

        // Nuevas columnas opcionales para rango de grados y horario
        [Column("grado_min")]
        public int? GradoMin { get; set; } = 6;

        [Column("grado_max")]
        public int? GradoMax { get; set; } = 11;

        [Column("horario")]
        [StringLength(100)]
        public string? Horario { get; set; } // Ej: "Martes y Jueves 3:00 PM - 5:00 PM"

        [Column("id_docente")]
        public int? IdDocente { get; set; }

        [Column("tipo_curso")]
        [StringLength(50)]
        public string? TipoCurso { get; set; } = "Deportivo"; // "Deportivo", "Matemático", "Artístico", "Servicio Social", "Académico"

        [Column("documentos_requeridos")]
        [StringLength(500)]
        public string? DocumentosRequeridos { get; set; } // Lista separada por comas de códigos de docs requeridos
    }
}
