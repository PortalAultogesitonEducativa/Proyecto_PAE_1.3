namespace PAE_APP_MOVIL_1._0.Services
{
    // Guarda en el dispositivo la URL del portal PAE (por defecto https://pae-portal.lat vía Cloudflare),
    // para poder cambiarla desde la app sin recompilar el APK.
    public class ServidorConfigService
    {
        private const string ClaveUrl = "servidor_url";

        public const string UrlPredeterminada = "https://pae-portal.lat";

        public string UrlBase => NormalizarUrl(Preferences.Default.Get(ClaveUrl, UrlPredeterminada));

        public void Guardar(string url)
        {
            Preferences.Default.Set(ClaveUrl, NormalizarUrl(url));
        }

        public void Restablecer()
        {
            Preferences.Default.Remove(ClaveUrl);
        }

        public string ConstruirUrl(string rutaRelativa)
        {
            string ruta = (rutaRelativa ?? string.Empty).TrimStart('/');
            return $"{UrlBase}/{ruta}";
        }

        public static string NormalizarUrl(string? url)
        {
            string limpia = (url ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(limpia))
                return UrlPredeterminada;

            if (!limpia.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !limpia.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                limpia = "https://" + limpia;
            }

            return limpia;
        }
    }
}
