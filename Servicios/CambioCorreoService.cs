using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Services;

namespace ProyectoPAE.Servicios
{
    public class ResultadoCambioCorreo
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cambio de correo con confirmación (RF-03.2): el correo nuevo no se aplica al pedirlo, sino cuando el usuario
    /// abre el enlace que se envía a ese correo nuevo.
    /// </summary>
    public class CambioCorreoService
    {
        /// <summary>Horas que dura el enlace de confirmación.</summary>
        public const int HorasVigencia = 24;

        /// <summary>Máximo de solicitudes por usuario en una hora, para evitar el envío masivo de correos.</summary>
        public const int MaxSolicitudesPorHora = 3;

        private readonly ApplicationDbContext _context;
        private readonly ServicioEmail _email;

        public CambioCorreoService(ApplicationDbContext context, ServicioEmail email)
        {
            _context = context;
            _email = email;
        }

        // ──────────────────────────────────────────────────────────────
        // SOLICITAR
        // ──────────────────────────────────────────────────────────────

        /// <param name="construirEnlace">Recibe el token y devuelve la dirección completa que va en el correo.</param>
        public async Task<ResultadoCambioCorreo> SolicitarAsync(
            int idUsuario, string? correoNuevo, string? passwordActual, Func<string, string> construirEnlace)
        {
            _context.AsegurarEsquemaCambioCorreo();

            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null || !usuario.ACTIVO)
                return Error("No fue posible procesar la solicitud. Inicia sesión nuevamente.");

            // 1. Se pide la contraseña actual: así nadie con una sesión abierta puede desviar el correo de la cuenta
            if (string.IsNullOrEmpty(passwordActual) || !VerificarPassword(passwordActual, usuario.CONTRASEÑA))
                return Error("La contraseña actual no es correcta.");

            // 2. El correo nuevo debe ser válido y distinto al actual
            string nuevo = (correoNuevo ?? string.Empty).Trim().ToLowerInvariant();
            if (!EsCorreoValido(nuevo))
                return Error("Escribe un correo electrónico válido.");

            string actual = (usuario.CORREO_ELECTRONICO ?? string.Empty).Trim().ToLowerInvariant();
            if (nuevo == actual)
                return Error("Ese ya es tu correo actual.");

            // 3. No debe pertenecer a otra cuenta. El mensaje es genérico a propósito: no se confirma si el correo existe.
            bool enUso = await _context.Usuarios.AnyAsync(u =>
                u.ID_Usuario != idUsuario &&
                u.CORREO_ELECTRONICO != null &&
                u.CORREO_ELECTRONICO.ToLower() == nuevo);
            if (enUso)
                return Error("Ese correo no está disponible. Usa uno distinto.");

            // 4. Límite de solicitudes por hora
            var ahora = DateTime.Now;
            int recientes = await _context.CambiosCorreo.CountAsync(c =>
                c.IdUsuario == idUsuario && c.FechaSolicitud >= ahora.AddHours(-1));
            if (recientes >= MaxSolicitudesPorHora)
                return Error("Hiciste demasiadas solicitudes en poco tiempo. Intenta de nuevo dentro de una hora.");

            // 5. Cualquier solicitud pendiente anterior queda anulada
            var pendientes = await _context.CambiosCorreo
                .Where(c => c.IdUsuario == idUsuario && !c.Usado)
                .ToListAsync();
            pendientes.ForEach(c => c.Usado = true);

            // 6. Token aleatorio: al usuario le llega el token y en la base solo queda su hash
            string token = NuevoToken();
            _context.CambiosCorreo.Add(new CambioCorreo
            {
                IdUsuario = idUsuario,
                CorreoAnterior = usuario.CORREO_ELECTRONICO ?? string.Empty,
                CorreoNuevo = nuevo,
                TokenHash = HashToken(token),
                FechaSolicitud = ahora,
                FechaExpiracion = ahora.AddHours(HorasVigencia),
                Usado = false
            });

            // 7. Se envía al correo NUEVO; si el envío falla no se guarda nada
            try
            {
                string nombre = $"{usuario.NOMBRES} {usuario.APELLIDOS}".Trim();
                if (string.IsNullOrWhiteSpace(nombre)) nombre = usuario.NOMBRE_USUARIO;

                _email.EnviarConfirmacionCorreo(nuevo, nombre, construirEnlace(token), HorasVigencia);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error CambioCorreoService/EnviarConfirmacion]: {ex.Message}");
                _context.ChangeTracker.Clear();
                return Error("No pudimos enviar el correo de confirmación. Verifica la dirección e inténtalo más tarde.");
            }

            await _context.SaveChangesAsync();

            return new ResultadoCambioCorreo
            {
                Exito = true,
                Mensaje = "Te enviamos un enlace de confirmación al nuevo correo. Tu correo actual no cambiará hasta que lo abras."
            };
        }

        // ──────────────────────────────────────────────────────────────
        // CANCELAR
        // ──────────────────────────────────────────────────────────────

        public async Task CancelarPendientesAsync(int idUsuario)
        {
            _context.AsegurarEsquemaCambioCorreo();

            var pendientes = await _context.CambiosCorreo
                .Where(c => c.IdUsuario == idUsuario && !c.Usado)
                .ToListAsync();

            if (pendientes.Count == 0)
                return;

            pendientes.ForEach(c => c.Usado = true);
            await _context.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────────────────────
        // CONFIRMAR
        // ──────────────────────────────────────────────────────────────

        public async Task<ResultadoCambioCorreo> ConfirmarAsync(string? token)
        {
            _context.AsegurarEsquemaCambioCorreo();

            if (string.IsNullOrWhiteSpace(token))
                return Error("El enlace no es válido.");

            string hash = HashToken(token.Trim());
            var cambio = await _context.CambiosCorreo.FirstOrDefaultAsync(c => c.TokenHash == hash);

            if (cambio == null)
                return Error("El enlace no es válido.");

            if (cambio.Usado)
                return Error("Este enlace ya fue utilizado o fue reemplazado por uno más reciente. Si el cambio ya se aplicó, inicia sesión con tu nuevo correo.");

            var ahora = DateTime.Now;
            if (cambio.FechaExpiracion < ahora)
                return Error("El enlace expiró. Solicita el cambio de correo nuevamente desde tu perfil.");

            var usuario = await _context.Usuarios.FindAsync(cambio.IdUsuario);
            if (usuario == null || !usuario.ACTIVO)
                return Error("No fue posible aplicar el cambio en esta cuenta.");

            // El correo pudo haber sido tomado por otra cuenta mientras se esperaba la confirmación
            bool enUso = await _context.Usuarios.AnyAsync(u =>
                u.ID_Usuario != usuario.ID_Usuario &&
                u.CORREO_ELECTRONICO != null &&
                u.CORREO_ELECTRONICO.ToLower() == cambio.CorreoNuevo);
            if (enUso)
            {
                cambio.Usado = true;
                await _context.SaveChangesAsync();
                return Error("Ese correo ya no está disponible. Solicita el cambio con otra dirección.");
            }

            string correoAnterior = usuario.CORREO_ELECTRONICO ?? string.Empty;

            // Se aplica el cambio y se cierra la solicitud (y cualquier otra pendiente de la cuenta)
            usuario.CORREO_ELECTRONICO = cambio.CorreoNuevo;
            cambio.Usado = true;
            cambio.FechaConfirmacion = ahora;

            var otras = await _context.CambiosCorreo
                .Where(c => c.IdUsuario == usuario.ID_Usuario && !c.Usado && c.IdCambio != cambio.IdCambio)
                .ToListAsync();
            otras.ForEach(c => c.Usado = true);

            _context.Notificaciones.Add(new Notificacion
            {
                Titulo = "Correo actualizado",
                Mensaje = "Tu correo electrónico fue actualizado correctamente. Desde ahora inicia sesión con tu nuevo correo.",
                RolDestino = $"user_{usuario.ID_Usuario}",
                FechaCreacion = ahora,
                ID_UsuarioEmisor = usuario.ID_Usuario,
                Activa = true,
                Prioridad = "leve"
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // El correo tiene restricción de unicidad en la base de datos: protege contra solicitudes simultáneas
                System.Diagnostics.Debug.WriteLine($"[Error CambioCorreoService/Confirmar]: {ex.Message}");
                _context.ChangeTracker.Clear();
                return Error("No fue posible aplicar el cambio porque ese correo ya no está disponible.");
            }

            await SincronizarCorreoEnTablasRelacionadasAsync(usuario.ID_Usuario, correoAnterior, cambio.CorreoNuevo);

            // Aviso de seguridad al correo anterior (si falla, el cambio ya está aplicado y no se revierte)
            if (!string.IsNullOrWhiteSpace(correoAnterior))
            {
                try
                {
                    string nombre = $"{usuario.NOMBRES} {usuario.APELLIDOS}".Trim();
                    _email.EnviarAvisoCambioCorreo(correoAnterior, string.IsNullOrWhiteSpace(nombre) ? usuario.NOMBRE_USUARIO : nombre, cambio.CorreoNuevo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Error CambioCorreoService/AvisoCorreoAnterior]: {ex.Message}");
                }
            }

            return new ResultadoCambioCorreo
            {
                Exito = true,
                Mensaje = "Tu correo electrónico fue actualizado. Desde ahora inicia sesión con tu nuevo correo."
            };
        }

        /// <summary>
        /// Algunas tablas heredadas (ESTUDIANTE, PROFESOR, PADRE_TUTOR) guardan una copia del correo.
        /// Se mantienen iguales para no romper los enlaces por correo. Si algo falla aquí, el cambio principal ya quedó aplicado.
        /// </summary>
        private async Task SincronizarCorreoEnTablasRelacionadasAsync(int idUsuario, string correoAnterior, string correoNuevo)
        {
            try
            {
                string anterior = correoAnterior.Trim().ToLowerInvariant();

                var estudiantes = await _context.ESTUDIANTE
                    .Where(e => e.id_usuario == idUsuario || (e.email != null && e.email.ToLower() == anterior))
                    .ToListAsync();
                estudiantes.ForEach(e => e.email = correoNuevo);

                var profesores = await _context.PROFESOR
                    .Where(p => p.id_usuario == idUsuario || (p.email != null && p.email.ToLower() == anterior))
                    .ToListAsync();
                profesores.ForEach(p => p.email = correoNuevo);

                var acudientes = await _context.PADRE_TUTOR
                    .Where(p => p.id_usuario == idUsuario || (p.email != null && p.email.ToLower() == anterior))
                    .ToListAsync();
                acudientes.ForEach(p => p.email = correoNuevo);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error CambioCorreoService/SincronizarCorreo]: {ex.Message}");
                _context.ChangeTracker.Clear();
            }
        }

        // ──────────────────────────────────────────────────────────────
        // APOYO
        // ──────────────────────────────────────────────────────────────

        /// <summary>Token de 256 bits generado con el generador criptográfico del sistema, seguro para usar en una URL.</summary>
        public static string NuevoToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }

        private static bool EsCorreoValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo) || correo.Length > 150)
                return false;

            try
            {
                var direccion = new MailAddress(correo);
                return direccion.Address == correo && direccion.Host.Contains('.');
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Misma regla que usa el resto del proyecto: hash BCrypt y, si no lo es, comparación directa
        private static bool VerificarPassword(string ingresada, string almacenada)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(ingresada, almacenada);
            }
            catch
            {
                return almacenada == ingresada;
            }
        }

        private static ResultadoCambioCorreo Error(string mensaje)
        {
            return new ResultadoCambioCorreo { Exito = false, Mensaje = mensaje };
        }
    }
}
