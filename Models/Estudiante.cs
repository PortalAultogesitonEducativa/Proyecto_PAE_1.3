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
    }
}