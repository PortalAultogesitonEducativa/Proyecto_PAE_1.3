using Microsoft.Extensions.DependencyInjection;
using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class StudentDashboardPage : ContentPage
    {
        private readonly SessionService _session;
        private readonly INavigationService _navigation;

        public StudentDashboardPage(SessionService session, INavigationService navigation)
        {
            InitializeComponent();
            _session = session;
            _navigation = navigation;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_session.UsuarioActual is { } usuario)
                BienvenidaLabel.Text = $"Bienvenido/a, {usuario.NOMBRES} {usuario.APELLIDOS}";
        }

        private async void OnVerNotasClicked(object sender, EventArgs e)
        {
            if (_session.UsuarioActual is not { } usuario)
                return;

            _session.EstudianteActivo = new EstudianteContexto(
                usuario.ID_Usuario,
                $"{usuario.NOMBRES} {usuario.APELLIDOS}");

            await _navigation.PushAsync<NotasPage>();
        }

        private async void OnHorarioClicked(object sender, EventArgs e)
        {
            if (_session.UsuarioActual is not { } usuario)
                return;

            var horarioPage = Application.Current.Handler.MauiContext.Services.GetRequiredService<HorarioPage>();
            horarioPage.ConfigurarEstudiante(usuario.ID_Usuario);
            await Navigation.PushAsync(horarioPage);
        }

        private async void OnActividadesClicked(object sender, EventArgs e)
        {
            if (_session.UsuarioActual is not { } usuario)
                return;

            var actividadesPage = Application.Current.Handler.MauiContext.Services.GetRequiredService<ActividadesPage>();
            actividadesPage.ConfigurarEstudiante(usuario.ID_Usuario);
            await Navigation.PushAsync(actividadesPage);
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
