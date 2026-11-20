using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("HORARIO")]
    public class Horario
    {
        [Key]
        public int id_horario { get; set; }

        public int id_curso { get; set; }

        public int id_aula { get; set; }

        public int id_profesor { get; set; }

        // Clave foránea para la materia
        public int id_materia { get; set; }

        public string dia { get; set; } = string.Empty;

        public TimeSpan hora_inicio { get; set; }

        public TimeSpan hora_fin { get; set; }
        public int anio_escolar { get; set; }
        public int? periodo_academico { get; set; }

        // Indica si el horario ya fue confirmado/publicado por el administrador (false = Borrador, true = Publicado)
        [Column("publicado")]
        public bool publicado { get; set; } = false;

        [NotMapped]
        public string HoraInicioFormateada => hora_inicio.ToString(@"hh\:mm");

        [NotMapped]
        public string HoraFinFormateada => hora_fin.ToString(@"hh\:mm");

        // --- PROPIEDADES DE NAVEGACIÓN ---
        [ForeignKey("id_materia")]
        public virtual Materia? Materia { get; set; }

        [ForeignKey("id_curso")]
        public virtual Curso? Curso { get; set; }

        [ForeignKey("id_profesor")]
        public virtual Profesor? Profesor { get; set; }

        [ForeignKey("id_aula")]
        public virtual Aula? Aula { get; set; }
    }
}