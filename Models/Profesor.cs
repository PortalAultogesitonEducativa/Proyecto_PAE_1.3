using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ProyectoPAE.Models
{
    [Table("PROFESOR")]
    public class Profesor
    {
        [Key]
        public int id_profesor { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellido { get; set; } = string.Empty;
        public string? direccion { get; set; }
        public string? telefono { get; set; }
        public string email { get; set; } = string.Empty;
        public string? especialidad { get; set; }
        public int id_departamento { get; set; } = 1;
        public int? id_usuario { get; set; }
    }
}
