namespace PAE_APP_MOVIL_1._0.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _services;

        public NavigationService(IServiceProvider services)
        {
            _services = services;
        }

        private static Window VentanaActual =>
            Application.Current?.Windows.FirstOrDefault()
            ?? throw new InvalidOperationException("No hay una ventana activa para navegar.");

        private static INavigation Navegacion =>
            VentanaActual.Page?.Navigation
            ?? throw new InvalidOperationException("La ventana activa no tiene una página raíz.");

        private TPage ResolverPagina<TPage>() where TPage : Page
        {
            return (TPage?)_services.GetService(typeof(TPage))
                ?? throw new InvalidOperationException($"La página {typeof(TPage).Name} no está registrada en MauiProgram.");
        }

        public Task PushAsync<TPage>() where TPage : Page
        {
            return Navegacion.PushAsync(ResolverPagina<TPage>());
        }

        public Task PopAsync()
        {
            return Navegacion.PopAsync();
        }

        public Task PopToRootAsync()
        {
            return Navegacion.PopToRootAsync();
        }

        public void SetRoot<TPage>() where TPage : Page
        {
            VentanaActual.Page = new NavigationPage(ResolverPagina<TPage>());
        }
    }
}
