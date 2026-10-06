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
        public int id_curso { get; set; }

        [Column("fecha_matricula")]
        public DateTime fecha_matricula { get; set; } = DateTime.Now;

        [Column("periodo_academico")]
        public string? periodo_academico { get; set; } = "";

        [Column("estado")]
        public string? estado { get; set; } = "";

        [Column("anio")]
        public int ano { get; set; }

        [NotMapped]
        public int? anio
        {
            get => ano;
            set => ano = value ?? 0;
        }
    }
}