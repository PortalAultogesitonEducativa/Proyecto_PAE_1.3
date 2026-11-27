using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class HorarioPage : ContentPage
    {
        private readonly HorarioService _horarioService;
        private int _idUsuarioEstudiante;

        public HorarioPage(HorarioService horarioService)
        {
            InitializeComponent();
            _horarioService = horarioService;
        }

        public void ConfigurarEstudiante(int idUsuarioEstudiante)
        {
            _idUsuarioEstudiante = idUsuarioEstudiante;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHorarioAsync();
        }

        private async Task CargarHorarioAsync()
        {
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;

            try
            {
                var horario = await _horarioService.ObtenerHorarioAsync(_idUsuarioEstudiante);
                HorarioCollection.ItemsSource = horario;
                SinHorarioLabel.IsVisible = horario.Count == 0;
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