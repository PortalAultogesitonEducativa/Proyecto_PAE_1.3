<<<<<<< Updated upstream
﻿namespace ProyectoPAE.Models
{
    public class Course
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconClass { get; set; } // Clase de Bootstrap Icons
        public string LinkText { get; set; }
    }
}
=======
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("CURSO")]
    public class Course
    {
        // ── Propiedades de Base de Datos ──
        [Key]
        [Column("id_curso")]
        public int id_curso { get; set; }

        [Column("nombre_curso")]
        public string nombre_curso { get; set; }

        // ── Propiedades de Interfaz de Usuario (UI) ──
        // [NotMapped] indica a Entity Framework que estas propiedades no existen en SQL
        [NotMapped]
        public string Title { get; set; }

        [NotMapped]
        public string Description { get; set; }

        [NotMapped]
        public string IconClass { get; set; } // Clase de Bootstrap Icons

        [NotMapped]
        public string LinkText { get; set; }
    }
}
>>>>>>> Stashed changes
