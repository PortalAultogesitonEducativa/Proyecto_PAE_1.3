using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    /// <summary>
    /// Solicitud de cambio de correo electrónico. El cambio solo se aplica cuando el usuario confirma
    /// el enlace enviado al correo nuevo. El enlace no se guarda: solo su hash (SHA-256).
    /// </summary>
    [Table("GU_CAMBIO_CORREO")]
    public class CambioCorreo
    {
        [Key]
        [Column("id_cambio")]
        public int IdCambio { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(150)]
        [Column("correo_anterior")]
        public string CorreoAnterior { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Column("correo_nuevo")]
        public string CorreoNuevo { get; set; } = string.Empty;

        /// <summary>SHA-256 (hexadecimal) del token que viaja en el enlace.</summary>
        [Required]
        [StringLength(64)]
        [Column("token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        [Column("fecha_solicitud")]
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Column("fecha_expiracion")]
        public DateTime FechaExpiracion { get; set; }

        /// <summary>Verdadero si el enlace ya se usó, se canceló o fue reemplazado por uno nuevo.</summary>
        [Column("usado")]
        public bool Usado { get; set; }

        /// <summary>Solo tiene valor si el cambio realmente se confirmó.</summary>
        [Column("fecha_confirmacion")]
        public DateTime? FechaConfirmacion { get; set; }
    }
}
