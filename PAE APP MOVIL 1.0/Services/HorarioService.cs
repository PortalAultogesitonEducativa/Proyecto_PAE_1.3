namespace PAE_APP_MOVIL_1._0.Services
{
    public class HorarioItem
    {
        public string Dia { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public string? Aula { get; set; }
        public string? Profesor { get; set; }
    }

    public class HorarioService
    {
        private readonly ApiService _api;

        public HorarioService(ApiService api)
        {
            _api = api;
        }

        public async Task<List<HorarioItem>> ObtenerHorarioAsync(int idUsuarioEstudiante)
        {
            return await _api.GetAsync<List<HorarioItem>>($"api/movil/horario/{idUsuarioEstudiante}")
                   ?? new List<HorarioItem>();
        }
    }
}
