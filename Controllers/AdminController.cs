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


        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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
        public IActionResult RenovarMatricula(int idEstudiante)
        {
            // Buscamos la matrícula más reciente de este estudiante
            var ultimaMatricula = _context.Matriculas
                .Where(m => m.id_estudiante == idEstudiante)
                .OrderByDescending(m => m.ano)
                .FirstOrDefault();


            if (ultimaMatricula != null)
            {
                var nuevaMatricula = new Matricula
                {
                    id_estudiante = idEstudiante,
                    id_curso = ultimaMatricula.id_curso, // Lo dejamos en el mismo curso o podrías subirlo
                    fecha_matricula = DateTime.Now,
                    periodo_academico = "2026-I",
                    estado = "Activa",
                    ano = 2026 // Año de renovación
                };


                _context.Matriculas.Add(nuevaMatricula);
                _context.SaveChanges();
            }


            return RedirectToAction("Usuarios");
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
        // CRUD DE MATRÍCULAS
        // =====================================================


        /// <summary>Actualiza el grado y el estado de una matrícula existente.</summary>
        [HttpPost]
        [RequierePermiso(Permisos.GestionarMatriculas)]
        public async Task<IActionResult> EditarMatricula(int idMatricula, int idCurso, string estado)
        {


            var matricula = await _context.Matriculas.FindAsync(idMatricula);
            if (matricula == null)
            {
                TempData["ErrorMatriculas"] = "No se encontró la matrícula indicada.";
                return RedirectToAction("Dashboard", "Home", new { rol = "admin", seccion = "matricula" });
            }


            matricula.id_curso = idCurso;
            matricula.estado = estado;
            await _context.SaveChangesAsync();


            TempData["MensajeMatriculas"] = "Matrícula actualizada correctamente.";
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
                TempData["MensajeMatriculas"] = $"Estado cambiado a '{nuevoEstado}' correctamente.";
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
    }
}