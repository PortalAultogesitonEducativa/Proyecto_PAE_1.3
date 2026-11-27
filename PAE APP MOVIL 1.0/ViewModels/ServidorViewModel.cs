using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0.ViewModels
{
    public partial class ServidorViewModel : ObservableObject
    {
        private readonly ServidorConfigService _config;
        private readonly ApiService _api;
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        public ServidorViewModel(
            ServidorConfigService config,
            ApiService api,
            INavigationService navigation,
            IDialogService dialogs)
        {
            _config = config;
            _api = api;
            _navigation = navigation;
            _dialogs = dialogs;

            UrlServidor = _config.UrlBase;
        }

        [ObservableProperty]
        private string _urlServidor = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NoOcupado))]
        private bool _isBusy;

        public bool NoOcupado => !IsBusy;

        private string? Validar(out string urlNormalizada)
        {
            string entrada = (UrlServidor ?? string.Empty).Trim();
            urlNormalizada = string.Empty;

            if (string.IsNullOrWhiteSpace(entrada))
                return "Escribe el dominio o URL del portal PAE (ejemplo: https://pae-portal.lat).";

            string candidata = ServidorConfigService.NormalizarUrl(entrada);

            if (!Uri.TryCreate(candidata, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                string.IsNullOrWhiteSpace(uri.Host))
            {
                return "La URL no es válida. Ejemplo: https://pae-portal.lat";
            }

            urlNormalizada = candidata;
            return null;
        }

        [RelayCommand]
        private async Task ProbarConexionAsync()
        {
            string? error = Validar(out string url);

            if (error != null)
            {
                await _dialogs.AlertAsync("Datos incompletos", error);
                return;
            }

            IsBusy = true;

            try
            {
                var (ok, mensajeError) = await _api.ProbarConexionAsync(url, 8);

                if (ok)
                {
                    await _dialogs.AlertAsync("Conexión exitosa",
                        $"La app se conectó correctamente al portal y la base de datos en {url}. Ya puedes guardar esta configuración.");
                }
                else
                {
                    await _dialogs.AlertAsync("No se pudo conectar",
                        mensajeError ?? "El servidor no respondió correctamente.");
                }
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync("No se pudo conectar", ex.GetBaseException().Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GuardarAsync()
        {
            string? error = Validar(out string url);

            if (error != null)
            {
                await _dialogs.AlertAsync("Datos incompletos", error);
                return;
            }

            _config.Guardar(url);
            UrlServidor = url;

            await _dialogs.AlertAsync("Servidor guardado",
                $"La app usará el dominio {url}. Se abrirá de nuevo el inicio de sesión.");

            _navigation.SetRoot<LoginPage>();
        }

        [RelayCommand]
        private async Task RestablecerAsync()
        {
            _config.Restablecer();
            UrlServidor = _config.UrlBase;

            await _dialogs.AlertAsync("Valores restablecidos",
                $"Se volvió a la configuración original: {UrlServidor}. Se abrirá de nuevo el inicio de sesión.");

            _navigation.SetRoot<LoginPage>();
        }
    }
}
