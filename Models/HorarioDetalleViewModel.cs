using System;

namespace ProyectoPAE.Models
{
    public class HorarioDetalleViewModel
    {
        public string DiaSemana { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string NombreMateria { get; set; } = string.Empty;
        public string NombreProfesor { get; set; } = string.Empty;
        public string NombreSalon { get; set; } = string.Empty;
        public bool EsDescanso { get; set; } = false;
    }


}
