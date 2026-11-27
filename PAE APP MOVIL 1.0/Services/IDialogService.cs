namespace PAE_APP_MOVIL_1._0.Services
{
    public interface IDialogService
    {
        Task AlertAsync(string titulo, string mensaje, string aceptar = "Aceptar");
        Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar, string cancelar);
    }
}
