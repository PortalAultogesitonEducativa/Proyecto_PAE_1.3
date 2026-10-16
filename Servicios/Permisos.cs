using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProyectoPAE.Servicios
{
    public static class Permisos
    {
        // Constantes de permisos
        public const string AdministrarUsuarios = "AdministrarUsuarios";
        public const string GestionarMatriculas = "GestionarMatriculas";
        public const string EliminarMatriculas = "EliminarMatriculas";
        public const string VerReportes = "VerReportes";
        public const string EnviarNotificaciones = "EnviarNotificaciones";
        public const string AsignarAcademica = "AsignarAcademica";
        public const string SupervisarDocentes = "SupervisarDocentes";
        public const string GestionarClases = "GestionarClases";
        public const string ConsultarSeguimiento = "ConsultarSeguimiento";
        public const string ParticiparComoEstudiante = "ParticiparComoEstudiante";
        public const string UsarPanelEstudiante = "UsarPanelEstudiante";
        public const string GenerarCertificados = "GenerarCertificados";
        public const string GenerarCertificadoMatricula = "GenerarCertificadoMatricula";
        public const string VerCertificados = "VerCertificados";

        /// <summary>
        /// Normaliza el nombre del rol a minúsculas y alias conocidos (ej. 'administrador' -> 'admin', 'profesor' -> 'docente').
        /// </summary>
        public static string NormalizarRol(string? rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
                return string.Empty;

            var r = rol.Trim().ToLowerInvariant();
            return r switch
            {
                "administrador" => "admin",
                "profesor" => "docente",
                _ => r
            };
        }

        /// <summary>
        /// Obtiene el rol normalizado del usuario en la sesión actual.
        /// </summary>
        public static string RolActual(HttpContext? httpContext)
        {
            if (httpContext == null)
                return string.Empty;

            var rol = httpContext.Session.GetString("UserRol");
            return NormalizarRol(rol);
        }

        /// <summary>
        /// Determina si un rol cuenta con un permiso específico.
        /// </summary>
        public static bool TienePermiso(string? rol, string permiso)
        {
            var rolNorm = NormalizarRol(rol);
            if (string.IsNullOrEmpty(rolNorm))
                return false;

            // Administrador tiene acceso a todos los permisos
            if (rolNorm == "admin")
                return true;

            return rolNorm switch
            {
                "coordinador" => permiso switch
                {
                    SupervisarDocentes => true,
                    AsignarAcademica => true,
                    ConsultarSeguimiento => true,
                    GestionarMatriculas => true,
                    VerReportes => true,
                    EnviarNotificaciones => true,
                    GenerarCertificados => true,
                    GenerarCertificadoMatricula => true,
                    VerCertificados => true,
                    UsarPanelEstudiante => true,
                    _ => false
                },
                "docente" => permiso switch
                {
                    GestionarClases => true,
                    ConsultarSeguimiento => true,
                    VerCertificados => true,
                    UsarPanelEstudiante => true,
                    _ => false
                },
                "estudiante" => permiso switch
                {
                    ParticiparComoEstudiante => true,
                    UsarPanelEstudiante => true,
                    VerCertificados => true,
                    _ => false
                },
                "acudiente" => permiso switch
                {
                    VerCertificados => true,
                    _ => false
                },
                _ => false
            };
        }

        /// <summary>
        /// Determina si el usuario en sesión cuenta con el permiso solicitado.
        /// </summary>
        public static bool TienePermiso(HttpContext? httpContext, string permiso)
        {
            return TienePermiso(RolActual(httpContext), permiso);
        }
    }

    /// <summary>
    /// Filtro de autorización para validar si el rol del usuario posee el permiso requerido.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequierePermisoAttribute : Attribute, IAuthorizationFilter
    {
        public string Permiso { get; }

        public RequierePermisoAttribute(string permiso)
        {
            Permiso = permiso;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            var rol = Permisos.RolActual(httpContext);

            if (string.IsNullOrEmpty(rol))
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            if (!Permisos.TienePermiso(rol, Permiso))
            {
                var headers = httpContext.Request.Headers;
                bool esAjax = headers["X-Requested-With"] == "XMLHttpRequest" ||
                              (headers.ContainsKey("Accept") && headers["Accept"].ToString().Contains("application/json"));

                if (esAjax)
                {
                    context.Result = new JsonResult(new { success = false, message = "No tienes permiso para realizar esta acción." })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                }
                else
                {
                    context.Result = new RedirectToActionResult("Dashboard", "Home", null);
                }
            }
        }
    }
}
