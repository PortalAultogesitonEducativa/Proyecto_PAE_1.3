using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("ASISTENCIA")]
    public class Asistencia
    {
        [Key]
        public int id_asistencia { get; set; }
        public int id_estudiante { get; set; }
        public int id_horario { get; set; }
        public int id_profesor { get; set; }
        public DateTime fecha { get; set; }
        public string estado { get; set; } = string.Empty;
        public TimeSpan? hora_llegada { get; set; }
        public string? justificacion { get; set; }
    }
}