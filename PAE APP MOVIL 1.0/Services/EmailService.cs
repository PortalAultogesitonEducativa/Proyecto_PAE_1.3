using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PAE_APP_MOVIL_1._0.Services
{
    public class EmailService
    {
        private const string Servidor = "smtp.gmail.com";
        private const int Puerto = 587;
        private const string Remitente = "paesml2025@gmail.com";
        private const string PasswordApp = "dtkxhgfdrstsofgd";
        private const string NombreRemitente = "Sistema PAE";

        public async Task<bool> EnviarTokenRecuperacionAsync(string correoDestino, string nombreUsuario, string token)
        {
            try
            {
                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress(NombreRemitente, Remitente));
                mensaje.To.Add(MailboxAddress.Parse(correoDestino));
                mensaje.Subject = "Recuperación de contraseña - Sistema PAE";

                var builder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <p>Hola {nombreUsuario},</p>
                        <p>Recibimos una solicitud para restablecer tu contraseña en el Portal de Autogestión Educativa.</p>
                        <p>Tu código de recuperación es:</p>
                        <h2>{token}</h2>
                        <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                    "
                };
                mensaje.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(Servidor, Puerto, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(Remitente, PasswordApp);
                await client.SendAsync(mensaje);
                await client.DisconnectAsync(true);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}