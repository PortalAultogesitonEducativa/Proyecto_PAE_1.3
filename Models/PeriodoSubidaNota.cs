using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("PERIODOS_SUBIDA_NOTAS")]
    public class PeriodoSubidaNota
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("periodo")]
        public int Periodo { get; set; } // 1, 2, 3, 4

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Now;

        [Column("fecha_cierre")]
        public DateTime FechaCierre { get; set; } = DateTime.Now.AddDays(30);

        [Column("activo")]
        public bool Activo { get; set; } = true; // Si está habilitado para subir notas

        [Column("porcentaje")]
        public int Porcentaje { get; set; } = 25;

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [NotMapped]
        public bool Vigente => Activo && DateTime.Now >= FechaInicio && DateTime.Now <= FechaCierre;

        [NotMapped]
        public int DiasRestantes
        {
            get
            {
                var diff = (FechaCierre.Date - DateTime.Now.Date).Days;
                return diff < 0 ? 0 : diff;
            }
        }
    }
}
