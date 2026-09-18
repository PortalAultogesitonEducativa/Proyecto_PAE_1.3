using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("CERTIFICADO_DETALLE")]
    public class CertificadoDetalle
    {
        [Key]
        [Column("id_detalle")]
        public int IdDetalle { get; set; }

        [Column("id_certificado")]
        public int IdCertificado { get; set; }

        [Column("id_calificacion")]
        public int IdCalificacion { get; set; }

        [Column("materia")]
        [MaxLength(80)]
        public string Materia { get; set; }

        [Column("periodo")]
        [MaxLength(20)]
        public string Periodo { get; set; }

        [Column("nota_final")]
        public decimal NotaFinal { get; set; }

        [Column("estado_nota")]
        [MaxLength(20)]
        public string EstadoNota { get; set; }

        // Navegación
        [ForeignKey("IdCertificado")]
        public virtual Certificado Certificado { get; set; }

        [ForeignKey("IdCalificacion")]
        public virtual Calificacion Calificacion { get; set; }
    }
}