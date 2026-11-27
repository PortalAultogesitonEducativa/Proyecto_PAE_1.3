namespace PAE_APP_MOVIL_1._0.Services
{
    public interface INavigationService
    {
        Task PushAsync<TPage>() where TPage : Page;
        Task PopAsync();
        Task PopToRootAsync();
        void SetRoot<TPage>() where TPage : Page;
    }
}
