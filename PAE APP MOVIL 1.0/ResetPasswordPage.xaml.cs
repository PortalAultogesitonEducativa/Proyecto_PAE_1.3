using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class ResetPasswordPage : ContentPage
    {
        private readonly AuthService _authService;
        private int _idUsuario;
        private string _token = string.Empty;

        public ResetPasswordPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        public void ConfigurarDatos(int idUsuario, string token)
        {
            _idUsuario = idUsuario;
            _token = token;
            TokenLabel.Text = $"Token: {token}";
        }

        private async void OnCambiarClicked(object sender, EventArgs e)
        {
            string nuevaPassword = NuevaPasswordEntry.Text ?? string.Empty;
            string confirmarPassword = ConfirmarPasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nuevaPassword))
            {
                await DisplayAlert("Campo vacío", "Ingresa la nueva contraseña.", "Aceptar");
                return;
            }

            if (nuevaPassword != confirmarPassword)
            {
                await DisplayAlert("No coinciden", "Las contraseñas no coinciden.", "Aceptar");
                return;
            }

            CambiarBtn.IsEnabled = false;
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;

            try
            {
                var resultado = await _authService.CambiarPasswordAsync(_idUsuario, nuevaPassword, _token);

                if (resultado.Codigo != "OK")
                {
                    await DisplayAlert("Error", resultado.Mensaje, "Aceptar");
                    return;
                }

                await DisplayAlert("Listo", "Contraseña actualizada. Ya puedes iniciar sesión.", "Aceptar");
                await Navigation.PopToRootAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de conexión", ex.Message, "Aceptar");
            }
            finally
            {
                CambiarBtn.IsEnabled = true;
                CargandoIndicator.IsVisible = false;
                CargandoIndicator.IsRunning = false;
            }
        }
    }
}