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
                if (rol == "coordinador") return RedirectToAction("Dashboard", "Home");
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

            // 2b. Regla Req 2: Estudiante con matrícula Cancelada o Finalizada queda inactivo y "vetado"
            if (!string.IsNullOrEmpty(user.ROL) && user.ROL.Trim().Equals("estudiante", StringComparison.OrdinalIgnoreCase))
            {
                var est = _context.ESTUDIANTE.FirstOrDefault(e => e.id_usuario == user.ID_Usuario);
                if (est != null)
                {
                    var matriculas = _context.Matriculas
                        .Where(m => m.id_estudiante == est.id_estudiante)
                        .OrderByDescending(m => m.fecha_matricula)
                        .ToList();

                    bool matriculaVetada = matriculas.Any() &&
                        (matriculas.First().estado == "Cancelada" || matriculas.First().estado == "Finalizada");

                    if (matriculaVetada)
                    {
                        if (user.ACTIVO)
                        {
                            user.ACTIVO = false;
                            _context.SaveChanges();
                        }
                        ViewBag.EstudianteVetado = true;
                        ViewBag.MensajeBloqueo = "Acceso denegado: Su matrícula académica se encuentra en estado Cancelada o Finalizada. Su usuario ha sido inhabilitado (vetado) en el sistema. Comuníquese con la administración institucional.";
                        return View("Index");
                    }
                }
            }

            // 3. Cuenta inactiva permanente (por administrador o por 3er ciclo de fallos definitivo)
            if (!user.ACTIVO)
            {
                // Revisar si fue por bloqueo definitivo de 3 ciclos
                var ultimoBloqueoDefinitivo = _context.IntentosLogin
                    .Where(i => i.id_usuario == user.ID_Usuario && i.bloqueado_hasta != null && i.bloqueado_hasta > DateTime.Now.AddYears(1))
                    .OrderByDescending(i => i.fecha_intento)
                    .FirstOrDefault();

                if (ultimoBloqueoDefinitivo != null)
                {
                    ViewBag.CuentaBloqueadaDefinitiva = true;
                    ViewBag.MensajeBloqueo = "Su cuenta ha sido bloqueada definitivamente por haber superado el tercer ciclo de intentos fallidos. Debe restablecer su contraseña de manera obligatoria para reactivar el acceso.";
                    return View("Index");
                }

                ViewBag.CuentaInactiva = true;
                ViewBag.MensajeBloqueo = "Su cuenta de usuario se encuentra inactiva o deshabilitada. Comuníquese con la coordinación o el administrador del sistema.";
                return View("Index");
            }

            // 4. Regla Req 7: Verificar si tiene un bloqueo temporal activo (15 minutos)
            var ultimoBloqueo = _context.IntentosLogin
                .Where(i => i.id_usuario == user.ID_Usuario && i.bloqueado_hasta != null)
                .OrderByDescending(i => i.fecha_intento)
                .FirstOrDefault();

            if (ultimoBloqueo != null && ultimoBloqueo.bloqueado_hasta.HasValue && ultimoBloqueo.bloqueado_hasta.Value > DateTime.Now)
            {
                int minutosRestantes = Math.Max(1, (int)Math.Ceiling((ultimoBloqueo.bloqueado_hasta.Value - DateTime.Now).TotalMinutes));
                ViewBag.BloqueoTemporal = true;
                ViewBag.MinutosRestantes = minutosRestantes;
                ViewBag.MensajeBloqueo = $"Su cuenta se encuentra bloqueada temporalmente por intentos fallidos. Podrá volver a intentar iniciar sesión en {minutosRestantes} minuto(s).";
                return View("Index");
            }

            // 5. Verificar contraseña
            bool claveValida = false;
            try
            {
                claveValida = BCrypt.Net.BCrypt.Verify(password, user.CONTRASEÑA);
            }
            catch
            {
                claveValida = (user.CONTRASEÑA == password);
            }

            // Calcular ciclos de bloqueo previos desde el último login exitoso
            var ultimoExito = _context.IntentosLogin
                .Where(i => i.id_usuario == user.ID_Usuario && i.exitoso)
                .OrderByDescending(i => i.fecha_intento)
                .Select(i => (DateTime?)i.fecha_intento)
                .FirstOrDefault();

            var queryFallidos = _context.IntentosLogin
                .Where(i => i.id_usuario == user.ID_Usuario && !i.exitoso);

            if (ultimoExito.HasValue)
            {
                queryFallidos = queryFallidos.Where(i => i.fecha_intento > ultimoExito.Value);
            }

            int ciclosBloqueoPrevios = queryFallidos.Count(i => i.bloqueado_hasta != null);

            // Intentos fallidos en el ciclo actual (desde el último bloqueo)
            var fechaUltimoBloqueo = queryFallidos
                .Where(i => i.bloqueado_hasta != null)
                .OrderByDescending(i => i.fecha_intento)
                .Select(i => (DateTime?)i.fecha_intento)
                .FirstOrDefault();

            var queryCicloActual = queryFallidos;
            if (fechaUltimoBloqueo.HasValue)
            {
                queryCicloActual = queryCicloActual.Where(i => i.fecha_intento > fechaUltimoBloqueo.Value);
            }
            int fallosCicloActual = queryCicloActual.Count();

            if (!claveValida)
            {
                fallosCicloActual++; // Sumar el intento actual

                if (fallosCicloActual < 3)
                {
                    // Aún le quedan intentos en el ciclo actual
                    _context.IntentosLogin.Add(new GU_IntentosLogin
                    {
                        id_usuario = user.ID_Usuario,
                        fecha_intento = DateTime.Now,
                        exitoso = false,
                        ip_origen = ip,
                        bloqueado_hasta = null
                    });
                    _context.SaveChanges();

                    ViewBag.ErrorPassword = $"Contraseña incorrecta. Le quedan {3 - fallosCicloActual} intento(s) antes del bloqueo.";
                    return View("Index");
                }
                else
                {
                    // Ha completado 3 fallos en este ciclo!
                    if (ciclosBloqueoPrevios == 0)
                    {
                        // 1er Ciclo: Bloqueo de 15 minutos
                        var hasta = DateTime.Now.AddMinutes(15);
                        _context.IntentosLogin.Add(new GU_IntentosLogin
                        {
                            id_usuario = user.ID_Usuario,
                            fecha_intento = DateTime.Now,
                            exitoso = false,
                            ip_origen = ip,
                            bloqueado_hasta = hasta
                        });
                        _context.SaveChanges();

                        ViewBag.BloqueoTemporal = true;
                        ViewBag.MinutosRestantes = 15;
                        ViewBag.MensajeBloqueo = "Ha superado los 3 intentos permitidos. Su cuenta ha sido bloqueada temporalmente por 15 minutos. Al cumplirse este tiempo podrá reintentar con 3 nuevos intentos.";
                        return View("Index");
                    }
                    else if (ciclosBloqueoPrevios == 1)
                    {
                        // 2do Ciclo: Bloqueo de otros 15 minutos
                        var hasta = DateTime.Now.AddMinutes(15);
                        _context.IntentosLogin.Add(new GU_IntentosLogin
                        {
                            id_usuario = user.ID_Usuario,
                            fecha_intento = DateTime.Now,
                            exitoso = false,
                            ip_origen = ip,
                            bloqueado_hasta = hasta
                        });
                        _context.SaveChanges();

                        ViewBag.BloqueoTemporal = true;
                        ViewBag.MinutosRestantes = 15;
                        ViewBag.MensajeBloqueo = "Ha superado los 3 intentos por segunda vez. Su cuenta ha sido bloqueada temporalmente por 15 minutos adicionales. ADVERTENCIA: Si agota los siguientes 3 intentos, su cuenta será bloqueada de forma definitiva y requerirá recuperar contraseña obligatoriamente.";
                        return View("Index");
                    }
                    else
                    {
                        // 3er Ciclo: Bloqueo definitivo obligatorio con recuperación de contraseña
                        user.ACTIVO = false;
                        var hasta = DateTime.Now.AddYears(100);
                        _context.IntentosLogin.Add(new GU_IntentosLogin
                        {
                            id_usuario = user.ID_Usuario,
                            fecha_intento = DateTime.Now,
                            exitoso = false,
                            ip_origen = ip,
                            bloqueado_hasta = hasta
                        });

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

                        // Enviar correo con enlace
                        string enlace = Url.Action("RestablecerContraseña", "Login", new { token }, Request.Scheme);
                        _emailService.EnviarCorreoRecuperacion(user.CORREO_ELECTRONICO, user.NOMBRES, enlace);

                        ViewBag.CuentaBloqueadaDefinitiva = true;
                        ViewBag.MensajeBloqueo = "Ha fallado por tercera vez los 3 intentos permitidos. Por seguridad, su cuenta ha sido bloqueada de manera definitiva. Es OBLIGATORIO restablecer su contraseña para recuperar el acceso. Hemos enviado un enlace de recuperación a su correo electrónico.";
                        return View("Index");
                    }
                }
            }

            // 6. Login exitoso → registrar intento y crear sesión
            _context.IntentosLogin.Add(new GU_IntentosLogin
            {
                id_usuario = user.ID_Usuario,
                fecha_intento = DateTime.Now,
                exitoso = true,
                ip_origen = ip,
                bloqueado_hasta = null
            });
            _context.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.ID_Usuario);
            HttpContext.Session.SetString("UserIdStr", user.ID_Usuario.ToString());
            HttpContext.Session.SetString("NombreUsuario", user.NOMBRES ?? user.NOMBRE_USUARIO);
            HttpContext.Session.SetString("UserRol", user.ROL.ToLower().Trim());

            if (user.ROL.ToLower().Trim() == "estudiante")
            {
                var estudiantePerfil = _context.ESTUDIANTE
                    .FirstOrDefault(e => e.id_usuario == user.ID_Usuario);

                if (estudiantePerfil != null)
                {
                    HttpContext.Session.SetInt32("IdEstudiante", estudiantePerfil.id_estudiante);
                }
                else
                {
                    HttpContext.Session.SetInt32("IdEstudiante", 1);
                }
            }

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