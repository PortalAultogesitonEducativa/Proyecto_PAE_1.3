using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("GU_NOTIFICACION")]
    public class Notificacion
    {
        [Key]
        [Column("ID_Notificacion")]
        public int ID_Notificacion { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [MaxLength(150)]
        [Column("Titulo")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        [Column("Mensaje")]
        public string Mensaje { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("RolDestino")]
        public string RolDestino { get; set; } // "todos", "estudiante", "docente", "acudiente"

        [Column("FechaCreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("ID_UsuarioEmisor")]
        public int ID_UsuarioEmisor { get; set; }

        [Column("Activa")]
        public bool Activa { get; set; } = true;

        [ForeignKey("ID_UsuarioEmisor")]
        public virtual Usuario Emisor { get; set; }
    }
}
