using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        public const string RolEstudiante = "estudiante";
        public const string RolAcudiente = "acudiente";

        private readonly AuthService _authService;
        private readonly SessionService _session;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        public LoginViewModel(
            AuthService authService,
            SessionService session,
            INavigationService navigation,
            IDialogService dialogs)
        {
            _authService = authService;
            _session = session;
            _navigation = navigation;
            _dialogs = dialogs;
        }

        [ObservableProperty]
        private string _usuario = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PasswordOculto))]
        [NotifyPropertyChangedFor(nameof(IconoPassword))]
        private bool _passwordVisible;

        [ObservableProperty]
        private string _rolSeleccionado = RolEstudiante;

        [ObservableProperty]
        private bool _isBusy;

        public bool PasswordOculto => !PasswordVisible;

        public string IconoPassword => PasswordVisible ? "🙈" : "👁";

        [RelayCommand]
        private void SeleccionarRol(string rol)
        {
            RolSeleccionado = rol;
        }

        [RelayCommand]
        private void AlternarPassword()
        {
            PasswordVisible = !PasswordVisible;
        }

        [RelayCommand]
        private Task OlvidoPasswordAsync()
        {
            return _navigation.PushAsync<ForgotPasswordPage>();
        }

        [RelayCommand]
        private Task ConfigurarServidorAsync()
        {
            return _navigation.PushAsync<ServidorPage>();
        }

        [RelayCommand]
        private async Task IngresarAsync()
        {
            string usuario = Usuario?.Trim() ?? string.Empty;
            string password = Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                await _dialogs.AlertAsync("Campos incompletos", "Ingresa tu usuario y contraseña.");
                return;
            }

            IsBusy = true;

            try
            {
                var resultado = await _authService.IniciarSesionAsync(usuario, password);

                if (resultado.Codigo != 0 || resultado.IdUsuario == null)
                {
                    await _dialogs.AlertAsync("Error", resultado.Mensaje);
                    return;
                }

                var usuarioLogueado = await _authService.ObtenerUsuarioAsync(resultado.IdUsuario.Value);

                if (usuarioLogueado == null)
                {
                    await _dialogs.AlertAsync("Error", "No se pudo cargar la información del usuario.");
                    return;
                }

                string rol = usuarioLogueado.ROL?.ToLower() ?? string.Empty;

                if (rol != RolEstudiante && rol != RolAcudiente)
                {
                    await _dialogs.AlertAsync("Acceso no permitido",
                        "Esta aplicación es exclusiva para estudiantes y acudientes. Usa el portal web con tu rol actual.");
                    return;
                }

                if (rol != RolSeleccionado)
                {
                    string nombreSeleccionado = RolSeleccionado == RolEstudiante ? "Estudiante" : "Acudiente";
                    string nombreReal = rol == RolEstudiante ? "Estudiante" : "Acudiente";

                    await _dialogs.AlertAsync("Rol incorrecto",
                        $"Esta cuenta corresponde a {nombreReal}, pero seleccionaste {nombreSeleccionado}. Cambia la pestaña e intenta de nuevo.");
                    return;
                }

                _session.IniciarSesion(usuarioLogueado);

                if (rol == RolEstudiante)
                    _navigation.SetRoot<StudentDashboardPage>();
                else
                    _navigation.SetRoot<AcudienteDashboardPage>();
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync("Error de conexión",
                    ex.GetBaseException().Message +
                    "\n\nVerifica tu conexión a internet o revisa \"Configurar servidor\" al final de esta pantalla.");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
