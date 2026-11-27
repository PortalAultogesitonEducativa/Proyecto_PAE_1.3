using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class CitacionesPage : ContentPage
    {
        private readonly CitacionesService _citacionesService;
        private int _idEstudiante;

        public CitacionesPage(CitacionesService citacionesService)
        {
            InitializeComponent();
            _citacionesService = citacionesService;
        }

        public void ConfigurarEstudiante(int idUsuarioEstudiante, string nombreEstudiante)
        {
            _idEstudiante = idUsuarioEstudiante;
            TituloLabel.Text = $"Citaciones de {nombreEstudiante}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarCitacionesAsync();
        }

        private async Task CargarCitacionesAsync()
        {
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;

            try
            {
                var citaciones = await _citacionesService.ObtenerCitacionesAsync(_idEstudiante);
                CitacionesCollection.ItemsSource = citaciones;
                SinCitacionesLabel.IsVisible = citaciones.Count == 0;
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