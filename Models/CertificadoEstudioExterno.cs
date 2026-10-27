using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("CERTIFICADO_ESTUDIO_EXTERNO")]
    public class CertificadoEstudioExterno
    {
        [Key]
        [Column("id_certificado_externo")]
        public int IdCertificadoExterno { get; set; }

        [Column("id_estudiante")]
        [Required]
        public int IdEstudiante { get; set; }

        [Column("grado_aprobado")]
        [Required]
        public int GradoAprobado { get; set; }

        [Column("ano_lectivo")]
        [Required]
        public int AnoLectivo { get; set; }

        [Column("institucion_origen")]
        [Required]
        [MaxLength(200)]
        public string InstitucionOrigen { get; set; } = "";

        [Column("archivo_ruta")]
        [MaxLength(300)]
        public string? ArchivoRuta { get; set; }

        [Column("estado_verificacion")]
        [Required]
        [MaxLength(30)]
        public string EstadoVerificacion { get; set; } = "Pendiente de verificación"; // "Pendiente de verificación", "Verificado", "Rechazado"

        [Column("fecha_verificacion")]
        public DateTime? FechaVerificacion { get; set; }

        [Column("revisado_por")]
        [MaxLength(150)]
        public string? RevisadoPor { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [ForeignKey("IdEstudiante")]
        public virtual Estudiante? Estudiante { get; set; }
    }
}
