namespace PAE_APP_MOVIL_1._0.Services
{
    public class DialogService : IDialogService
    {
        private static Page PaginaActual =>
            Application.Current?.Windows.FirstOrDefault()?.Page
            ?? throw new InvalidOperationException("No hay una página activa para mostrar el mensaje.");

        public Task AlertAsync(string titulo, string mensaje, string aceptar = "Aceptar")
        {
            return PaginaActual.DisplayAlertAsync(titulo, mensaje, aceptar);
        }

        public Task<bool> ConfirmAsync(string titulo, string mensaje, string aceptar, string cancelar)
        {
            return PaginaActual.DisplayAlertAsync(titulo, mensaje, aceptar, cancelar);
        }
    }
}
