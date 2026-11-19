using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("PROFESOR_MATERIA")]
    public class ProfesorMateria
    {
        public int id_profesor { get; set; }
        public int id_materia { get; set; }
    }
}
