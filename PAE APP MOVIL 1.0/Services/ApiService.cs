using System.Net.Http.Json;
using System.Text.Json;

namespace PAE_APP_MOVIL_1._0.Services
{
    // Cliente HTTP centralizado para comunicarse con la API del portal PAE (https://pae-portal.lat) a través de Cloudflare.
    public class ApiService
    {
        private readonly ServidorConfigService _config;
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService(ServidorConfigService config)
        {
            _config = config;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<T?> GetAsync<T>(string rutaRelativa)
        {
            string url = _config.ConstruirUrl(rutaRelativa);
            using var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string rutaRelativa, TRequest payload)
        {
            string url = _config.ConstruirUrl(rutaRelativa);
            using var response = await _http.PostAsJsonAsync(url, payload, JsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }

        public async Task<(bool Ok, string? MensajeError)> ProbarConexionAsync(string urlBase, int timeoutSegundos = 8)
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSegundos)
            };

            string url = $"{ServidorConfigService.NormalizarUrl(urlBase)}/api/movil/ping";
            using var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
                return (true, null);

            string detalle = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(detalle)
                ? $"El servidor respondió con código {(int)response.StatusCode}."
                : detalle);
        }
    }
}
