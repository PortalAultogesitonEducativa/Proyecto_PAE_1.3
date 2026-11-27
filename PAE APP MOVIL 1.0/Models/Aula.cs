using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("AULA")]
    public class Aula
    {
        [Key]
        public int id_aula { get; set; }
        public string codigo_aula { get; set; } = string.Empty;
        public string? ubicacion { get; set; }
    }
}