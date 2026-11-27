using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("ESTUDIANTE")]
    public class Estudiante
    {
        [Key]
        public int id_estudiante { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellido { get; set; } = string.Empty;
        public string? email { get; set; }
        public int? id_usuario { get; set; }
    }
}