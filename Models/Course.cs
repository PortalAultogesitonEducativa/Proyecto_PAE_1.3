namespace ProyectoPAE.Models
{
    public class Course
    {
        public int id_curso { get; set; }
        public string? nombre_curso { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? IconClass { get; set; } // Clase de Bootstrap Icons
        public string? LinkText { get; set; }
    }
}
