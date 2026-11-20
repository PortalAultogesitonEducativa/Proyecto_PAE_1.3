using System;
using System.Net;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace ProyectoPAE.Services
{
    public class ServicioEmail
    {
        private readonly IConfiguration _config;

        public ServicioEmail(IConfiguration config)
        {
            _config = config;
        }

        public void EnviarCorreoRecuperacion(string destinatario, string nombre, string enlace)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(
                _config["Email:NombreRemitente"],
                _config["Email:Remitente"]
            ));
            mensaje.To.Add(new MailboxAddress(nombre, destinatario));
            mensaje.Subject = "Recuperación de contraseña — Sistema PAE";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto'>
                    <h2 style='color:#2c3e50'>Recuperación de contraseña</h2>
                    <p>Hola <strong>{nombre}</strong>,</p>
                    <p>Recibimos una solicitud para restablecer tu contraseña.
                       Haz clic en el botón de abajo. El enlace expira en <strong>30 minutos</strong>.</p>
                    <a href='{enlace}' style='
                        display:inline-block;
                        background:#2980b9;
                        color:#fff;
                        padding:12px 24px;
                        border-radius:6px;
                        text-decoration:none;
                        margin:16px 0'>
                        Restablecer contraseña
                    </a>
                    <p style='color:#888;font-size:12px'>
                        Si no solicitaste este cambio, ignora este correo.<br/>
                        El enlace expirará automáticamente.
                    </p>
                </div>"
            };

            Enviar(mensaje);
        }

        /// <summary>Correo que se envía al correo NUEVO para que el usuario confirme el cambio.</summary>
        public void EnviarConfirmacionCorreo(string destinatario, string nombre, string enlace, int horasVigencia)
        {
            string nombreSeguro = WebUtility.HtmlEncode(nombre);

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(
                _config["Email:NombreRemitente"],
                _config["Email:Remitente"]
            ));
            mensaje.To.Add(new MailboxAddress(nombre, destinatario));
            mensaje.Subject = "Confirma tu nuevo correo — Sistema PAE";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto'>
                    <h2 style='color:#2c3e50'>Confirma tu nuevo correo</h2>
                    <p>Hola <strong>{nombreSeguro}</strong>,</p>
                    <p>Recibimos una solicitud para usar esta dirección como el correo de tu cuenta en el Sistema PAE.
                       Para confirmarla haz clic en el botón de abajo. El enlace expira en <strong>{horasVigencia} horas</strong>
                       y solo se puede usar una vez.</p>
                    <a href='{enlace}' style='
                        display:inline-block;
                        background:#2980b9;
                        color:#fff;
                        padding:12px 24px;
                        border-radius:6px;
                        text-decoration:none;
                        margin:16px 0'>
                        Confirmar nuevo correo
                    </a>
                    <p style='color:#888;font-size:12px'>
                        Hasta que confirmes, tu correo actual no cambia.<br/>
                        Si no solicitaste este cambio, ignora este mensaje.
                    </p>
                </div>"
            };

            Enviar(mensaje);
        }

        /// <summary>Aviso de seguridad al correo ANTERIOR cuando el cambio ya se aplicó.</summary>
        public void EnviarAvisoCambioCorreo(string destinatario, string nombre, string correoNuevo)
        {
            string nombreSeguro = WebUtility.HtmlEncode(nombre);
            string nuevoSeguro = WebUtility.HtmlEncode(correoNuevo);

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(
                _config["Email:NombreRemitente"],
                _config["Email:Remitente"]
            ));
            mensaje.To.Add(new MailboxAddress(nombre, destinatario));
            mensaje.Subject = "Tu correo en el Sistema PAE fue cambiado";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto'>
                    <h2 style='color:#2c3e50'>Cambio de correo</h2>
                    <p>Hola <strong>{nombreSeguro}</strong>,</p>
                    <p>El correo de tu cuenta en el Sistema PAE fue cambiado a <strong>{nuevoSeguro}</strong>.</p>
                    <p style='color:#888;font-size:12px'>
                        Si no fuiste tú, comunícate de inmediato con la coordinación del colegio para recuperar tu cuenta.
                    </p>
                </div>"
            };

            Enviar(mensaje);
        }

        private void Enviar(MimeMessage mensaje)
        {
            using var smtp = new SmtpClient();
            smtp.Connect(
                _config["Email:Servidor"],
                int.Parse(_config["Email:Puerto"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls
            );
            smtp.Authenticate(_config["Email:Remitente"], _config["Email:Password"]);
            smtp.Send(mensaje);
            smtp.Disconnect(true);
        }
    }
}
