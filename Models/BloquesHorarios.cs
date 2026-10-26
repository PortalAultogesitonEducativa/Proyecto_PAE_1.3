using System;
using System.Linq;

namespace ProyectoPAE.Models
{
    /// <summary>
    /// Una franja de la parrilla horaria (clase o descanso).
    /// </summary>
    public record BloqueHorario(TimeSpan Inicio, TimeSpan Fin, string Etiqueta, bool EsReceso, string Nombre);

    /// <summary>
    /// ÚNICA fuente de verdad de las franjas horarias del colegio.
    /// La usan: el formulario de asignación, la validación al guardar y la vista "Mi Horario".
    /// Si el colegio cambia las horas, se cambia SOLO aquí.
    ///
    /// Jornada Mañana: 6:00 am a 12:00 m, descanso de 9:30 a 10:00.
    /// Jornada Tarde : 12:00 m a 6:00 pm, descanso de 4:00 a 4:30 pm.
    /// </summary>
    public static class BloquesHorarios
    {
        private static TimeSpan T(int h, int m = 0) => new TimeSpan(h, m, 0);

        public static readonly BloqueHorario[] Manana =
        {
            new(T(6),      T(7),      "06:00 - 07:00", false, "Bloque 1"),
            new(T(7),      T(8),      "07:00 - 08:00", false, "Bloque 2"),
            new(T(8),      T(9, 30),  "08:00 - 09:30", false, "Bloque 3"),
            new(T(9, 30),  T(10),     "09:30 - 10:00", true,  "Descanso"),
            new(T(10),     T(11),     "10:00 - 11:00", false, "Bloque 4"),
            new(T(11),     T(12),     "11:00 - 12:00", false, "Bloque 5"),
        };

        public static readonly BloqueHorario[] Tarde =
        {
            new(T(12),     T(13),     "12:00 - 13:00", false, "Bloque 1"),
            new(T(13),     T(14),     "13:00 - 14:00", false, "Bloque 2"),
            new(T(14),     T(15),     "14:00 - 15:00", false, "Bloque 3"),
            new(T(15),     T(16),     "15:00 - 16:00", false, "Bloque 4"),
            new(T(16),     T(16, 30), "16:00 - 16:30", true,  "Descanso"),
            new(T(16, 30), T(18),     "16:30 - 18:00", false, "Bloque 5"),
        };

        // ---------------------------------------------------------------------------------
        // CLASES DOBLES (2 horas seguidas). Son las parejas de bloques consecutivos que NO
        // cruzan el descanso. El bloque de 90 min (Mañana B3 / Tarde B5) queda siempre sencillo.
        //   Mañana: 06:00-08:00 (B1+B2) y 10:00-12:00 (B4+B5)
        //   Tarde : 12:00-14:00 (B1+B2) y 14:00-16:00 (B3+B4)
        // Si el colegio quiere otras parejas, se cambian SOLO aquí.
        // ---------------------------------------------------------------------------------
        public static readonly BloqueHorario[] MananaDobles =
        {
            new(T(6),  T(8),  "06:00 - 08:00", false, "Bloques 1 y 2"),
            new(T(10), T(12), "10:00 - 12:00", false, "Bloques 4 y 5"),
        };

        public static readonly BloqueHorario[] TardeDobles =
        {
            new(T(12), T(14), "12:00 - 14:00", false, "Bloques 1 y 2"),
            new(T(14), T(16), "14:00 - 16:00", false, "Bloques 3 y 4"),
        };

        public static BloqueHorario[] PorJornada(string? jornada) =>
            EsTarde(jornada) ? Tarde : Manana;

        public static BloqueHorario[] DoblesPorJornada(string? jornada) =>
            EsTarde(jornada) ? TardeDobles : MananaDobles;

        /// <summary>
        /// Devuelve la franja (sencilla o doble) que coincide exactamente con inicio-fin, o null si no es válida.
        /// </summary>
        public static BloqueHorario? BuscarFranjaValida(string? jornada, TimeSpan inicio, TimeSpan fin) =>
            PorJornada(jornada).Concat(DoblesPorJornada(jornada))
                .FirstOrDefault(b => !b.EsReceso && b.Inicio == inicio && b.Fin == fin);

        private static bool EsTarde(string? jornada) =>
            string.Equals(jornada?.Trim(), "Tarde", StringComparison.OrdinalIgnoreCase);
    }
}
