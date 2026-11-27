using PAE_APP_MOVIL_1._0.ViewModels;

namespace PAE_APP_MOVIL_1._0
{
    public partial class ServidorPage : ContentPage
    {
        public ServidorPage(ServidorViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
