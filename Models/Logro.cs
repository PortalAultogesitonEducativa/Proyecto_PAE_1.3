using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("LOGRO")]
    public class Logro
    {
        [Key]
        public int id_logro { get; set; }

        /// <summary>
        /// Grado escolar (6, 7, 8, 9, 10, 11). Aplica por igual para jornada Mañana y Tarde.
        /// </summary>
        public int grado { get; set; }

        /// <summary>
        /// Nombre base de la asignatura (ej: "Matemáticas", "Biología", "Inglés").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string materia { get; set; } = string.Empty;

        /// <summary>
        /// Período académico (1, 2, 3 o 4).
        /// </summary>
        public int periodo { get; set; } = 1;

        /// <summary>
        /// Código identificador del indicador (ej: "LOG-6-MAT-P1").
        /// </summary>
        [MaxLength(30)]
        public string? codigo_logro { get; set; }

        /// <summary>
        /// Descripción pedagógica del logro esperado.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string descripcion { get; set; } = string.Empty;

        public bool activo { get; set; } = true;
    }
}