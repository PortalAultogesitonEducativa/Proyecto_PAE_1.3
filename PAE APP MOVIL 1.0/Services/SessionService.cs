using PAE_APP_MOVIL_1._0.Models;

namespace PAE_APP_MOVIL_1._0.Services
{
    public record EstudianteContexto(int IdUsuario, string NombreCompleto);

    public class SessionService
    {
        public Usuario? UsuarioActual { get; private set; }

        // Estudiante cuyos datos se están consultando: el propio estudiante o el hijo elegido por el acudiente.
        public EstudianteContexto? EstudianteActivo { get; set; }

        public void IniciarSesion(Usuario usuario)
        {
            UsuarioActual = usuario;
            EstudianteActivo = null;
        }

        public void CerrarSesion()
        {
            UsuarioActual = null;
            EstudianteActivo = null;
        }
    }
}
