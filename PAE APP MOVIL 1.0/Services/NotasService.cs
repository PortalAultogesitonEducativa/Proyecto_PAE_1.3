using PAE_APP_MOVIL_1._0.Models;

namespace PAE_APP_MOVIL_1._0.Services
{
    public class NotasService
    {
        private readonly ApiService _api;

        public NotasService(ApiService api)
        {
            _api = api;
        }

        private class PromedioResponse
        {
            public decimal? Promedio { get; set; }
        }

        public async Task<List<Calificacion>> ObtenerNotasAsync(int idUsuarioEstudiante)
        {
            return await _api.GetAsync<List<Calificacion>>($"api/movil/notas/{idUsuarioEstudiante}")
                   ?? new List<Calificacion>();
        }

        public async Task<decimal?> ObtenerPromedioAsync(int idUsuarioEstudiante)
        {
            var res = await _api.GetAsync<PromedioResponse>($"api/movil/notas/{idUsuarioEstudiante}/promedio");
            return res?.Promedio;
        }
    }
}
