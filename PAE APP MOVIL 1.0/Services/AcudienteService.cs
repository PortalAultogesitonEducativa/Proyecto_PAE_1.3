namespace PAE_APP_MOVIL_1._0.Services
{
    public class HijoInfo
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }

    public class AcudienteService
    {
        private readonly ApiService _api;

        public AcudienteService(ApiService api)
        {
            _api = api;
        }

        public async Task<List<HijoInfo>> ObtenerHijosAsync(int idUsuarioAcudiente)
        {
            return await _api.GetAsync<List<HijoInfo>>($"api/movil/acudiente/{idUsuarioAcudiente}/hijos")
                   ?? new List<HijoInfo>();
        }
    }
}
