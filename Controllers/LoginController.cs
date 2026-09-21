using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using ProyectoPAE.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;

namespace ProyectoPAE.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ServicioEmail _emailService;

        public LoginController(ApplicationDbContext context, ServicioEmail emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ─── LOGIN ────────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            string rol = HttpContext.Session.GetString("UserRol");
            if (rol != null)
            {
                if (rol == "admin") return RedirectToAction("Usuarios", "Admin");
                if (rol == "docente") return RedirectToAction("Planilla", "Docente");
                // Dentro del método de validación de LoginController.cs
                if (rol == "estudiante")
                {
                    return RedirectToAction("Dashboard", "Home");
                }
            }
            return View();
        }

        [HttpPost]
        public IActionResult Ingresar(string correo, string password)
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // 1. Buscar usuario solo por correo (sin importar si está activo)
            var user = _context.Usuarios
                .FirstOrDefault(u => u.CORREO_ELECTRONICO == correo);

            // 2. Correo no existe
            if (user == null)
            {
                ViewBag.ErrorCorreo = "Correo equivocado, revisar credenciales.";
                return View("Index");
            }

            // 3. Cuenta inactiva (bloqueada por intentos)
            if (!user.ACTIVO)
            {
                ViewBag.CuentaBloqueada = true;
                return View("Index");
            }

            // 4. Verificar contraseña
            bool claveValida = false;
            try
            {
                claveValida = BCrypt.Net.BCrypt.Verify(password, user.CONTRASEÑA);
            }
            catch
            {
                claveValida = (user.CONTRASEÑA == password);
            }

            if (!claveValida)
            {
                // 4a. Registrar intento fallido
                _context.IntentosLogin.Add(new GU_IntentosLogin
                {
                    id_usuario = user.ID_Usuario,
                    fecha_intento = DateTime.Now,
                    exitoso = false,
                    ip_origen = ip
                });
                _context.SaveChanges();

                // 4b. Contar intentos fallidos en los últimos 30 minutos
                var ventana = DateTime.Now.AddMinutes(-30);
                int intentosFallidos = _context.IntentosLogin
                    .Count(i => i.id_usuario == user.ID_Usuario
                             && !i.exitoso
                             && i.fecha_intento >= ventana);

                // 4c. Si llegó a 3 → bloquear y generar token de recuperación
                if (intentosFallidos >= 3)
                {
                    // Inactivar usuario
                    user.ACTIVO = false;
                    _context.SaveChanges();

                    // Invalidar tokens anteriores
                    var tokensAnteriores = _context.RecuperacionesPassword
                        .Where(r => r.id_usuario == user.ID_Usuario && !r.usado)
                        .ToList();
                    tokensAnteriores.ForEach(t => t.usado = true);

                    // Generar nuevo token
                    string token = Guid.NewGuid().ToString("N");
                    _context.RecuperacionesPassword.Add(new GU_RECUPERACION_PASSWORD
                    {
                        id_usuario = user.ID_Usuario,
                        token = token,
                        fecha_solicitud = DateTime.Now,
                        fecha_expiracion = DateTime.Now.AddMinutes(60),
                        usado = false
                    });
                    _context.SaveChanges();

                    // Enviar correo con enlace de recuperación
                    string enlace = Url.Action(
                        "RestablecerContraseña", "Login",
                        new { token },
                        Request.Scheme
                    );
                    _emailService.EnviarCorreoRecuperacion(
                        user.CORREO_ELECTRONICO,
                        user.NOMBRES,
                        enlace
                    );

                    ViewBag.CuentaBloqueada = true;
                    return View("Index");
                }

                // 4d. Aún tiene intentos restantes
                ViewBag.ErrorPassword = $"Contraseña incorrecta. " +
                    $"Intentos restantes: {3 - intentosFallidos}.";
                return View("Index");
            }

            // 5. Login exitoso → registrar intento y crear sesión
            _context.IntentosLogin.Add(new GU_IntentosLogin
            {
                id_usuario = user.ID_Usuario,
                fecha_intento = DateTime.Now,
                exitoso = true,
                ip_origen = ip
            });
            _context.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.ID_Usuario);
            HttpContext.Session.SetString("UserIdStr", user.ID_Usuario.ToString());
            HttpContext.Session.SetString("NombreUsuario", !string.IsNullOrWhiteSpace(user.NOMBRES) ? user.NOMBRES : (!string.IsNullOrWhiteSpace(user.NOMBRE_USUARIO) ? user.NOMBRE_USUARIO : "Usuario"));
            HttpContext.Session.SetString("UserRol", (!string.IsNullOrWhiteSpace(user.ROL) ? user.ROL : "docente").ToLower().Trim());

            return RedirectToAction("Dashboard", "Home");
        }

        // ─── OLVIDÉ MI CONTRASEÑA ─────────────────────────────────────────────
        public IActionResult OlvideContraseña()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SolicitarRecuperacion(string correo)
        {
            ViewBag.Mensaje = "Si ese correo está registrado, recibirás un enlace en unos minutos.";

            var user = _context.Usuarios
                .FirstOrDefault(u => u.CORREO_ELECTRONICO == correo && u.ACTIVO == true);

            if (user == null)
                return View("OlvideContraseña");

            // Invalidar tokens anteriores del mismo usuario
            var tokensAnteriores = _context.RecuperacionesPassword
                .Where(r => r.id_usuario == user.ID_Usuario && !r.usado)
                .ToList();
            tokensAnteriores.ForEach(t => t.usado = true);

            // Crear nuevo token
            string token = Guid.NewGuid().ToString("N");

            var recuperacion = new GU_RECUPERACION_PASSWORD
            {
                id_usuario = user.ID_Usuario,
                token = token,
                fecha_solicitud = DateTime.Now,
                fecha_expiracion = DateTime.Now.AddMinutes(30),
                usado = false
            };

            _context.RecuperacionesPassword.Add(recuperacion);
            _context.SaveChanges();

            // Armar enlace y enviar correo
            string enlace = Url.Action(
                "RestablecerContraseña", "Login",
                new { token },
                Request.Scheme
            );

            _emailService.EnviarCorreoRecuperacion(
                user.CORREO_ELECTRONICO,
                user.NOMBRES,
                enlace
            );

            return View("OlvideContraseña");
        }

        // ─── RESTABLECER CONTRASEÑA ───────────────────────────────────────────
        public IActionResult RestablecerContraseña(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Index");

            var recuperacion = _context.RecuperacionesPassword
                .FirstOrDefault(r =>
                    r.token == token &&
                    !r.usado &&
                    r.fecha_expiracion > DateTime.Now
                );

            if (recuperacion == null)
            {
                ViewBag.Error = "El enlace no es válido o ya expiró.";
                return View("OlvideContraseña");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult GuardarNuevaContraseña(string token, string nuevaPassword, string confirmar)
        {
            if (nuevaPassword != confirmar)
            {
                ViewBag.Token = token;
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View("RestablecerContraseña");
            }

            // Validación de contraseña segura (misma regla que el cambio de contraseña desde el perfil)
            if (!ValidarContrasenaSegura(nuevaPassword, out string mensajeError))
            {
                ViewBag.Token = token;
                ViewBag.Error = mensajeError;
                return View("RestablecerContraseña");
            }

            var recuperacion = _context.RecuperacionesPassword
                .FirstOrDefault(r =>
                    r.token == token &&
                    !r.usado &&
                    r.fecha_expiracion > DateTime.Now
                );

            if (recuperacion == null)
            {
                ViewBag.Error = "El enlace no es válido o ya expiró.";
                return View("OlvideContraseña");
            }

            var user = _context.Usuarios.Find(recuperacion.id_usuario);

            // Guardar contraseña anterior en historial
            _context.HistorialPasswords.Add(new GU_HISTORIAL_PASSWORD
            {
                id_usuario = user.ID_Usuario,
                contrasena_hash = user.CONTRASEÑA,
                fecha_cambio = DateTime.Now
            });

            // Actualizar contraseña y reactivar usuario
            user.CONTRASEÑA = nuevaPassword;
            user.ACTIVO = true;

            // Marcar token como usado
            recuperacion.usado = true;
            recuperacion.fecha_uso = DateTime.Now;

            // ── Limpiar intentos fallidos para que el contador arranque en 0 ──
            var intentosAnteriores = _context.IntentosLogin
                .Where(i => i.id_usuario == user.ID_Usuario)
                .ToList();
            _context.IntentosLogin.RemoveRange(intentosAnteriores);

            // Crear notificación de sistema por cambio de contraseña
            var notifPassword = new Notificacion
            {
                Titulo = "Cambio de Contraseña",
                Mensaje = "Tu contraseña ha sido restablecida exitosamente mediante la recuperación de cuenta.",
                RolDestino = $"user_{user.ID_Usuario}",
                FechaCreacion = DateTime.Now,
                ID_UsuarioEmisor = user.ID_Usuario,
                Activa = true,
                Prioridad = "leve"
            };
            _context.Notificaciones.Add(notifPassword);

            _context.SaveChanges();

            ViewBag.Exito = "✅ Contraseña actualizada. Ya puedes iniciar sesión.";
            return View("Index");
        }

        // ─── LOGOUT ───────────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ─── VALIDACIÓN DE CONTRASEÑA SEGURA (misma regla usada en HomeController) ──
        private bool ValidarContrasenaSegura(string contrasena, out string mensaje)
        {
            if (string.IsNullOrEmpty(contrasena) || contrasena.Length < 8)
            {
                mensaje = "La contraseña debe tener al menos 8 caracteres.";
                return false;
            }
            if (!contrasena.Any(char.IsUpper))
            {
                mensaje = "La contraseña debe contener al menos una letra mayúscula.";
                return false;
            }
            if (!contrasena.Any(char.IsLower))
            {
                mensaje = "La contraseña debe contener al menos una letra minúscula.";
                return false;
            }
            if (!contrasena.Any(char.IsDigit))
            {
                mensaje = "La contraseña debe contener al menos un número.";
                return false;
            }
            if (!contrasena.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
            {
                mensaje = "La contraseña debe contener al menos un carácter especial (!@#$%^&*...).";
                return false;
            }
            mensaje = string.Empty;
            return true;
        }
    }
}