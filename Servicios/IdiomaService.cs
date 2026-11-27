using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace ProyectoPAE.Servicios
{
    /// <summary>
    /// Servicio centralizado de internacionalización (i18n) para Proyecto PAE.
    /// Soporta Español ("es" - por defecto) e Inglés ("en").
    /// </summary>
    public static class IdiomaService
    {
        public const string IdiomaPorDefecto = "es";

        private static readonly HashSet<string> IdiomasSoportados = new() { "es", "en" };

        private static readonly Dictionary<string, Dictionary<string, string>> Diccionario = new()
        {
            ["es"] = new Dictionary<string, string>
            {
                ["AppTitulo"] = "Portal de Autogestión Educativa",
                ["NavInicio"] = "Inicio",
                ["NavNosotros"] = "Nosotros",
                ["NavPoliticas"] = "Políticas de Datos",
                ["NavBlog"] = "Blog",
                ["NavContacto"] = "Contacto",
                ["NavIniciarSesion"] = "Iniciar Sesión",
                ["NavAyuda"] = "Ayuda",
                ["NavPqr"] = "PQR",
                ["NavVerPerfil"] = "Ver Mi Perfil",
                ["NavMisPqr"] = "Mis Solicitudes PQR",
                ["NavCerrarSesion"] = "Cerrar Sesión",
                ["IdiomaNombre"] = "Español",
                ["IdiomaCorto"] = "ES",
                ["CambiarIdioma"] = "Cambiar idioma"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["AppTitulo"] = "Educational Self-Management Portal",
                ["NavInicio"] = "Home",
                ["NavNosotros"] = "About Us",
                ["NavPoliticas"] = "Data Policies",
                ["NavBlog"] = "Blog",
                ["NavContacto"] = "Contact",
                ["NavIniciarSesion"] = "Sign In",
                ["NavAyuda"] = "Help",
                ["NavPqr"] = "Requests & Claims",
                ["NavVerPerfil"] = "View My Profile",
                ["NavMisPqr"] = "My PQR Requests",
                ["NavCerrarSesion"] = "Log Out",
                ["IdiomaNombre"] = "English",
                ["IdiomaCorto"] = "EN",
                ["CambiarIdioma"] = "Change language"
            }
        };

        /// <summary>
        /// Normaliza el código de idioma recibido ("es" o "en"). Si no es válido, devuelve "es".
        /// </summary>
        public static string NormalizarIdioma(string? cultura)
        {
            if (string.IsNullOrWhiteSpace(cultura))
                return IdiomaPorDefecto;

            string codigo = cultura.Trim().ToLowerInvariant();
            if (codigo.StartsWith("en")) return "en";
            if (codigo.StartsWith("es")) return "es";

            return IdiomasSoportados.Contains(codigo) ? codigo : IdiomaPorDefecto;
        }

        /// <summary>
        /// Obtiene el código de idioma activo ("es" o "en") desde la cookie de cultura o la cultura actual del hilo.
        /// </summary>
        public static string ObtenerIdiomaActual(HttpContext? httpContext)
        {
            if (httpContext != null)
            {
                string? cookieValor = httpContext.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
                if (!string.IsNullOrWhiteSpace(cookieValor))
                {
                    var parsed = CookieRequestCultureProvider.ParseCookieValue(cookieValor);
                    string? culturaCookie = parsed?.UICultures.Count > 0
                        ? parsed.UICultures[0].Value
                        : null;

                    if (!string.IsNullOrWhiteSpace(culturaCookie))
                        return NormalizarIdioma(culturaCookie);
                }

                string? sesionIdioma = httpContext.Session.GetString("IdiomaActual");
                if (!string.IsNullOrWhiteSpace(sesionIdioma))
                    return NormalizarIdioma(sesionIdioma);
            }

            return NormalizarIdioma(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        }

        /// <summary>
        /// Traduce una clave registrada según el idioma actual de la petición HTTP.
        /// </summary>
        public static string T(string clave, HttpContext? httpContext)
        {
            string idioma = ObtenerIdiomaActual(httpContext);
            if (Diccionario.TryGetValue(idioma, out var textos) && textos.TryGetValue(clave, out var valor))
            {
                return valor;
            }

            if (Diccionario["es"].TryGetValue(clave, out var fallback))
            {
                return fallback;
            }

            return clave;
        }
    }
}