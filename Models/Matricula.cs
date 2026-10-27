using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("MATRICULA")]
    public class Matricula
    {
        [Key]
        [Column("id_matricula")]
        public int id_matricula { get; set; }

        [Column("id_estudiante")]
        public int id_estudiante { get; set; }

        [Column("id_curso")]
        public int? id_curso { get; set; }

        [Column("fecha_matricula")]
        public DateTime fecha_matricula { get; set; } = DateTime.Now;

        [Column("periodo_academico")]
        public string? periodo_academico { get; set; } = "";

        [Column("estado")]
        public string? estado { get; set; } = "Registrada"; // "Registrada", "Pendiente", "Activa", "Finalizada", "Cancelada"

        [Column("anio")]
        public int ano { get; set; }

        [Column("estado_academico")]
        public string estado_academico { get; set; } = "Pendiente de resultado"; // "Pendiente de resultado", "Aprobado", "Reprobado"

        [Column("estado_documental")]
        public string estado_documental { get; set; } = "Pendiente de revisión"; // "Pendiente de revisión", "En revisión", "Pendiente de documentos", "Aprobada", "Rechazada"

        [Column("grado_solicitado")]
        public int? grado_solicitado { get; set; }

        [Column("observaciones")]
        public string? observaciones { get; set; }

        [Column("documentos_pendientes")]
        public string? documentos_pendientes { get; set; }

        [Column("fecha_revision_documental")]
        public DateTime? fecha_revision_documental { get; set; }

        [Column("revisado_por")]
        public string? revisado_por { get; set; }

        [NotMapped]
        public int? anio
        {
            get => ano;
            set => ano = value ?? 0;
        }

        [ForeignKey("id_estudiante")]
        public virtual Estudiante? Estudiante { get; set; }

        [ForeignKey("id_curso")]
        public virtual Curso? Curso { get; set; }
    }
}