using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class ActividadesPage : ContentPage
    {
        private readonly ActividadesService _actividadesService;
        private int _idUsuarioEstudiante;

        public ActividadesPage(ActividadesService actividadesService)
        {
            InitializeComponent();
            _actividadesService = actividadesService;
        }

        public void ConfigurarEstudiante(int idUsuarioEstudiante)
        {
            _idUsuarioEstudiante = idUsuarioEstudiante;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarActividadesAsync();
        }

        private async Task CargarActividadesAsync()
        {
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;

            try
            {
                var actividades = await _actividadesService.ObtenerActividadesAsync(_idUsuarioEstudiante);
                ActividadesCollection.ItemsSource = actividades;
                SinActividadesLabel.IsVisible = actividades.Count == 0;
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