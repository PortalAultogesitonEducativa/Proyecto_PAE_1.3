using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("GU_RECUPERACION_PASSWORD")]
    public class GU_RECUPERACION_PASSWORD
    {
        [Key]
        public int id_recuperacion { get; set; }

        public int id_usuario { get; set; }

        public string token { get; set; }

        public DateTime fecha_expiracion { get; set; }

        public DateTime fecha_solicitud { get; set; }

        public DateTime? fecha_uso { get; set; }

        public int? id_solicitante { get; set; }

        public bool usado { get; set; }

        [ForeignKey("id_usuario")]
        public virtual Usuario Usuario { get; set; }
    }

    [Table("GU_HISTORIAL_PASSWORD")]
    public class GU_HISTORIAL_PASSWORD
    {
        [Key]
        public int id_historial { get; set; }

        public int id_usuario { get; set; }

        public string contrasena_hash { get; set; }

        public DateTime fecha_cambio { get; set; }

        [ForeignKey("id_usuario")]
        public virtual Usuario Usuario { get; set; }
    }
    [Table("GU_INTENTOS_LOGIN")]
    public class GU_IntentosLogin
    {
        [Key]
        public int id_intento { get; set; }

        public int id_usuario { get; set; }

        public DateTime fecha_intento { get; set; }

        public bool exitoso { get; set; }

        public string? ip_origen { get; set; }

        public DateTime? bloqueado_hasta { get; set; }

        [ForeignKey("id_usuario")]
        public virtual Usuario Usuario { get; set; }
    }
}


