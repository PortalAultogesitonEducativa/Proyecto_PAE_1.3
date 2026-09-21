<<<<<<< Updated upstream
﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 
namespace ProyectoPAE.Models
{
    [Table("MATRICULA")]
    public class Matricula
    {
        [Key]
        public int id_matricula { get; set; }
        public int id_estudiante { get; set; }
        public int id_curso { get; set; }
        public DateTime fecha_matricula { get; set; }
        public string periodo_academico { get; set; } = "";
        public string estado { get; set; } = "";
 
        [Column("anio")]
        public int ano { get; set; }
    }
=======
﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    // Mapea la clase a la tabla fisica MATRICULA de SQL Server
    [Table("MATRICULA")]
    public class Matricula
    {
        [Key]
        [Column("id_matricula")]
        public int id_matricula { get; set; }

        [Column("id_estudiante")]
        public int id_estudiante { get; set; }

        [Column("id_curso")]
        public int id_curso { get; set; }

        [Column("fecha_matricula")]
        public DateTime fecha_matricula { get; set; } = DateTime.Now;

        [Column("periodo_academico")]
        public string? periodo_academico { get; set; }

        [Column("estado")]
        public string? estado { get; set; }

        // Columna real en SQL Server: anio
        [Column("anio")]
        public int? anio { get; set; }

        // Propiedad de apoyo en C# para evitar errores de redacción en otras partes del código
        [NotMapped]
        public int? ano
        {
            get => anio;
            set => anio = value;
        }
    }
>>>>>>> Stashed changes
}