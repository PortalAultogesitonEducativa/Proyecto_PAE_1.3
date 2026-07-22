using System.ComponentModel.DataAnnotations;

namespace ProyectoPAE.Models
{
    public class Materia
    {
        [Key]
        public int id_materia { get; set; }
        public string nombre_materia { get; set; }
        public string descripcion { get; set; }
        public int creditos { get; set; }
    }
}