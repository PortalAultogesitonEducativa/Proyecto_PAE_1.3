using System.ComponentModel;
using PAE_APP_MOVIL_1._0.ViewModels;

namespace PAE_APP_MOVIL_1._0
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _viewModel;

        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void SelectorGrid_SizeChanged(object sender, EventArgs e)
        {
            if (SelectorGrid.Width > 0)
            {
                IndicadorBorder.WidthRequest = SelectorGrid.Width / 2;
            }
        }

        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.RolSeleccionado))
                await AnimarSelectorAsync();
        }

        // La animación de la pestaña es un asunto puramente visual, por eso se queda en la vista.
        private async Task AnimarSelectorAsync()
        {
            bool esEstudiante = _viewModel.RolSeleccionado == LoginViewModel.RolEstudiante;

            // Estudiante: azul PAE con texto blanco. Acudiente: naranja PAE con texto azul (más legible que blanco sobre naranja).
            EstudianteTabLabel.TextColor = esEstudiante ? Colors.White : PaeColores.Navy;
            AcudienteTabLabel.TextColor = PaeColores.Navy;
            IndicadorBorder.BackgroundColor = esEstudiante ? PaeColores.Navy : PaeColores.Naranja;

            await IndicadorBorder.TranslateToAsync(esEstudiante ? 0 : SelectorGrid.Width / 2, 0, 200, Easing.CubicInOut);
        }
    }
}
