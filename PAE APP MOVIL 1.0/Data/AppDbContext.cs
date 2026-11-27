namespace PAE_APP_MOVIL_1._0.Data
{
    public class LoginResultado
    {
        public int Codigo { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? IdUsuario { get; set; }
    }

    public class RecuperacionResultado
    {
        public string Codigo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? Token { get; set; }
    }

    public class CambioPasswordResultado
    {
        public string Codigo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
