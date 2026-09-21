using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("CURSO_MATERIA")]
    public class CursoMateria
    {
        [Key]
        public int id_curso_materia { get; set; }
        public int id_curso { get; set; }
        public int id_materia { get; set; }
    }
}