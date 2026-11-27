using PAE_APP_MOVIL_1._0.ViewModels;

namespace PAE_APP_MOVIL_1._0
{
    public partial class NotasPage : ContentPage
    {
        private readonly NotasViewModel _viewModel;

        public NotasPage(NotasViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CargarCommand.ExecuteAsync(null);
        }
    }
}
