using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ProyectoPAE.Models
{
    /// <summary>
    /// Representa la entidad de la tabla CURSO en la base de datos (Asignaturas / Grados académicos).
    /// </summary>
    [Table("CURSO")]
    public class Curso
    {
        /// <summary>
        /// Identificador único del curso.
        /// </summary>
        [Key]
        public int id_curso { get; set; }


        /// <summary>
        /// Nombre descriptivo del curso o asignatura (ej: Matemáticas, 6°A, etc.).
        /// </summary>
        public string nombre_curso { get; set; } = string.Empty;


        /// <summary>
        /// Año lectivo u escolar correspondiente.
        /// </summary>
        public int ano_escolar { get; set; }


        /// <summary>
        /// Identificador del departamento académico al que pertenece.
        /// </summary>
        public int id_departamento { get; set; }


        /// <summary>
        /// Identificador del área de conocimiento.
        /// </summary>
        public int id_area { get; set; }


        /// <summary>
        /// Identificador del periodo académico.
        /// </summary>
        public int id_periodo { get; set; }
    }
}
