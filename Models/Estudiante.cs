using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("ESTUDIANTE")]
    public class Estudiante
    {
        [Key]
        public int id_estudiante { get; set; } // Nombre exacto en SQL
        public string nombre { get; set; } = string.Empty;
        public string apellido { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string codigo_estudiante { get; set; } = string.Empty;
        public DateTime fecha_inscripcion { get; set; }
        public int? id_usuario { get; set; } // enlaza con GU_Usuario.ID_Usuario

        [NotMapped]
        public int? id_curso { get; set; } // Curso asignado al estudiante

        [NotMapped]
        public string? curso_asignado { get; set; } // Nombre o código de curso asignado (ej: "601 M", "1102 T")
    }
}