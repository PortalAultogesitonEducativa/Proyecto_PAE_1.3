using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("MATRICULA")]
    public class Matricula
    {
        [Key]
        public int id_matricula { get; set; }
        public int id_estudiante { get; set; }
        public int id_curso { get; set; }
        public string estado { get; set; } = string.Empty;
        public int anio { get; set; }
    }
}