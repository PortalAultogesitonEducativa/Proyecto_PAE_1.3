using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class AsistenciaPage : ContentPage
    {
        private readonly AsistenciaService _asistenciaService;
        private int _idUsuarioEstudiante;

        public AsistenciaPage(AsistenciaService asistenciaService)
        {
            InitializeComponent();
            _asistenciaService = asistenciaService;
        }

        public void ConfigurarEstudiante(int idUsuarioEstudiante, string nombreEstudiante)
        {
            _idUsuarioEstudiante = idUsuarioEstudiante;
            TituloLabel.Text = $"Asistencia de {nombreEstudiante}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarAsistenciaAsync();
        }

        private async Task CargarAsistenciaAsync()
        {
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;

            try
            {
                var registros = await _asistenciaService.ObtenerAsistenciaAsync(_idUsuarioEstudiante);
                var fallas = await _asistenciaService.ContarFallasAsync(_idUsuarioEstudiante);

                FallasLabel.Text = $"Fallas registradas: {fallas}";

                AsistenciaCollection.ItemsSource = registros;
                SinRegistrosLabel.IsVisible = registros.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
            finally
            {
                CargandoIndicator.IsVisible = false;
                CargandoIndicator.IsRunning = false;
            }
        }
    }
}