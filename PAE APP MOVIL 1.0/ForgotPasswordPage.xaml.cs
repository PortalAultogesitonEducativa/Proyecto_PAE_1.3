using Microsoft.Extensions.DependencyInjection;
using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0
{
    public partial class ForgotPasswordPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly EmailService _emailService;

        public ForgotPasswordPage(AuthService authService, EmailService emailService)
        {
            InitializeComponent();
            _authService = authService;
            _emailService = emailService;
        }

        private async void OnEnviarClicked(object sender, EventArgs e)
        {
            string usuarioOCorreo = UsuarioEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(usuarioOCorreo))
            {
                await DisplayAlert("Campo vacío", "Ingresa tu usuario o correo.", "Aceptar");
                return;
            }

            EnviarBtn.IsEnabled = false;
            CargandoIndicator.IsVisible = true;
            CargandoIndicator.IsRunning = true;

            try
            {
                var (resultado, idUsuario, correo, nombre) = await _authService.SolicitarRecuperacionAsync(usuarioOCorreo);

                if (resultado.Codigo != "OK" || idUsuario == null || string.IsNullOrEmpty(resultado.Token))
                {
                    await DisplayAlert("Error", resultado.Mensaje, "Aceptar");
                    return;
                }

                bool enviado = false;

                if (!string.IsNullOrEmpty(correo))
                {
                    enviado = await _emailService.EnviarTokenRecuperacionAsync(correo, nombre ?? "usuario", resultado.Token);
                }

                if (enviado)
                {
                    await DisplayAlert("Revisa tu correo",
                        $"Enviamos las instrucciones a {correo}. Revisa tu bandeja de entrada (y la carpeta de spam).",
                        "Continuar");
                }
                else
                {
#if DEBUG
                    // SOLO PARA PRUEBAS: el correo no se pudo enviar (por ejemplo, dominio de prueba inexistente),
                    // así que mostramos el token directamente para poder seguir probando el flujo.
                    await DisplayAlert("Token generado (solo pruebas, correo no enviado)", resultado.Token, "Continuar");
#else
                    await DisplayAlert("Error",
                        "No pudimos enviar el correo. Verifica que tu correo esté registrado correctamente o contacta a soporte.",
                        "Aceptar");
                    return;
#endif
                }

                var resetPage = Application.Current.Handler.MauiContext.Services.GetRequiredService<ResetPasswordPage>();
                resetPage.ConfigurarDatos(idUsuario.Value, resultado.Token);
                await Navigation.PushAsync(resetPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error de conexión", ex.Message, "Aceptar");
            }
            finally
            {
                EnviarBtn.IsEnabled = true;
                CargandoIndicator.IsVisible = false;
                CargandoIndicator.IsRunning = false;
            }
        }
    }
}