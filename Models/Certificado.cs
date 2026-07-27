using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("CERTIFICADO")]
    public class Certificado
    {
        [Key]
        [Column("id_certificado")]
        public int IdCertificado { get; set; }

        [Column("id_estudiante")]
        public int IdEstudiante { get; set; }

        [Column("tipo_certificado")]
        [MaxLength(50)]
        public string TipoCertificado { get; set; }

        [Column("codigo_verificacion")]
        [MaxLength(20)]
        public string CodigoVerificacion { get; set; }

        [Column("hash_contenido")]
        [MaxLength(100)]
        public string HashContenido { get; set; }

        [Column("fecha_emision")]
        public DateTime FechaEmision { get; set; }

        [Column("id_usuario_emisor")]
        public int IdUsuarioEmisor { get; set; }

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "Vigente";

        // Navegación — el "estudiante" en este sistema es un Usuario con ROL = 'estudiante'
        [ForeignKey("IdEstudiante")]
        public virtual Usuario Estudiante { get; set; }

        public virtual ICollection<CertificadoDetalle> Detalles { get; set; } = new List<CertificadoDetalle>();
    }
}