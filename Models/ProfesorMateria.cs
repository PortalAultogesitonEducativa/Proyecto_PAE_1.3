using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("PROFESOR_MATERIA")]
    public class ProfesorMateria
    {
        [Key]
        public int id_profesor_materia { get; set; }
        public int id_profesor { get; set; }
        public int id_materia { get; set; }
    }
}

