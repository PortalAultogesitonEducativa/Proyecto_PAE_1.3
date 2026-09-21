using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("AULA")]
    public class Aula
    {
        [Key]
        public int id_aula { get; set; }
        public string codigo_aula { get; set; } = string.Empty;
        public int capacidad { get; set; }
        public string tipo { get; set; } = string.Empty;
        public string? ubicacion { get; set; }
    }
}