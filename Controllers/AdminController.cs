using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;
using Rotativa.AspNetCore;


namespace ProyectoPAE.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MatriculaAcademicaService _matriculaService;

        public AdminController(ApplicationDbContext context, MatriculaAcademicaService matriculaService)
        {
            _context = context;
            _matriculaService = matriculaService;
        }


        // RF 1.4: Buscar y visualizar usuarios
        [RequierePermiso(Permisos.AdministrarUsuarios)]
        public IActionResult Usuarios(string buscar)
        {


            // Traemos todos los usuarios
            var lista = _context.Usuarios.AsQueryable();


            // Si hay algo en el buscador, filtramos por nombre, apellido o rol
            if (!string.IsNullOrEmpty(buscar))
            {
                lista = lista.Where(u => u.NOMBRES.Contains(buscar) ||
                                         u.APELLIDOS.Contains(buscar) ||
                                         u.ROL.Contains(buscar));
            }


            return View(lista.ToList());
        }


        [HttpPost]
        [RequierePermiso(Permisos.AdministrarUsuarios)]
        public IActionResult RegistrarEstudiante(Usuario nuevoEstudiante, int CursoSeleccionado)
        {
            // 1. Comprobar si el correo ya existe
            var existe = _context.Usuarios.Any(u => u.CORREO_ELECTRONICO == nuevoEstudiante.CORREO_ELECTRONICO);
            if (existe)
            {
                TempData["Error"] = "Este correo ya está registrado.";
                return RedirectToAction("Usuarios");
            }


            if (ModelState.IsValid)
            {
                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    // 2. Insertar en GU_Usuario
                    nuevoEstudiante.NOMBRE_USUARIO = nuevoEstudiante.CORREO_ELECTRONICO;
                    nuevoEstudiante.CONTRASEÑA = BCrypt.Net.BCrypt.HashPassword(nuevoEstudiante.CONTRASEÑA ?? "123456");
                    nuevoEstudiante.ROL = "estudiante";
                    nuevoEstudiante.ACTIVO = true;
                    nuevoEstudiante.FECHA_CREACION = DateTime.Now;


                    _context.Usuarios.Add(nuevoEstudiante);
                    _context.SaveChanges(); // Genera nuevoEstudiante.ID_Usuario


                    // 3. Insertar en la tabla ESTUDIANTE vinculando id_usuario
                    var anioActual = DateTime.Now.Year;
                    var consecutivo = _context.ESTUDIANTE
                        .Count(e => e.codigo_estudiante.StartsWith($"EST-{anioActual}")) + 1;
                    var codigoEstudiante = $"EST-{anioActual}-{consecutivo:D3}";


                    var entidadEstudiante = new Estudiante
                    {
                        nombre = nuevoEstudiante.NOMBRES ?? "",
                        apellido = nuevoEstudiante.APELLIDOS ?? "",
                        email = nuevoEstudiante.CORREO_ELECTRONICO ?? "",
                        codigo_estudiante = codigoEstudiante,
                        fecha_inscripcion = DateTime.Now,
                        id_usuario = nuevoEstudiante.ID_Usuario // Se vincula con GU_Usuario
                    };


                    _context.ESTUDIANTE.Add(entidadEstudiante);
                    _context.SaveChanges(); // Genera entidadEstudiante.id_estudiante


                    // 4. Crear el registro en MATRICULA asociando id_estudiante
                    var hoy = DateTime.Now;
                    var semestre = hoy.Month <= 6 ? "I" : "II";


                    var nuevaMatricula = new Matricula
                    {
                        id_estudiante = entidadEstudiante.id_estudiante, // ID proveniente de ESTUDIANTE
                        id_curso = CursoSeleccionado,
                        fecha_matricula = hoy,
                        periodo_academico = $"{hoy.Year}-{semestre}",
                        estado = "Activa",
                        ano = hoy.Year
                    };


                    _context.Matriculas.Add(nuevaMatricula);
                    _context.SaveChanges();


                    transaction.Commit();


                    TempData["Mensaje"] = "Estudiante registrado y matriculado con éxito.";
                    return RedirectToAction("Usuarios");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var detalle = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    TempData["Error"] = "Error al guardar el estudiante: " + detalle;
                }
            }


            return View("Usuarios", _context.Usuarios.ToList());
        }






        [HttpPost]
        [RequierePermiso(Permisos.AdministrarUsuarios)]
        public async Task<IActionResult> RegistrarUsuario(RegistroUsuarioViewModel model)
        {
            if (model.ROL == "Estudiante" && model.CursoSeleccionado == null)
                ModelState.AddModelError("CursoSeleccionado", "Debe seleccionar un grado para el estudiante.");


            if (model.ROL == "Estudiante" && (model.Acudientes == null || !model.Acudientes.Any(a => !string.IsNullOrWhiteSpace(a.NOMBRES))))
                ModelState.AddModelError("Acudientes", "Debe registrar al menos un acudiente.");


            if (model.ROL == "Docente" && string.IsNullOrWhiteSpace(model.AREA_ASIGNATURA))
                ModelState.AddModelError("AREA_ASIGNATURA", "Debe indicar el área o asignatura del docente.");


            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revisa los campos del formulario, hay datos obligatorios sin diligenciar.";
                return RedirectToAction("Usuarios");
            }


            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevoUsuario = new Usuario
                {
                    NOMBRE_USUARIO = await GenerarNombreUsuario(model.NOMBRES, model.APELLIDOS),
                    CONTRASEÑA = BCrypt.Net.BCrypt.HashPassword(model.CONTRASEÑA),
                    NOMBRES = model.NOMBRES,
                    APELLIDOS = model.APELLIDOS,
                    CORREO_ELECTRONICO = model.CORREO_ELECTRONICO,
                    FECHA_CREACION = DateTime.Now,
                    ACTIVO = model.ESTADO_ACTIVO,
                    ROL = model.ROL,
                    TELEFONO = model.CELULAR,
                    DIRECCION = model.DIRECCION,
                    TIPO_DOCUMENTO = model.TIPO_DOCUMENTO,
                    NUM_DOCUMENTO = model.NUM_DOCUMENTO,
                    FECHA_NACIMIENTO = model.FECHA_NACIMIENTO,
                    GENERO = model.GENERO,
                    LUGAR_NACIMIENTO = model.LUGAR_NACIMIENTO,
                    CIUDAD = model.CIUDAD,
                    BARRIO = model.BARRIO,
                    AREA_ASIGNATURA = model.AREA_ASIGNATURA,
                    COLEGIO_PROCEDENCIA = model.COLEGIO_PROCEDENCIA,
                    TIPO_SANGRE = model.TIPO_SANGRE,
                    EPS = model.EPS,
                    ALERGIAS = model.ALERGIAS,
                    CONDICIONES_MEDICAS = model.CONDICIONES_MEDICAS,
                    OBSERVACIONES = model.OBSERVACIONES,
                    FORZAR_CAMBIO_CLAVE = model.FORZAR_CAMBIO_CLAVE
                };


                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();


                if (model.ROL == "Estudiante" && model.CursoSeleccionado.HasValue)
                {
                    var anioActual = DateTime.Now.Year;
                    var consecutivo = await _context.ESTUDIANTE
                        .CountAsync(e => e.codigo_estudiante.StartsWith($"EST-{anioActual}")) + 1;
                    var codigoEstudiante = $"EST-{anioActual}-{consecutivo:D3}";


                    var nuevoEstudiante = new Estudiante
                    {
                        nombre = model.NOMBRES,
                        apellido = model.APELLIDOS,
                        email = model.CORREO_ELECTRONICO,
                        codigo_estudiante = codigoEstudiante,
                        fecha_inscripcion = DateTime.Now,
                        id_usuario = nuevoUsuario.ID_Usuario
                    };
                    _context.ESTUDIANTE.Add(nuevoEstudiante);
                    await _context.SaveChangesAsync();


                    var hoy = DateTime.Now;
                    var semestre = hoy.Month <= 6 ? "I" : "II";


                    _context.Matriculas.Add(new Matricula
                    {
                        id_estudiante = nuevoEstudiante.id_estudiante,
                        id_curso = model.CursoSeleccionado.Value,
                        fecha_matricula = hoy,
                        periodo_academico = $"{hoy.Year}-{semestre}",
                        estado = "Activa",
                        ano = hoy.Year
                    });


                    if (model.Acudientes != null)
                    {
                        foreach (var acu in model.Acudientes.Where(a => !string.IsNullOrWhiteSpace(a.NOMBRES)))
                        {
                            var acudienteExistente = await BuscarAcudienteExistente(acu.DOCUMENTO, acu.CORREO);


                            var partesNombre = acu.NOMBRES!.Trim().Split(' ', 2);
                            var nombreAcudiente = partesNombre[0];
                            var apellidoAcudiente = partesNombre.Length > 1 ? partesNombre[1] : "-";


                            Usuario usuarioAcudiente;
                            if (acudienteExistente != null)
                            {
                                usuarioAcudiente = acudienteExistente;
                            }
                            else
                            {
                                usuarioAcudiente = new Usuario
                                {
                                    NOMBRE_USUARIO = await GenerarNombreUsuario(nombreAcudiente, apellidoAcudiente),
                                    CONTRASEÑA = BCrypt.Net.BCrypt.HashPassword(GenerarClaveTemporal()),
                                    NOMBRES = nombreAcudiente,
                                    APELLIDOS = apellidoAcudiente,
                                    CORREO_ELECTRONICO = acu.CORREO,
                                    NUM_DOCUMENTO = acu.DOCUMENTO,
                                    TELEFONO = acu.CELULAR,
                                    ROL = "acudiente",
                                    ACTIVO = true,
                                    FECHA_CREACION = DateTime.Now,
                                    FORZAR_CAMBIO_CLAVE = true
                                };
                                _context.Usuarios.Add(usuarioAcudiente);
                                await _context.SaveChangesAsync();
                            }


                            // Buscar o crear el registro legacy en PADRE_TUTOR, enlazado por id_usuario
                            var padreTutor = await _context.PADRE_TUTOR
                                .FirstOrDefaultAsync(p => p.id_usuario == usuarioAcudiente.ID_Usuario);


                            if (padreTutor == null)
                            {
                                padreTutor = new PadreTutor
                                {
                                    nombre = nombreAcudiente,
                                    apellido = apellidoAcudiente,
                                    telefono = acu.CELULAR,
                                    email = acu.CORREO,
                                    relacion_estudiante = acu.PARENTESCO,
                                    id_usuario = usuarioAcudiente.ID_Usuario
                                };
                                _context.PADRE_TUTOR.Add(padreTutor);
                                await _context.SaveChangesAsync();
                            }


                            _context.ESTUDIANTE_PADRE.Add(new EstudiantePadre
                            {
                                ID_PADRE = padreTutor.id_padre,
                                ID_ESTUDIANTE = nuevoEstudiante.id_estudiante,
                                relacion = acu.PARENTESCO,
                                ES_PRINCIPAL = acu.ES_PRINCIPAL
                            });
                        }
                    }
                }


                if (string.Equals(model.ROL, "Docente", StringComparison.OrdinalIgnoreCase))
                {
                    var profeExistente = await _context.PROFESOR
                        .FirstOrDefaultAsync(p => p.email == model.CORREO_ELECTRONICO);


                    if (profeExistente != null)
                    {
                        profeExistente.id_usuario = nuevoUsuario.ID_Usuario;
                        profeExistente.especialidad = model.AREA_ASIGNATURA;
                        profeExistente.telefono = model.CELULAR;
                        profeExistente.direccion = model.DIRECCION;
                    }
                    else
                    {
                        var nuevoProfesor = new Profesor
                        {
                            nombre = model.NOMBRES,
                            apellido = model.APELLIDOS,
                            email = model.CORREO_ELECTRONICO,
                            telefono = model.CELULAR,
                            direccion = model.DIRECCION,
                            especialidad = model.AREA_ASIGNATURA,
                            id_departamento = 1,
                            id_usuario = nuevoUsuario.ID_Usuario
                        };
                        _context.PROFESOR.Add(nuevoProfesor);
                    }
                    await _context.SaveChangesAsync();
                }


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


                TempData["Mensaje"] = "Usuario registrado correctamente.";
                return RedirectToAction("Usuarios");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var detalle = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = "Ocurrió un error al guardar el usuario: " + detalle;
                return RedirectToAction("Usuarios");
            }
        }


        private async Task<Usuario?> BuscarAcudienteExistente(string? documento, string? correo)
        {
            if (!string.IsNullOrWhiteSpace(documento))
            {
                var porDocumento = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.ROL == "acudiente" && u.NUM_DOCUMENTO == documento);
                if (porDocumento != null) return porDocumento;
            }


            if (!string.IsNullOrWhiteSpace(correo))
            {
                return await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.ROL == "acudiente" && u.CORREO_ELECTRONICO == correo);
            }


            return null;
        }


        private async Task<string> GenerarNombreUsuario(string nombres, string apellidos)
        {
            var inicial = nombres.Trim().Substring(0, 1).ToLower();
            var apellido = apellidos.Trim().Split(' ')[0].ToLower();
            var baseNombre = string.IsNullOrWhiteSpace(apellido)
                ? $"{inicial}.{nombres.Trim().ToLower()}"
                : $"{inicial}.{apellido}";


            var nombreUsuario = baseNombre;
            var contador = 1;


            while (await _context.Usuarios.AnyAsync(u => u.NOMBRE_USUARIO == nombreUsuario))
            {
                contador++;
                nombreUsuario = $"{baseNombre}{contador}";
            }


            return nombreUsuario;
        }


        private string GenerarClaveTemporal()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 10);
        }


        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> RenovarMatricula(int idEstudiante)
        {
            var ultimaMatricula = await _context.Matriculas
                .Where(m => m.id_estudiante == idEstudiante)
                .OrderByDescending(m => m.ano)
                .ThenByDescending(m => m.fecha_matricula)
                .FirstOrDefaultAsync();

            int anioRenovacion = DateTime.Now.Year;
            int gradoSolicitado = 6;

            if (ultimaMatricula != null)
            {
                int gradoUltima = ultimaMatricula.grado_solicitado ?? 6;
                if (ultimaMatricula.id_curso.HasValue)
                {
                    var cursoUlt = await _context.Cursos.FindAsync(ultimaMatricula.id_curso.Value);
                    if (cursoUlt != null) gradoUltima = MatriculaAcademicaService.ExtraerGrado(cursoUlt.nombre_curso, gradoUltima);
                }

                // Si aprobó, solicita el grado siguiente
                if (ultimaMatricula.estado_academico == "Aprobado")
                {
                    gradoSolicitado = Math.Min(11, gradoUltima + 1);
                }
                else
                {
                    gradoSolicitado = gradoUltima;
                }
            }

            var validacion = await _matriculaService.ValidarAvanceGradoEstudianteAsync(idEstudiante, gradoSolicitado, anioRenovacion);
            if (!validacion.Valido)
            {
                TempData["ErrorMatriculas"] = validacion.Mensaje;
                TempData["Error"] = validacion.Mensaje;
                return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
            }

            var nuevaMatricula = new Matricula
            {
                id_estudiante = idEstudiante,
                id_curso = null, // Sin curso asignado hasta completar revisión documental
                grado_solicitado = gradoSolicitado,
                fecha_matricula = DateTime.Now,
                periodo_academico = $"{anioRenovacion}-I",
                estado = "Registrada",
                estado_academico = "Pendiente de resultado",
                estado_documental = "Pendiente de revisión",
                ano = anioRenovacion,
                observaciones = $"Renovación de matrícula solicitada para Grado {gradoSolicitado}°."
            };

            _context.Matriculas.Add(nuevaMatricula);
            await _context.SaveChangesAsync();

            TempData["MensajeMatriculas"] = $"Renovación registrada para Grado {gradoSolicitado}° ({anioRenovacion}). Pendiente de revisión documental.";
            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }
        /// <summary>
        /// Ascenso / retiro de coordinador. Solo mueve a un usuario entre los roles docente y coordinador;
        /// los demás roles quedan fuera porque tienen datos que dependen de su rol (matrícula, acudientes).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso(Permisos.AdministrarUsuarios)]
        public IActionResult CambiarRol(int idUsuario, string nuevoRol)
        {
            var usuario = _context.Usuarios.Find(idUsuario);
            if (usuario == null)
            {
                TempData["Error"] = "No se encontró el usuario.";
                return RedirectToAction("Usuarios");
            }

            // 1. No se puede editar al Admin Principal (ID 1).
            if (usuario.ID_Usuario == 1)
            {
                TempData["Error"] = "El administrador principal no se puede modificar.";
                return RedirectToAction("Usuarios");
            }

            var rolActual = Permisos.NormalizarRol(usuario.ROL);
            var rolNuevo = Permisos.NormalizarRol(nuevoRol);

            // 2. Solo se permite pasar entre docente y coordinador.
            var rolesPermitidos = new[] { "docente", "coordinador" };
            if (!rolesPermitidos.Contains(rolActual) || !rolesPermitidos.Contains(rolNuevo))
            {
                TempData["Error"] = "Solo se puede cambiar el rol entre docente y coordinador.";
                return RedirectToAction("Usuarios");
            }

            if (rolActual == rolNuevo)
            {
                TempData["Mensaje"] = $"{usuario.NOMBRES} {usuario.APELLIDOS} ya tiene el rol {rolNuevo}.";
                return RedirectToAction("Usuarios");
            }

            // 3. Verificamos si tiene clases asignadas para informar al administrador
            bool tieneClases = false;
            if (rolNuevo == "coordinador")
            {
                var idsProfesor = _context.PROFESOR
                    .Where(p => p.id_usuario == usuario.ID_Usuario)
                    .Select(p => p.id_profesor)
                    .ToList();

                tieneClases = idsProfesor.Count > 0 &&
                              _context.HORARIOS.Any(h => idsProfesor.Contains(h.id_profesor));
            }

            usuario.ROL = rolNuevo;

            // 4. Guardamos los cambios del usuario en la base de datos
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar el rol en la base de datos: " + ex.Message;
                return RedirectToAction("Usuarios");
            }

            // 5. Aviso al usuario afectado (usa el mecanismo de notificaciones del sistema)
            try
            {
                _context.AsegurarEsquemaNotificaciones();
                _context.Notificaciones.Add(new Notificacion
                {
                    Titulo = "Tu rol fue actualizado",
                    Mensaje = $"Tu rol en el sistema ahora es {rolNuevo}. Cierra sesión y vuelve a ingresar para ver tu nuevo panel.",
                    RolDestino = $"user_{usuario.ID_Usuario}",
                    FechaCreacion = DateTime.Now,
                    ID_UsuarioEmisor = HttpContext.Session.GetInt32("UserId") ?? 1,
                    Activa = true,
                    Prioridad = "leve"
                });
                _context.SaveChanges();
            }
            catch { }

            if (rolNuevo == "coordinador")
            {
                TempData["Mensaje"] = tieneClases
                    ? $"{usuario.NOMBRES} {usuario.APELLIDOS} fue ascendido a Coordinador exitosamente. (Nota: Aún tiene clases en Horarios, puedes reasignarlas en Asignación Académica)."
                    : $"{usuario.NOMBRES} {usuario.APELLIDOS} fue ascendido a Coordinador exitosamente.";
            }
            else
            {
                TempData["Mensaje"] = $"{usuario.NOMBRES} {usuario.APELLIDOS} volvió a tener el rol de Docente.";
            }

            return RedirectToAction("Usuarios");
        }
        // 1. Acción para ver la lista
        [RequierePermiso(Permisos.VerReportes)]
        public IActionResult ListaEstudiantes()
        {
            var estudiantes = _context.ESTUDIANTE.ToList(); // Trae todos los de la DB
            return View(estudiantes);
        }
        [RequierePermiso(Permisos.VerReportes)]
        public IActionResult DescargarReportePro(int id)
        {
            // 1. Buscamos al estudiante
            var estudiante = _context.ESTUDIANTE.FirstOrDefault(e => e.id_estudiante == id);
            if (estudiante == null) return NotFound();


            var modelo = new ReporteEstudianteViewModel
            {
                IdEstudiante = estudiante.id_estudiante,
                NombreCompleto = $"{estudiante.nombre} {estudiante.apellido}",
                Codigo = estudiante.codigo_estudiante
            };


            // 2. Consulta ajustada a tu tabla real
            var notasAgrupadas = _context.Calificaciones
                .Where(c => c.ID_Estudiante == id)
                .ToList()
                .GroupBy(c => c.Materia) // Agrupamos por el nombre de la materia ("Matemáticas", etc.)
                .Select(grupo => new FilaNota
                {
                    NombreMateria = grupo.Key, // El nombre que ya tienes en la tabla
                                               // Filtramos por la columna 'Periodo' de tu SQL
                    Periodo1 = (double)(grupo.FirstOrDefault(x => x.Periodo == 1)?.Nota ?? 0),
                    Periodo2 = (double)(grupo.FirstOrDefault(x => x.Periodo == 2)?.Nota ?? 0),
                    Periodo3 = (double)(grupo.FirstOrDefault(x => x.Periodo == 3)?.Nota ?? 0),
                    Periodo4 = (double)(grupo.FirstOrDefault(x => x.Periodo == 4)?.Nota ?? 0)
                }).ToList();


            modelo.NotasPorMateria = notasAgrupadas;


            // 3. Generación del PDF sin tildes en el nombre para evitar errores
            return new ViewAsPdf("VistaReportePDF", modelo)
            {
                FileName = "ReporteAcademico.pdf",
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                CustomSwitches = "--allow ./" // Ayuda a cargar imágenes locales
            };
        }


        // --- GESTIÓN DE NOTIFICACIONES / COMUNICADOS POR ROL ---
        [HttpGet]
        [RequierePermiso(Permisos.EnviarNotificaciones)]
        public async Task<IActionResult> Notificaciones()
        {


            var lista = await _context.Notificaciones
                .Include(n => n.Emisor)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();


            return View(lista);
        }


        [HttpPost]
        [RequierePermiso(Permisos.EnviarNotificaciones)]
        public async Task<IActionResult> CrearNotificacion(string titulo, string mensaje, string rolDestino)
        {


            int? idUsuarioSession = HttpContext.Session.GetInt32("UserId");
            if (!idUsuarioSession.HasValue)
            {
                return RedirectToAction("Index", "Login");
            }


            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(mensaje) || string.IsNullOrWhiteSpace(rolDestino))
            {
                TempData["Error"] = "Todos los campos son obligatorios para publicar un comunicado.";
                return RedirectToAction("Notificaciones");
            }


            var nuevaNotificacion = new Notificacion
            {
                Titulo = titulo.Trim(),
                Mensaje = mensaje.Trim(),
                RolDestino = rolDestino.Trim().ToLower(),
                FechaCreacion = DateTime.Now,
                ID_UsuarioEmisor = idUsuarioSession.Value,
                Activa = true
            };


            _context.Notificaciones.Add(nuevaNotificacion);
            await _context.SaveChangesAsync();


            TempData["Exito"] = "Notificación publicada y enviada exitosamente.";
            return RedirectToAction("Notificaciones");
        }


        [HttpPost]
        [RequierePermiso(Permisos.EnviarNotificaciones)]
        public async Task<IActionResult> EliminarNotificacion(int id)
        {


            var notificacion = await _context.Notificaciones.FindAsync(id);
            if (notificacion != null)
            {
                _context.Notificaciones.Remove(notificacion);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "La notificación fue eliminada.";
            }


            return RedirectToAction("Notificaciones");
        }


        // =====================================================
        // CRUD Y GESTIÓN AVANZADA DE MATRÍCULAS
        // =====================================================

        /// <summary>
        /// Consulta el historial académico completo de un estudiante (matrículas internas y certificados externos).
        /// </summary>
        [HttpGet]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> HistorialMatriculas(int idEstudiante)
        {
            var historial = await _matriculaService.ObtenerHistorialAcademicoAsync(idEstudiante);
            return Json(historial);
        }

        /// <summary>
        /// Obtiene los certificados externos registrados para un estudiante y su estado de verificación.
        /// </summary>
        [HttpGet]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> ListarCertificadosExternos(int idEstudiante)
        {
            var estudiante = await _context.ESTUDIANTE.FindAsync(idEstudiante);
            var certs = await _context.CertificadosEstudioExterno
                .Where(c => c.IdEstudiante == idEstudiante && c.Activo)
                .OrderBy(c => c.GradoAprobado)
                .Select(c => new
                {
                    c.IdCertificadoExterno,
                    c.IdEstudiante,
                    c.GradoAprobado,
                    c.AnoLectivo,
                    c.InstitucionOrigen,
                    c.ArchivoRuta,
                    c.EstadoVerificacion,
                    FechaVerificacion = c.FechaVerificacion.HasValue ? c.FechaVerificacion.Value.ToString("yyyy-MM-dd") : "-",
                    c.RevisadoPor,
                    c.Observaciones,
                    FechaRegistro = c.FechaRegistro.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            var gradosAprobados = await _matriculaService.ObtenerGradosAprobadosAsync(idEstudiante);

            return Json(new
            {
                nombreEstudiante = estudiante != null ? $"{estudiante.nombre} {estudiante.apellido}".Trim() : "Estudiante",
                codigoEstudiante = estudiante?.codigo_estudiante ?? "",
                gradosAprobados,
                certificados = certs
            });
        }

        /// <summary>
        /// Registra un certificado externo de estudio para estudiantes antiguos o de transferencia.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> RegistrarCertificadoExterno(
            int idEstudiante,
            int gradoAprobado,
            int anoLectivo,
            string institucionOrigen,
            string? observaciones,
            IFormFile? archivoCertificado)
        {
            string usuarioActivo = HttpContext.Session.GetString("UsuarioNombre") ?? User.Identity?.Name ?? "Secretaría";
            string? rutaArchivo = null;

            if (archivoCertificado != null && archivoCertificado.Length > 0)
            {
                try
                {
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "certificados");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }
                    var extension = Path.GetExtension(archivoCertificado.FileName);
                    var fileName = $"Cert_{idEstudiante}_G{gradoAprobado}_{DateTime.Now.Ticks}{extension}";
                    var fullPath = Path.Combine(uploadsDir, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await archivoCertificado.CopyToAsync(stream);
                    }
                    rutaArchivo = $"/uploads/certificados/{fileName}";
                }
                catch
                {
                    // Manejo silencioso de archivo físico
                }
            }

            var nuevoCert = new CertificadoEstudioExterno
            {
                IdEstudiante = idEstudiante,
                GradoAprobado = gradoAprobado,
                AnoLectivo = anoLectivo,
                InstitucionOrigen = string.IsNullOrWhiteSpace(institucionOrigen) ? "Institución externa acreditada" : institucionOrigen.Trim(),
                ArchivoRuta = rutaArchivo,
                EstadoVerificacion = "Pendiente de verificación",
                Observaciones = observaciones,
                FechaRegistro = DateTime.Now,
                Activo = true
            };

            var resultado = await _matriculaService.RegistrarCertificadoExternoAsync(nuevoCert, usuarioActivo);
            if (resultado.Exito)
            {
                TempData["MensajeMatriculas"] = resultado.Mensaje;
            }
            else
            {
                TempData["ErrorMatriculas"] = resultado.Mensaje;
            }

            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>
        /// Verifica o rechaza un certificado de estudio externo.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> VerificarCertificadoExterno(int idCertificado, string estadoVerificacion, string? observaciones)
        {
            string usuarioActivo = HttpContext.Session.GetString("UsuarioNombre") ?? User.Identity?.Name ?? "Secretaría";
            var resultado = await _matriculaService.VerificarCertificadoExternoAsync(idCertificado, estadoVerificacion, observaciones, usuarioActivo);

            if (resultado.Exito)
            {
                TempData["MensajeMatriculas"] = resultado.Mensaje;
            }
            else
            {
                TempData["ErrorMatriculas"] = resultado.Mensaje;
            }

            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>
        /// Realiza la revisión documental y administrativa de una matrícula.
        /// No altera ni sobreescribe el estado académico.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> RevisarDocumentosMatricula(
            int idMatricula,
            string estadoDocumental,
            string? documentosPendientes,
            string? observaciones)
        {
            string usuarioActivo = HttpContext.Session.GetString("UsuarioNombre") ?? User.Identity?.Name ?? "Secretaría";
            var resultado = await _matriculaService.RevisarDocumentosAsync(idMatricula, estadoDocumental, documentosPendientes, observaciones, usuarioActivo);

            if (resultado.Exito)
            {
                TempData["MensajeMatriculas"] = resultado.Mensaje;
            }
            else
            {
                TempData["ErrorMatriculas"] = resultado.Mensaje;
            }

            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>
        /// Actualiza el estado académico (Aprobado, Reprobado, Pendiente) de un curso/matrícula.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> ActualizarEstadoAcademicoMatricula(int idMatricula, string estadoAcademico, string? observaciones)
        {
            string usuarioActivo = HttpContext.Session.GetString("UsuarioNombre") ?? User.Identity?.Name ?? "Secretaría";
            var resultado = await _matriculaService.ActualizarEstadoAcademicoAsync(idMatricula, estadoAcademico, observaciones, usuarioActivo);

            if (resultado.Exito)
            {
                TempData["MensajeMatriculas"] = resultado.Mensaje;
            }
            else
            {
                TempData["ErrorMatriculas"] = resultado.Mensaje;
            }

            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>
        /// Asigna definitivamente a un estudiante a un curso tras cumplir validaciones documentales y académicas.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> AsignarCursoDefinitivo(int idMatricula, int idCurso)
        {
            string usuarioActivo = HttpContext.Session.GetString("UsuarioNombre") ?? User.Identity?.Name ?? "Secretaría";
            var resultado = await _matriculaService.AsignarCursoDefinitivoAsync(idMatricula, idCurso, usuarioActivo);

            if (resultado.Exito)
            {
                TempData["MensajeMatriculas"] = resultado.Mensaje;
            }
            else
            {
                TempData["ErrorMatriculas"] = resultado.Mensaje;
            }

            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>
        /// Actualiza los campos de una matrícula respetando la independencia de estados.
        /// </summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> EditarMatricula(
            int idMatricula,
            int? idCurso,
            int? gradoSolicitado,
            string estado,
            string? estadoAcademico,
            string? estadoDocumental,
            string? observaciones)
        {
            var matricula = await _context.Matriculas.FindAsync(idMatricula);
            if (matricula == null)
            {
                TempData["ErrorMatriculas"] = "No se encontró la matrícula indicada.";
                return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
            }

            if (gradoSolicitado.HasValue)
            {
                matricula.grado_solicitado = gradoSolicitado.Value;
            }
            if (idCurso.HasValue && idCurso.Value > 0)
            {
                matricula.id_curso = idCurso.Value;
            }
            if (!string.IsNullOrWhiteSpace(estado))
            {
                matricula.estado = estado;
            }
            if (!string.IsNullOrWhiteSpace(estadoAcademico))
            {
                matricula.estado_academico = estadoAcademico;
            }
            if (!string.IsNullOrWhiteSpace(estadoDocumental))
            {
                matricula.estado_documental = estadoDocumental;
            }
            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                matricula.observaciones = observaciones;
            }

            // Si tiene curso y la revisión documental está aprobada, sincronizar en ESTUDIANTE
            if (matricula.id_curso.HasValue && matricula.estado_documental == "Aprobada")
            {
                var est = await _context.ESTUDIANTE.FindAsync(matricula.id_estudiante);
                var cur = await _context.Cursos.FindAsync(matricula.id_curso.Value);
                if (est != null && cur != null)
                {
                    est.id_curso = cur.id_curso;
                    est.curso_asignado = cur.nombre_curso;
                }
            }

            await _context.SaveChangesAsync();
            TempData["MensajeMatriculas"] = "Matrícula actualizada correctamente con diferenciación de estados.";
            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>Cambia únicamente el estado de una matrícula.</summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> CambiarEstadoMatricula(int idMatricula, string nuevoEstado)
        {
            var matricula = await _context.Matriculas.FindAsync(idMatricula);
            if (matricula != null)
            {
                matricula.estado = nuevoEstado;
                await _context.SaveChangesAsync();
                TempData["MensajeMatriculas"] = $"Estado de matrícula cambiado a '{nuevoEstado}' correctamente.";
            }
            else
            {
                TempData["ErrorMatriculas"] = "No se encontró la matrícula indicada.";
            }

            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>Elimina permanentemente una matrícula de la base de datos.</summary>
        [HttpPost]
        [RequierePermiso(Permisos.EliminarMatriculas)]
        public async Task<IActionResult> EliminarMatricula(int idMatricula)
        {
            var matricula = await _context.Matriculas.FindAsync(idMatricula);
            if (matricula != null)
            {
                _context.Matriculas.Remove(matricula);
                await _context.SaveChangesAsync();
                TempData["MensajeMatriculas"] = "Matrícula eliminada correctamente.";
            }
            else
            {
                TempData["ErrorMatriculas"] = "No se encontró la matrícula a eliminar.";
            }

            return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
        }

        /// <summary>
        /// Suite de pruebas automatizada de criterios de aceptación de matrículas.
        /// Verifica los 9 escenarios obligatorios solicitados por el usuario.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EjecutarPruebasMatriculas()
        {
            var resultados = new List<object>();

            // 0. Crear estudiante de prueba
            var estPrueba = new Estudiante
            {
                nombre = "Carlos Andres",
                apellido = "Prueba Validacion",
                email = $"test_mat_{DateTime.Now.Ticks}@pae.edu.co",
                codigo_estudiante = $"TEST-{DateTime.Now.Ticks % 100000}",
                fecha_inscripcion = DateTime.Now
            };
            _context.ESTUDIANTE.Add(estPrueba);
            await _context.SaveChangesAsync();
            int idEst = estPrueba.id_estudiante;

            try
            {
                // PRUEBA 1: Matrícula en grado superior sin historial ni certificados
                var p1 = await _matriculaService.ValidarAvanceGradoEstudianteAsync(idEst, 7, 2026);
                resultados.Add(new
                {
                    Prueba = "1. Bloqueo de avance a Grado 7° sin grado previo aprobado",
                    ResultadoEsperado = "Falla de validación por falta de Grado 6°",
                    EsValido = !p1.Valido,
                    Detalle = p1.Mensaje,
                    Estado = !p1.Valido ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 2: Certificado pendiente no otorga avance automático
                var cert5Pend = new CertificadoEstudioExterno
                {
                    IdEstudiante = idEst,
                    GradoAprobado = 5,
                    AnoLectivo = 2025,
                    InstitucionOrigen = "Colegio Externo Primaria",
                    EstadoVerificacion = "Pendiente de verificación"
                };
                await _matriculaService.RegistrarCertificadoExternoAsync(cert5Pend, "AdminPruebas");

                var p2 = await _matriculaService.ValidarAvanceGradoEstudianteAsync(idEst, 6, 2026);
                resultados.Add(new
                {
                    Prueba = "2. Certificado externo en estado Pendiente no permite avance automático a Grado 6°",
                    ResultadoEsperado = "Falla de validación indicando que el certificado está pendiente de verificación",
                    EsValido = !p2.Valido,
                    Detalle = p2.Mensaje,
                    Estado = !p2.Valido ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 3: Secretaría verifica certificados de 1° a 5° y se habilita Grado 6°
                await _matriculaService.VerificarCertificadoExternoAsync(cert5Pend.IdCertificadoExterno, "Verificado", "Documento original cotejado", "Secretaría General");
                for (int g = 1; g <= 4; g++)
                {
                    var cG = new CertificadoEstudioExterno
                    {
                        IdEstudiante = idEst,
                        GradoAprobado = g,
                        AnoLectivo = 2020 + g,
                        InstitucionOrigen = "Colegio Externo Primaria",
                        EstadoVerificacion = "Verificado"
                    };
                    await _matriculaService.RegistrarCertificadoExternoAsync(cG, "Secretaría General");
                }

                var p3 = await _matriculaService.ValidarAvanceGradoEstudianteAsync(idEst, 6, 2026);
                resultados.Add(new
                {
                    Prueba = "3. Certificados 1°-5° verificados reconocen antecedentes y habilitan Grado 6°",
                    ResultadoEsperado = "Validación satisfactoria",
                    EsValido = p3.Valido,
                    Detalle = p3.Mensaje,
                    Estado = p3.Valido ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 4: Asignación definitiva bloqueada si estado documental no está Aprobado
                var mat6 = new Matricula
                {
                    id_estudiante = idEst,
                    grado_solicitado = 6,
                    ano = 2026,
                    fecha_matricula = DateTime.Now,
                    periodo_academico = "2026-I",
                    estado = "Registrada",
                    estado_academico = "Pendiente de resultado",
                    estado_documental = "Pendiente de documentos",
                    documentos_pendientes = "Falta fotocopia TI"
                };
                _context.Matriculas.Add(mat6);
                await _context.SaveChangesAsync();

                var curso6 = await _context.Cursos.FirstOrDefaultAsync(c => c.nombre_curso.StartsWith("601"));
                int idCurso6 = curso6 != null ? curso6.id_curso : 1;

                var p4 = await _matriculaService.AsignarCursoDefinitivoAsync(mat6.id_matricula, idCurso6, "Secretaría");
                resultados.Add(new
                {
                    Prueba = "4. Asignación definitiva bloqueada por revisión documental pendiente",
                    ResultadoEsperado = "Falla de asignación exigiendo aprobación documental",
                    EsValido = !p4.Exito,
                    Detalle = p4.Mensaje,
                    Estado = !p4.Exito ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 5: Aprobación documental permite asignación definitiva a curso
                await _matriculaService.RevisarDocumentosAsync(mat6.id_matricula, "Aprobada", null, "Documentos completos y válidos", "Secretaría");
                var p5 = await _matriculaService.AsignarCursoDefinitivoAsync(mat6.id_matricula, idCurso6, "Secretaría");
                var estActualizado = await _context.ESTUDIANTE.FindAsync(idEst);

                bool p5Ok = p5.Exito && estActualizado?.id_curso == idCurso6 && mat6.id_curso == idCurso6;
                resultados.Add(new
                {
                    Prueba = "5. Asignación definitiva autorizada tras aprobación documental (actualiza MATRICULA y ESTUDIANTE)",
                    ResultadoEsperado = "Asignación exitosa al curso 601 M",
                    EsValido = p5Ok,
                    Detalle = p5.Mensaje + $" (Curso asignado en Estudiante: {estActualizado?.curso_asignado})",
                    Estado = p5Ok ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 6: Bloqueo de avance si resultado académico está 'Pendiente de resultado'
                var p6 = await _matriculaService.ValidarAvanceGradoEstudianteAsync(idEst, 7, 2027);
                resultados.Add(new
                {
                    Prueba = "6. Resultado académico Pendiente impide avance automático al grado siguiente",
                    ResultadoEsperado = "Falla de validación por estado académico pendiente",
                    EsValido = !p6.Valido,
                    Detalle = p6.Mensaje,
                    Estado = !p6.Valido ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 7: Bloqueo de avance si resultado académico es 'Reprobado'
                await _matriculaService.ActualizarEstadoAcademicoAsync(mat6.id_matricula, "Reprobado", "Comisión final de evaluación", "Secretaría");
                var p7 = await _matriculaService.ValidarAvanceGradoEstudianteAsync(idEst, 7, 2027);
                resultados.Add(new
                {
                    Prueba = "7. Resultado académico Reprobado obliga a repetir y bloquea avance a Grado 7°",
                    ResultadoEsperado = "Falla de validación obligando a repetir Grado 6°",
                    EsValido = !p7.Valido,
                    Detalle = p7.Mensaje,
                    Estado = !p7.Valido ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 8: Resultado académico 'Aprobado' habilita avance a Grado 7°
                await _matriculaService.ActualizarEstadoAcademicoAsync(mat6.id_matricula, "Aprobado", "Aprobó año escolar", "Secretaría");
                var p8 = await _matriculaService.ValidarAvanceGradoEstudianteAsync(idEst, 7, 2027);
                resultados.Add(new
                {
                    Prueba = "8. Resultado académico Aprobado autoriza avance al Grado 7°",
                    ResultadoEsperado = "Validación satisfactoria",
                    EsValido = p8.Valido,
                    Detalle = p8.Mensaje,
                    Estado = p8.Valido ? "PASÓ" : "FALLÓ"
                });

                // PRUEBA 9: Historial académico completo conserva todos los registros sin sobrescritura
                var historial = await _matriculaService.ObtenerHistorialAcademicoAsync(idEst);
                bool p9Ok = historial.Registros.Count == 6 // 5 certificados externos + 1 matrícula de 6°
                            && historial.UltimoGradoAprobado == 6
                            && historial.SiguienteGradoHabilitado == 7;

                resultados.Add(new
                {
                    Prueba = "9. Historial académico conserva antecedentes externos (1°-5°) y matrículas internas (6°) sin pérdidas",
                    ResultadoEsperado = "6 registros en historial, último grado aprobado: 6°, siguiente habilitado: 7°",
                    EsValido = p9Ok,
                    Detalle = $"Total registros: {historial.Registros.Count}. Último aprobado: {historial.UltimoGradoAprobado}°. {historial.EstadoAcademicoGeneral}",
                    Estado = p9Ok ? "PASÓ" : "FALLÓ"
                });

                // Limpieza de datos de prueba
                _context.Matriculas.Remove(mat6);
                var certsAEliminar = _context.CertificadosEstudioExterno.Where(c => c.IdEstudiante == idEst).ToList();
                _context.CertificadosEstudioExterno.RemoveRange(certsAEliminar);
                _context.ESTUDIANTE.Remove(estPrueba);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                resultados.Add(new
                {
                    Prueba = "Error de ejecución",
                    Detalle = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "")
                });
            }

            return Json(new
            {
                FechaPruebas = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalPruebas = resultados.Count,
                TodasExitosas = resultados.All(r => (r as dynamic).Estado == "PASÓ"),
                Detalles = resultados
            });
        }
    }
}