using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("EXTRA_INSCRIPCION")]
    public class ExtraInscripcion
    {
        [Key]
        [Column("id_extra_inscripcion")]
        public int IdExtraInscripcion { get; set; }

        [Column("id_estudiante")]
        public int IdEstudiante { get; set; }

        [Column("id_extra_curso")]
        public int IdExtraCurso { get; set; }

        [Column("fecha_inscripcion")]
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;

        [Column("estado")]
        [StringLength(30)]
        public string Estado { get; set; } = "Inscrito"; // "Inscrito", "Cancelado"

        [Column("fecha_cancelacion")]
        public DateTime? FechaCancelacion { get; set; }

        [Column("motivo_cancelacion")]
        [StringLength(300)]
        public string? MotivoCancelacion { get; set; }

        [Column("id_extra_curso_nuevo")]
        public int? IdExtraCursoNuevo { get; set; }
    }
}
