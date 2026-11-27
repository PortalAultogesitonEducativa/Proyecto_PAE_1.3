using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("CURSO")]
    public class Curso
    {
        [Key]
        public int id_curso { get; set; }
        public string nombre_curso { get; set; } = string.Empty;
        public int ano_escolar { get; set; }
        public int id_periodo { get; set; }
    }
}