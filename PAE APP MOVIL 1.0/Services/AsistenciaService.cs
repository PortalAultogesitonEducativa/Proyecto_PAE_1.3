using PAE_APP_MOVIL_1._0.Models;

namespace PAE_APP_MOVIL_1._0.Services
{
    public class AsistenciaService
    {
        private readonly ApiService _api;

        public AsistenciaService(ApiService api)
        {
            _api = api;
        }

        private class FallasResponse
        {
            public int Fallas { get; set; }
        }

        public async Task<List<Asistencia>> ObtenerAsistenciaAsync(int idUsuarioEstudiante)
        {
            return await _api.GetAsync<List<Asistencia>>($"api/movil/asistencia/{idUsuarioEstudiante}")
                   ?? new List<Asistencia>();
        }

        public async Task<int> ContarFallasAsync(int idUsuarioEstudiante)
        {
            var res = await _api.GetAsync<FallasResponse>($"api/movil/asistencia/{idUsuarioEstudiante}/fallas");
            return res?.Fallas ?? 0;
        }
    }
}
