using PAE_APP_MOVIL_1._0.Data;
using PAE_APP_MOVIL_1._0.Models;

namespace PAE_APP_MOVIL_1._0.Services
{
    public class AuthService
    {
        private readonly ApiService _api;

        public AuthService(ApiService api)
        {
            _api = api;
        }

        private record LoginPayload(string UsuarioOCorreo, string Password);
        private record RecuperarPayload(string UsuarioOCorreo);
        private record CambiarPasswordPayload(int IdUsuario, string NuevaPassword, string Token);

        private class RecuperacionApiResponse
        {
            public string Codigo { get; set; } = string.Empty;
            public string Mensaje { get; set; } = string.Empty;
            public string? Token { get; set; }
            public int? IdUsuario { get; set; }
            public string? Correo { get; set; }
            public string? Nombre { get; set; }
        }

        public async Task<LoginResultado> IniciarSesionAsync(string usuarioOCorreo, string password)
        {
            var resultado = await _api.PostAsync<LoginPayload, LoginResultado>(
                "api/movil/auth/login",
                new LoginPayload(usuarioOCorreo, password));

            return resultado ?? new LoginResultado
            {
                Codigo = 2,
                Mensaje = "No se pudo validar el usuario."
            };
        }

        public async Task<Usuario?> ObtenerUsuarioAsync(int idUsuario)
        {
            return await _api.GetAsync<Usuario>($"api/movil/auth/usuario/{idUsuario}");
        }

        public async Task<(RecuperacionResultado resultado, int? idUsuario, string? correo, string? nombre)> SolicitarRecuperacionAsync(string usuarioOCorreo)
        {
            var respuesta = await _api.PostAsync<RecuperarPayload, RecuperacionApiResponse>(
                "api/movil/auth/recuperar",
                new RecuperarPayload(usuarioOCorreo));

            if (respuesta == null)
            {
                return (new RecuperacionResultado
                {
                    Codigo = "Error",
                    Mensaje = "No se pudo generar el token."
                }, null, null, null);
            }

            return (new RecuperacionResultado
            {
                Codigo = respuesta.Codigo,
                Mensaje = respuesta.Mensaje,
                Token = respuesta.Token
            }, respuesta.IdUsuario, respuesta.Correo, respuesta.Nombre);
        }

        public async Task<CambioPasswordResultado> CambiarPasswordAsync(int idUsuario, string nuevaPassword, string token)
        {
            var resultado = await _api.PostAsync<CambiarPasswordPayload, CambioPasswordResultado>(
                "api/movil/auth/cambiar-password",
                new CambiarPasswordPayload(idUsuario, nuevaPassword, token));

            return resultado ?? new CambioPasswordResultado
            {
                Codigo = "Error",
                Mensaje = "No se pudo cambiar la contraseña."
            };
        }
    }
}
