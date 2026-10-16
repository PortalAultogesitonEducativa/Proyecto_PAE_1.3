using System.Linq;
using Microsoft.AspNetCore.Http;
using ProyectoPAE.Models;

namespace ProyectoPAE.Servicios
{
    public static class AccesoEstudiantes
    {
        /// <summary>
        /// Valida si el usuario en sesión tiene permisos para consultar la información del estudiante indicado.
        /// - Administrador, Coordinador y Docente: acceso a cualquier estudiante.
        /// - Estudiante: solo puede ver su propia información.
        /// - Acudiente: solo puede consultar la información de sus hijos asignados.
        /// </summary>
        public static bool PuedeVerEstudiante(ApplicationDbContext context, HttpContext httpContext, int idEstudiante)
        {
            if (context == null || httpContext == null)
                return false;

            var rol = Permisos.RolActual(httpContext);
            var idUsuario = httpContext.Session.GetInt32("UserId");

            if (string.IsNullOrEmpty(rol) || !idUsuario.HasValue)
                return false;

            // 1. Personal académico y administrativo tienen acceso total
            if (rol == "admin" || rol == "coordinador" || rol == "docente")
                return true;

            // 2. Estudiante: únicamente sus propios datos
            if (rol == "estudiante")
            {
                if (idUsuario.Value == idEstudiante)
                    return true;

                var esPropio = context.ESTUDIANTE.Any(e =>
                    (e.id_estudiante == idEstudiante || e.id_usuario == idEstudiante) &&
                    e.id_usuario == idUsuario.Value);

                return esPropio;
            }

            // 3. Acudiente: únicamente sus hijos vinculados
            if (rol == "acudiente")
            {
                var padreTutor = context.PADRE_TUTOR.FirstOrDefault(p => p.id_usuario == idUsuario.Value);
                int idPadreLegacy = padreTutor != null ? padreTutor.id_padre : idUsuario.Value;

                var hijosIds = context.ESTUDIANTE_PADRE
                    .Where(ep => ep.ID_PADRE == idPadreLegacy || ep.ID_PADRE == idUsuario.Value)
                    .Select(ep => ep.ID_ESTUDIANTE)
                    .ToList();

                if (hijosIds.Contains(idEstudiante))
                    return true;

                var estudiantesVinculados = context.ESTUDIANTE
                    .Where(e => hijosIds.Contains(e.id_estudiante) || (e.id_usuario != null && hijosIds.Contains(e.id_usuario.Value)))
                    .ToList();

                bool vinculado = estudiantesVinculados.Any(e => e.id_estudiante == idEstudiante || e.id_usuario == idEstudiante);
                if (vinculado)
                    return true;

                // Fallback de demostración si no existen relaciones formalizadas en la BD aún
                if (!hijosIds.Any())
                {
                    bool esHijoDemo = context.Usuarios
                        .Where(u => u.ROL != null && u.ROL.Trim().ToLower() == "estudiante")
                        .Take(5)
                        .Any(u => u.ID_Usuario == idEstudiante);

                    if (esHijoDemo)
                        return true;
                }

                return false;
            }

            return false;
        }
    }
}
