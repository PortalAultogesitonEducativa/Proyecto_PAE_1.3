using PAE_APP_MOVIL_1._0.Models;

namespace PAE_APP_MOVIL_1._0.Services
{
    public class ActividadesService
    {
        private readonly ApiService _api;

        public ActividadesService(ApiService api)
        {
            _api = api;
        }

        public async Task<List<Documento>> ObtenerActividadesAsync(int idUsuarioEstudiante)
        {
            return await _api.GetAsync<List<Documento>>($"api/movil/actividades/{idUsuarioEstudiante}")
                   ?? new List<Documento>();
        }
    }
}
