using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("DOCUMENTO")]
    public class Documento
    {
        [Key]
        public int id_documento { get; set; }
        public string titulo { get; set; } = string.Empty;
        public string? descripcion { get; set; }
        public string tipo_documento { get; set; } = string.Empty;
        public string estado_documento { get; set; } = string.Empty;
        public DateTime fecha_subida { get; set; }
        public int? id_curso { get; set; }
    }
}