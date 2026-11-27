using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("PROFESOR")]
    public class Profesor
    {
        [Key]
        public int id_profesor { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string apellido { get; set; } = string.Empty;
    }
}