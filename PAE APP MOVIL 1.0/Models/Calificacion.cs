using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace PAE_APP_MOVIL_1._0.Models
{
    public class Calificacion
    {
        [Key]
        public int ID_Calificacion { get; set; }
        public int ID_Estudiante { get; set; }
        public string Materia { get; set; }
        public decimal Nota { get; set; }
        public int Periodo { get; set; }
        public DateTime FechaRegistro { get; set; }

        [ForeignKey("ID_Estudiante")]
        public virtual Usuario Estudiante { get; set; }
    }
}