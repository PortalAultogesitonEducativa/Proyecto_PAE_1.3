using Microsoft.Extensions.DependencyInjection;
using PAE_APP_MOVIL_1._0.Models;
using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class AcudienteDashboardPage : ContentPage
    {
        private readonly AcudienteService _acudienteService;
        private readonly NotasService _notasService;
        private readonly AsistenciaService _asistenciaService;
        private readonly SessionService _session;
        private readonly INavigationService _navigation;
        private Usuario? _usuario;
        private List<HijoInfo> _hijos = new();

        public AcudienteDashboardPage(
            AcudienteService acudienteService,
            NotasService notasService,
            AsistenciaService asistenciaService,
            SessionService session,
            INavigationService navigation)
        {
            InitializeComponent();
            _acudienteService = acudienteService;
            _notasService = notasService;
            _asistenciaService = asistenciaService;
            _session = session;
            _navigation = navigation;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _usuario = _session.UsuarioActual;

            if (_usuario == null)
                return;

            BienvenidaLabel.Text = $"Bienvenido/a, {_usuario.NOMBRES} {_usuario.APELLIDOS}";

            _hijos = await _acudienteService.ObtenerHijosAsync(_usuario.ID_Usuario);

            if (_hijos.Count == 0)
            {
                await DisplayAlertAsync("Sin estudiantes vinculados",
                    "No se encontraron estudiantes asociados a tu cuenta.", "Aceptar");
                return;
            }

            HijoPicker.ItemsSource = _hijos.Select(h => h.NombreCompleto).ToList();
            HijoPicker.SelectedIndex = 0;
        }

        private async void OnHijoSeleccionado(object sender, EventArgs e)
        {
            await CargarPromedioAsync();
            await CargarFallasAsync();
        }

        private async Task CargarPromedioAsync()
        {
            if (HijoPicker.SelectedIndex < 0 || _hijos.Count == 0)
                return;

            var hijo = _hijos[HijoPicker.SelectedIndex];
            var promedio = await _notasService.ObtenerPromedioAsync(hijo.IdUsuario);

            PromedioLabel.Text = promedio.HasValue
                ? $"Promedio actual: {promedio.Value}"
                : "Sin notas registradas";
        }

        private async Task CargarFallasAsync()
        {
            if (HijoPicker.SelectedIndex < 0 || _hijos.Count == 0)
                return;

            var hijo = _hijos[HijoPicker.SelectedIndex];
            var fallas = await _asistenciaService.ContarFallasAsync(hijo.IdUsuario);

            FallasLabel.Text = $"Fallas registradas: {fallas}";
        }

        private async void OnVerCalificacionesClicked(object sender, EventArgs e)
        {
            if (HijoPicker.SelectedIndex < 0 || _hijos.Count == 0)
                return;

            var hijo = _hijos[HijoPicker.SelectedIndex];
            _session.EstudianteActivo = new EstudianteContexto(hijo.IdUsuario, hijo.NombreCompleto);

            await _navigation.PushAsync<NotasPage>();
        }

        private async void OnAsistenciaClicked(object sender, EventArgs e)
        {
            if (HijoPicker.SelectedIndex < 0 || _hijos.Count == 0)
                return;

            var hijo = _hijos[HijoPicker.SelectedIndex];
            var asistenciaPage = Application.Current.Handler.MauiContext.Services.GetRequiredService<AsistenciaPage>();
            asistenciaPage.ConfigurarEstudiante(hijo.IdUsuario, hijo.NombreCompleto);
            await Navigation.PushAsync(asistenciaPage);
        }

        private async void OnCitacionesClicked(object sender, EventArgs e)
        {
            if (HijoPicker.SelectedIndex < 0 || _hijos.Count == 0)
                return;

            var hijo = _hijos[HijoPicker.SelectedIndex];
            var citacionesPage = Application.Current.Handler.MauiContext.Services.GetRequiredService<CitacionesPage>();
            citacionesPage.ConfigurarEstudiante(hijo.IdUsuario, hijo.NombreCompleto);
            await Navigation.PushAsync(citacionesPage);
        }

        private async void OnCerrarSesionClicked(object sender, TappedEventArgs e)
        {
            await ConfirmarCerrarSesionAsync();
        }

        private async Task ConfirmarCerrarSesionAsync()
        {
            bool confirmar = await DisplayAlertAsync("Cerrar sesión", "¿Seguro que quieres salir?", "Sí, salir", "Cancelar");

            if (!confirmar)
                return;

            _session.CerrarSesion();
            _navigation.SetRoot<LoginPage>();
        }

        protected override bool OnBackButtonPressed()
        {
            _ = ConfirmarCerrarSesionAsync();
            return true;
        }
    }
}
