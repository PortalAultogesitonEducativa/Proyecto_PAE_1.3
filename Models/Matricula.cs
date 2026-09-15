using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 
namespace ProyectoPAE.Models
{
    [Table("MATRICULA")]
    public class Matricula
    {
        [Key]
        public int id_matricula { get; set; }
        public int id_estudiante { get; set; }
        public int id_curso { get; set; }
        public DateTime fecha_matricula { get; set; }
        public string periodo_academico { get; set; } = "";
        public string estado { get; set; } = "";
 
        [Column("anio")]
        public int ano { get; set; }
    }
}