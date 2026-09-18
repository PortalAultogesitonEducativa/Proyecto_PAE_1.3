using System;

using System;

namespace ProyectoPAE.Models
{
    /// <summary>
    /// ViewModel para estructurar y presentar los detalles de cada celda/bloque en la vista de Mi Horario.
    /// </summary>
    public class HorarioDetalleViewModel
    {
        /// <summary>
        /// Día de la semana (Lunes, Martes, etc.)
        /// </summary>
        public string DiaSemana { get; set; } = string.Empty;

        /// <summary>
        /// Hora de inicio de la clase o receso.
        /// </summary>
        public TimeSpan HoraInicio { get; set; }

        /// <summary>
        /// Hora de finalización de la clase o receso.
        /// </summary>
        public TimeSpan HoraFin { get; set; }

        /// <summary>
        /// Nombre de la materia o asignatura.
        /// </summary>
        public string NombreMateria { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del docente a cargo.
        /// </summary>
        public string NombreProfesor { get; set; } = string.Empty;

        /// <summary>
        /// Nombre o grado asignado al curso (ej. 6°A, Matemáticas).
        /// </summary>
        public string NombreCurso { get; set; } = string.Empty;

        /// <summary>
        /// Nombre o número del aula / salón de clases.
        /// </summary>
        public string NombreSalon { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la franja corresponde a un receso o descanso institucional.
        /// </summary>
        public bool EsDescanso { get; set; } = false;
    }
}


