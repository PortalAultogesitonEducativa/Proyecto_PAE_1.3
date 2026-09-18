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

        public string dia { get; set; } = string.Empty;

        public TimeSpan hora_inicio { get; set; }

        public TimeSpan hora_fin { get; set; }

        // Propiedades auxiliares para mostrar la hora formateada (HH:mm) en las vistas Razor
        [NotMapped]
        public string HoraInicioFormateada => hora_inicio.ToString(@"hh\:mm");

        [NotMapped]
        public string HoraFinFormateada => hora_fin.ToString(@"hh\:mm");
    }
}
