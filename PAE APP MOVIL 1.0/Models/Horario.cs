using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("HORARIO")]
    public class Horario
    {
        [Key]
        public int id_horario { get; set; }
        public int id_curso { get; set; }
        public int id_aula { get; set; }
        public int id_profesor { get; set; }
        public string dia { get; set; } = string.Empty;
        public TimeSpan hora_inicio { get; set; }
        public TimeSpan hora_fin { get; set; }
    }
}