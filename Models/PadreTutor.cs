using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("PADRE_TUTOR")]
    public class PadreTutor
    {
        [Key]
        public int id_padre { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellido { get; set; } = string.Empty;
        public string? telefono { get; set; }
        public string? email { get; set; }
        public string? relacion_estudiante { get; set; }
        public int? id_usuario { get; set; } // enlaza con GU_Usuario.ID_Usuario
    }
}
