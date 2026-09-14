using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("GU_NOTIFICACION_LEIDA")]
    public class NotificacionLeida
    {
        [Key]
        [Column("ID_NotificacionLeida")]
        public int ID_NotificacionLeida { get; set; }

        [Column("ID_Notificacion")]
        public int ID_Notificacion { get; set; }

        [Column("ID_Usuario")]
        public int ID_Usuario { get; set; }

        [Column("FechaLeido")]
        public DateTime FechaLeido { get; set; } = DateTime.Now;

        [ForeignKey("ID_Notificacion")]
        public virtual Notificacion Notificacion { get; set; }

        [ForeignKey("ID_Usuario")]
        public virtual Usuario Usuario { get; set; }
    }
}
