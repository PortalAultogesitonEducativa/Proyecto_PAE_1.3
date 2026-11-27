using PAE_APP_MOVIL_1._0.Models;

namespace PAE_APP_MOVIL_1._0.Services
{
    public class CitacionesService
    {
        private readonly ApiService _api;

        public CitacionesService(ApiService api)
        {
            _api = api;
        }

        public async Task<List<Citacion>> ObtenerCitacionesAsync(int idUsuarioEstudiante)
        {
            return await _api.GetAsync<List<Citacion>>($"api/movil/citaciones/{idUsuarioEstudiante}")
                   ?? new List<Citacion>();
        }
    }
}
