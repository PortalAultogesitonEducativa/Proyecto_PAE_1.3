using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using ProyectoPAE.Servicios;
using Microsoft.EntityFrameworkCore;

public class EstudianteController : Controller
{
    private readonly ApplicationDbContext _context;
    public EstudianteController(ApplicationDbContext context) { _context = context; }
    // Solo debe existir una versión de este método en el archivo
    public IActionResult MisNotas(int? idEstudiante)
    {
        var rolSesion = Permisos.RolActual(HttpContext);
        var idSesion = HttpContext.Session.GetInt32("UserId");

        // Sin sesión válida: al login
        if (string.IsNullOrEmpty(rolSesion) || !idSesion.HasValue)
            return RedirectToAction("Index", "Login");

        int userId;
        if (idEstudiante.HasValue)
        {
            // Acudiente consultando a su hijo, o personal académico consultando a un estudiante.
            // Un estudiante solo puede ver sus propias notas y un acudiente solo las de sus hijos.
            if (!AccesoEstudiantes.PuedeVerEstudiante(_context, HttpContext, idEstudiante.Value))
                return RedirectToAction("Dashboard", "Home");

            userId = idEstudiante.Value;
        }
        else
        {
            // Estudiante: sus propias notas
            userId = idSesion.Value;
        }

        // Consulta a la base de datos filtrando por el ID determinado
        var notas = _context.Calificaciones
                            .Where(n => n.ID_Estudiante == userId)
                            .ToList();

        // RF 5.2: Cálculo automático del promedio para la vista
        double promedio = notas.Any() ? notas.Average(n => (double)n.Nota) : 0.0;
        ViewBag.PromedioGeneral = promedio;

        _context.AsegurarEsquemaAccionesMejora();
        var mejoras = _context.AccionesMejora
            .Where(a => a.IdEstudiante == userId && a.Activo)
            .OrderByDescending(a => a.FechaRegistro)
            .ToList();
        ViewBag.AccionesMejora = mejoras;

        return View(notas);
    }

    // ===============================================
    // INSCRIPCIÓN A CURSOS EXTRACURRICULARES (ESTUDIANTE)
    // ===============================================

    [HttpPost]
    [RequierePermiso(Permisos.ParticiparComoEstudiante)]
    public IActionResult InscribirseCursoExtra(int idCurso)
    {
        try
        {
        _context.AsegurarEsquemaExtracurriculares();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "Debes iniciar sesión para inscribirte." });
        }

        var curso = _context.ExtraCursos.Find(idCurso);
        if (curso == null || !curso.Activo)
        {
            return Json(new { success = false, message = "El curso no se encuentra disponible." });
        }

        var hoy = DateTime.Now.Date;
        if (hoy < curso.FechaInicioInscripcion.Date)
        {
            return Json(new { success = false, message = $"Las inscripciones inician el {curso.FechaInicioInscripcion:dd/MM/yyyy}." });
        }

        if (hoy > curso.FechaFinInscripcion.Date)
        {
            return Json(new { success = false, message = $"El periodo de inscripciones cerró el {curso.FechaFinInscripcion:dd/MM/yyyy}." });
        }

        if (curso.CuposDisponibles <= 0)
        {
            return Json(new { success = false, message = "Lo sentimos, los cupos para este curso ya se encuentran agotados." });
        }

        // Verificar si ya está inscrito o tiene solicitud pendiente
        var inscripcionExistente = _context.ExtraInscripciones
            .FirstOrDefault(i => i.IdEstudiante == userId.Value && i.IdExtraCurso == idCurso && (i.Estado == "Inscrito" || i.Estado == "PendienteAprobacion"));

        if (inscripcionExistente != null)
        {
            var msgExistente = inscripcionExistente.Estado == "PendienteAprobacion"
                ? "Ya enviaste tu solicitud de inscripción. Está pendiente de aprobación por el docente."
                : "Ya te encuentras inscrito en este curso extracurricular.";
            return Json(new { success = false, message = msgExistente });
        }

        // Verificar grado del estudiante si el curso tiene rango definido
        var usuarioActual = _context.Usuarios.Find(userId.Value);
        var estudianteObj = _context.ESTUDIANTE
            .FirstOrDefault(e => e.id_usuario == userId.Value || (usuarioActual != null && e.email == usuarioActual.CORREO_ELECTRONICO));

        int? gradoEstudiante = null;
        if (estudianteObj != null)
        {
            var matricula = _context.Matriculas
                .Where(m => m.id_estudiante == estudianteObj.id_estudiante && m.estado != null && (m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo"))
                .OrderByDescending(m => m.ano)
                .ThenByDescending(m => m.id_matricula)
                .FirstOrDefault();

            if (matricula != null)
            {
                if (matricula.id_curso.HasValue)
                {
                    var cursoObj = _context.Cursos.Find(matricula.id_curso.Value);
                    if (cursoObj != null)
                    {
                        gradoEstudiante = MatriculaAcademicaService.ExtraerGrado(cursoObj.nombre_curso, matricula.grado_solicitado ?? 6);
                    }
                    else
                    {
                        gradoEstudiante = matricula.grado_solicitado;
                    }
                }
                else
                {
                    gradoEstudiante = matricula.grado_solicitado;
                }
            }

            if (!gradoEstudiante.HasValue && !string.IsNullOrEmpty(estudianteObj.curso_asignado))
            {
                gradoEstudiante = MatriculaAcademicaService.ExtraerGrado(estudianteObj.curso_asignado, 6);
            }
        }

        if (gradoEstudiante.HasValue && gradoEstudiante.Value > 0)
        {
            if (curso.GradoMin.HasValue && gradoEstudiante.Value < curso.GradoMin.Value)
            {
                return Json(new { success = false, message = $"Este curso está dirigido a estudiantes de grado {curso.GradoMin}° a {curso.GradoMax}° (Tu grado actual: {gradoEstudiante}°)." });
            }
            if (curso.GradoMax.HasValue && gradoEstudiante.Value > curso.GradoMax.Value)
            {
                return Json(new { success = false, message = $"Este curso está dirigido a estudiantes de grado {curso.GradoMin}° a {curso.GradoMax}° (Tu grado actual: {gradoEstudiante}°)." });
            }
        }

        // Obtener lista de documentos requeridos por el profesor o por tipo de curso
        List<string> docsList = new List<string>();
        if (!string.IsNullOrWhiteSpace(curso.DocumentosRequeridos))
        {
            try
            {
                if (curso.DocumentosRequeridos.Trim().StartsWith("["))
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(curso.DocumentosRequeridos);
                    if (parsed != null && parsed.Any()) docsList = parsed;
                }
                else
                {
                    docsList = curso.DocumentosRequeridos
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(d => d.Trim())
                        .Where(d => !string.IsNullOrEmpty(d))
                        .ToList();
                }
            }
            catch
            {
                docsList = new List<string>();
            }
        }

        if (!docsList.Any())
        {
            if (curso.TipoCurso?.ToLower() == "deportivo")
                docsList = new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Certificado Médico de Aptitud Física", "Copia de EPS o Seguro Estudiantil" };
            else if (curso.TipoCurso?.ToLower() == "servicio social y comunitario" || curso.TipoCurso?.ToLower() == "servicio social")
                docsList = new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Copia de EPS o Seguro Estudiantil", "Paz y Salvo Institucional" };
            else
                docsList = new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Copia de EPS o Seguro Estudiantil" };
        }

        // Limpiar cualquier borrador previo que haya quedado pendiente
        var borradorPrevio = _context.ExtraInscripciones
            .FirstOrDefault(i => i.IdEstudiante == userId.Value && i.IdExtraCurso == idCurso && i.Estado == "Borrador");
        if (borradorPrevio != null)
        {
            var docsBorrador = _context.ExtraDocumentosInscripciones
                .Where(d => d.IdExtraInscripcion == borradorPrevio.IdExtraInscripcion);
            _context.ExtraDocumentosInscripciones.RemoveRange(docsBorrador);
            _context.ExtraInscripciones.Remove(borradorPrevio);
            _context.SaveChanges();
        }

        // Crear registro en estado "Borrador". NO entrará a la cola del docente hasta que suba todos los documentos.
        var nuevaInscripcion = new ExtraInscripcion
        {
            IdEstudiante = userId.Value,
            IdExtraCurso = idCurso,
            FechaInscripcion = DateTime.Now,
            Estado = "Borrador"
        };
        _context.ExtraInscripciones.Add(nuevaInscripcion);
        _context.SaveChanges();

        return Json(new
        {
            success = true,
            pendienteDocumentos = true,
            idInscripcion = nuevaInscripcion.IdExtraInscripcion,
            documentosRequeridos = docsList,
            message = $"Para postularte a '{curso.Nombre}', es obligatorio adjuntar los documentos solicitados antes de enviar la solicitud al docente."
        });
    
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al procesar la inscripción: " + ex.Message });
        }
    }

    [HttpPost]
    [RequierePermiso(Permisos.ParticiparComoEstudiante)]
    public IActionResult CancelarInscripcionCursoExtra(int idInscripcion)
    {
        _context.AsegurarEsquemaExtracurriculares();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "Sesión inválida." });
        }

        var inscripcion = _context.ExtraInscripciones.Find(idInscripcion);
        if (inscripcion == null || inscripcion.IdEstudiante != userId.Value)
        {
            return Json(new { success = false, message = "Inscripción no encontrada." });
        }

        if (inscripcion.Estado == "Borrador")
        {
            var docsBorrador = _context.ExtraDocumentosInscripciones
                .Where(d => d.IdExtraInscripcion == idInscripcion);
            _context.ExtraDocumentosInscripciones.RemoveRange(docsBorrador);
            _context.ExtraInscripciones.Remove(inscripcion);
            _context.SaveChanges();
            return Json(new { success = true, message = "Postulación cancelada." });
        }

        if (inscripcion.Estado == "Cancelado")
        {
            return Json(new { success = false, message = "Esta inscripción ya fue cancelada previamente." });
        }

        bool cupoARestaurar = inscripcion.Estado == "Inscrito";
        inscripcion.Estado = "Cancelado";
        inscripcion.FechaCancelacion = DateTime.Now;
        inscripcion.MotivoCancelacion = "Cancelado voluntariamente por el estudiante";

        var curso = _context.ExtraCursos.Find(inscripcion.IdExtraCurso);
        if (curso != null && cupoARestaurar)
        {
            if (curso.CuposDisponibles < curso.CuposTotales)
            {
                curso.CuposDisponibles += 1;
            }
        }

        _context.SaveChanges();

        return Json(new
        {
            success = true,
            message = "Has cancelado tu inscripción exitosamente."
        });
    }

    [HttpPost]
    [RequierePermiso(Permisos.ParticiparComoEstudiante)]
    public IActionResult FinalizarInscripcionConDocumentos(int idInscripcion)
    {
        _context.AsegurarEsquemaExtracurriculares();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Json(new { success = false, message = "Sesión inválida." });

        var insc = _context.ExtraInscripciones.Find(idInscripcion);
        if (insc == null || insc.IdEstudiante != userId.Value)
            return Json(new { success = false, message = "Inscripción no encontrada." });

        if (insc.Estado != "Borrador" && insc.Estado != "PendienteAprobacion")
            return Json(new { success = false, message = "Esta inscripción no se encuentra pendiente de documentos." });

        var curso = _context.ExtraCursos.Find(insc.IdExtraCurso);
        if (curso == null)
            return Json(new { success = false, message = "Curso no encontrado." });

        // Obtener la lista de documentos obligatorios
        List<string> docsRequeridos = new List<string>();
        if (!string.IsNullOrWhiteSpace(curso.DocumentosRequeridos))
        {
            try
            {
                if (curso.DocumentosRequeridos.Trim().StartsWith("["))
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(curso.DocumentosRequeridos);
                    if (parsed != null && parsed.Any()) docsRequeridos = parsed;
                }
                else
                {
                    docsRequeridos = curso.DocumentosRequeridos
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(d => d.Trim())
                        .Where(d => !string.IsNullOrEmpty(d))
                        .ToList();
                }
            }
            catch { }
        }

        if (!docsRequeridos.Any())
        {
            if (curso.TipoCurso?.ToLower() == "deportivo")
                docsRequeridos = new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Certificado Médico de Aptitud Física", "Copia de EPS o Seguro Estudiantil" };
            else if (curso.TipoCurso?.ToLower() == "servicio social y comunitario" || curso.TipoCurso?.ToLower() == "servicio social")
                docsRequeridos = new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Copia de EPS o Seguro Estudiantil", "Paz y Salvo Institucional" };
            else
                docsRequeridos = new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Copia de EPS o Seguro Estudiantil" };
        }

        // Obtener documentos subidos por el estudiante
        var docsSubidos = _context.ExtraDocumentosInscripciones
            .Where(d => d.IdExtraInscripcion == idInscripcion && !string.IsNullOrEmpty(d.ArchivoRuta))
            .ToList();

        if (!docsSubidos.Any())
        {
            return Json(new { success = false, message = "Es obligatorio adjuntar todos los documentos requeridos antes de enviar la solicitud al docente." });
        }

        // Validar que TODOS los documentos requeridos hayan sido adjuntados
        var tiposSubidos = docsSubidos.Select(d => (d.TipoDocumento ?? "").Trim().ToLower()).ToList();
        var faltantes = docsRequeridos.Where(req => !tiposSubidos.Contains(req.Trim().ToLower())).ToList();

        if (faltantes.Any())
        {
            return Json(new { success = false, message = $"Faltan documentos obligatorios por adjuntar ({docsSubidos.Count} de {docsRequeridos.Count} cargados). Por favor adjunta: {string.Join(", ", faltantes)}." });
        }

        // Pasa a estado PendienteAprobacion para que ahora sí ingrese a la lista de cola/revisión del docente
        insc.Estado = "PendienteAprobacion";
        insc.FechaInscripcion = DateTime.Now;
        _context.SaveChanges();

        return Json(new { success = true, message = "¡Documentos cargados exitosamente! Tu postulación ha entrado en la lista de espera para el visto bueno del docente." });
    }

    // ===============================================
    // MÓDULO: ACTIVIDADES Y ENTREGAS (ESTUDIANTE)
    // ===============================================

    /// <summary>
    /// Retorna las actividades activas con el estado de entrega del estudiante (JSON).
    /// </summary>
    [HttpGet]
    [RequierePermiso(Permisos.UsarPanelEstudiante)]
    public IActionResult ObtenerActividades()
    {
        _context.AsegurarEsquemaActividades();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Json(new { success = false, message = "Sesión inválida." });

        var actividades = _context.Actividades
            .Where(a => a.Activo)
            .OrderBy(a => a.FechaLimite)
            .ToList();

        var entregas = _context.EntregasActividades
            .Where(e => e.IdEstudiante == userId.Value)
            .ToList();

        var resultado = actividades.Select(a =>
        {
            var entrega = entregas.FirstOrDefault(e => e.IdActividad == a.IdActividad);
            bool vencida = DateTime.Now > a.FechaLimite;
            string estado = entrega != null ? (!string.IsNullOrEmpty(entrega.Estado) && entrega.Estado == "Calificado" ? "Calificado" : (entrega.Calificacion.HasValue ? "Calificado" : "Entregado")) :
                            (vencida ? "Vencido" : "Pendiente");

            return new
            {
                idActividad = a.IdActividad,
                titulo = a.Titulo,
                descripcion = a.Descripcion,
                materia = a.Materia,
                grado = a.Grado,
                fechaLimite = a.FechaLimite.ToString("dd/MM/yyyy HH:mm"),
                fechaLimiteIso = a.FechaLimite.ToString("o"),
                archivoAdjunto = a.ArchivoAdjunto,
                estado = estado,
                entregada = entrega != null,
                idEntrega = entrega?.IdEntrega,
                fechaEntrega = entrega?.FechaEntrega.ToString("dd/MM/yyyy HH:mm"),
                archivoNombre = entrega?.ArchivoNombre,
                archivoRuta = entrega?.ArchivoRuta,
                comentario = entrega?.Comentario,
                calificacion = entrega?.Calificacion,
                retroalimentacion = entrega?.Retroalimentacion,
                vencida = vencida,
                esMejora = a.EsMejora
            };
        }).ToList();

        return Json(new { success = true, actividades = resultado });
    }

    /// <summary>
    /// Recibe la entrega de un estudiante para una actividad (archivo + comentario).
    /// </summary>
    [HttpPost]
    [RequierePermiso(Permisos.ParticiparComoEstudiante)]
    public async Task<IActionResult> SubirEntregaActividad(int idActividad, string? comentario, IFormFile? archivo)
    {
        _context.AsegurarEsquemaActividades();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Json(new { success = false, message = "Sesión inválida. Por favor inicia sesión nuevamente." });

        var actividad = _context.Actividades.Find(idActividad);
        if (actividad == null || !actividad.Activo)
            return Json(new { success = false, message = "La actividad no existe o está inactiva." });

        if (DateTime.Now > actividad.FechaLimite)
        {
            return Json(new { success = false, message = "La fecha límite de entrega ha expirado. Ya no es posible modificar ni enviar respuestas a esta actividad." });
        }

        // Verificar si ya tiene entrega registrada
        var entregaExistente = _context.EntregasActividades
            .FirstOrDefault(e => e.IdActividad == idActividad && e.IdEstudiante == userId.Value);

        string? rutaArchivo = null;
        string? nombreArchivo = null;

        // Procesar archivo si fue adjuntado
        if (archivo != null && archivo.Length > 0)
        {
            var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!extensionesPermitidas.Contains(extension))
                return Json(new { success = false, message = "Tipo de archivo no permitido. Use: PDF, Word, Excel, PowerPoint, ZIP o imágenes." });

            if (archivo.Length > 5 * 1024 * 1024) // 5 MB máximo según especificación
                return Json(new { success = false, message = "El archivo supera el tamaño máximo permitido de 5 MB." });

            var carpeta = Path.Combine("wwwroot", "uploads", "actividades");
            Directory.CreateDirectory(carpeta);

            nombreArchivo = $"{userId}_{idActividad}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            rutaArchivo = Path.Combine("/uploads/actividades", nombreArchivo);
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using var stream = new FileStream(rutaFisica, FileMode.Create);
            await archivo.CopyToAsync(stream);
        }

        if (string.IsNullOrWhiteSpace(comentario) && (archivo == null || archivo.Length == 0))
        {
            return Json(new { success = false, message = "Debe ingresar una respuesta en texto o adjuntar un archivo (máximo 5 MB)." });
        }

        if (entregaExistente != null)
        {
            // Actualizar entrega existente
            if (rutaArchivo != null)
            {
                entregaExistente.ArchivoRuta = rutaArchivo;
                entregaExistente.ArchivoNombre = archivo!.FileName;
            }
            if (!string.IsNullOrEmpty(comentario))
            {
                entregaExistente.Comentario = comentario;
            }
            entregaExistente.FechaEntrega = DateTime.Now;
            entregaExistente.Estado = "Entregado";
        }
        else
        {
            // Nueva entrega
            var nuevaEntrega = new EntregaActividad
            {
                IdActividad = idActividad,
                IdEstudiante = userId.Value,
                ArchivoRuta = rutaArchivo,
                ArchivoNombre = archivo?.FileName,
                Comentario = comentario,
                FechaEntrega = DateTime.Now,
                Estado = "Entregado"
            };
            _context.EntregasActividades.Add(nuevaEntrega);
        }

        _context.SaveChanges();

        return Json(new
        {
            success = true,
            message = "¡Tu actividad fue enviada exitosamente con el texto y archivos adjuntos!",
            archivoNombre = archivo?.FileName
        });
    }

    /// <summary>
    /// Consulta comentarios de clase y comentarios privados de una actividad para el estudiante.
    /// </summary>
    [HttpGet]
    public IActionResult ObtenerComentariosActividad(int idActividad)
    {
        _context.AsegurarEsquemaActividades();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Json(new { success = false, message = "Sesión inválida." });

        var comentarios = _context.ComentariosActividades
            .Where(c => c.IdActividad == idActividad && (!c.EsPrivado || c.IdEstudiantePrivado == userId.Value))
            .OrderBy(c => c.Fecha)
            .Select(c => new
            {
                idComentario = c.IdComentario,
                idActividad = c.IdActividad,
                idUsuario = c.IdUsuario,
                nombreUsuario = c.NombreUsuario,
                rolUsuario = c.RolUsuario,
                mensaje = c.Mensaje,
                fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"),
                esPrivado = c.EsPrivado,
                esMio = c.IdUsuario == userId.Value
            })
            .ToList();

        return Json(new { success = true, comentarios = comentarios });
    }

    /// <summary>
    /// Registra un comentario de clase o privado del estudiante.
    /// </summary>
    [HttpPost]
    [RequierePermiso(Permisos.ParticiparComoEstudiante)]
    public IActionResult GuardarComentarioActividad(int idActividad, string mensaje, bool esPrivado = false)
    {
        _context.AsegurarEsquemaActividades();

        var userId = HttpContext.Session.GetInt32("UserId");
        var nombreUsuario = HttpContext.Session.GetString("NombreUsuario") ?? "Estudiante";
        if (userId == null)
            return Json(new { success = false, message = "Sesión inválida." });

        if (string.IsNullOrWhiteSpace(mensaje))
            return Json(new { success = false, message = "El mensaje no puede estar vacío." });

        var actividad = _context.Actividades.Find(idActividad);
        if (actividad == null || !actividad.Activo)
            return Json(new { success = false, message = "Actividad no encontrada." });

        var nuevo = new ComentarioActividad
        {
            IdActividad = idActividad,
            IdUsuario = userId.Value,
            NombreUsuario = nombreUsuario,
            RolUsuario = "Estudiante",
            Mensaje = mensaje.Trim(),
            Fecha = DateTime.Now,
            EsPrivado = esPrivado,
            IdEstudiantePrivado = esPrivado ? userId.Value : null
        };

        _context.ComentariosActividades.Add(nuevo);
        _context.SaveChanges();

        return Json(new
        {
            success = true,
            mensaje = "Comentario registrado correctamente.",
            comentario = new
            {
                idComentario = nuevo.IdComentario,
                idActividad = nuevo.IdActividad,
                idUsuario = nuevo.IdUsuario,
                nombreUsuario = nuevo.NombreUsuario,
                rolUsuario = nuevo.RolUsuario,
                mensaje = nuevo.Mensaje,
                fecha = nuevo.Fecha.ToString("dd/MM/yyyy HH:mm"),
                esPrivado = nuevo.EsPrivado,
                esMio = true
            }
        });
    }

    // ===============================================
    // SUBIDA DE DOCUMENTOS PARA CURSOS EXTRACURRICULARES
    // ===============================================

    [HttpPost]
    [RequierePermiso(Permisos.ParticiparComoEstudiante)]
    public async Task<IActionResult> SubirDocumentoExtraCurso(int idInscripcion, string tipoDocumento, IFormFile? archivo)
    {
        _context.AsegurarEsquemaExtracurriculares();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Json(new { success = false, message = "Sesión no válida." });

        if (archivo == null || archivo.Length == 0)
            return Json(new { success = false, message = "Debe seleccionar un archivo para subir." });

        if (string.IsNullOrWhiteSpace(tipoDocumento))
            return Json(new { success = false, message = "El tipo de documento es requerido." });

        var inscripcion = _context.ExtraInscripciones.Find(idInscripcion);
        if (inscripcion == null || inscripcion.IdEstudiante != userId.Value)
            return Json(new { success = false, message = "Inscripción no encontrada." });

        var extPermitidas = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!extPermitidas.Contains(ext))
            return Json(new { success = false, message = "Formato no permitido. Solo se admiten archivos PDF, imágenes o Word." });

        if (archivo.Length > 5 * 1024 * 1024)
            return Json(new { success = false, message = "El archivo supera el tamaño máximo permitido de 5 MB." });

        var carpeta = Path.Combine("wwwroot", "uploads", "documentos_extra");
        Directory.CreateDirectory(carpeta);

        var safeNombreDoc = string.Concat(tipoDocumento.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        var nombreGuardado = $"doc_{idInscripcion}_{safeNombreDoc}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
        var rutaFisica = Path.Combine(carpeta, nombreGuardado);

        using (var stream = new FileStream(rutaFisica, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        var rutaWeb = $"/uploads/documentos_extra/{nombreGuardado}";

        var docExistente = _context.ExtraDocumentosInscripciones
            .FirstOrDefault(d => d.IdExtraInscripcion == idInscripcion && d.TipoDocumento.ToLower() == tipoDocumento.ToLower());

        if (docExistente != null)
        {
            docExistente.ArchivoRuta = rutaWeb;
            docExistente.ArchivoNombre = archivo.FileName;
            docExistente.FechaSubida = DateTime.Now;
            docExistente.Estado = "Cargado";
        }
        else
        {
            _context.ExtraDocumentosInscripciones.Add(new ExtraDocumentoInscripcion
            {
                IdExtraInscripcion = idInscripcion,
                TipoDocumento = tipoDocumento,
                ArchivoRuta = rutaWeb,
                ArchivoNombre = archivo.FileName,
                FechaSubida = DateTime.Now,
                Estado = "Cargado"
            });
        }

        _context.SaveChanges();

        return Json(new
        {
            success = true,
            message = $"¡Documento '{tipoDocumento}' cargado exitosamente!",
            archivoNombre = archivo.FileName,
            archivoRuta = rutaWeb
        });
    }

    [HttpGet]
    public IActionResult ObtenerDocumentosInscripcion(int idInscripcion)
    {
        var rol = Permisos.RolActual(HttpContext);
        var idSesion = HttpContext.Session.GetInt32("UserId");

        if (string.IsNullOrEmpty(rol) || !idSesion.HasValue)
            return new JsonResult(new { success = false, message = "Sesión no válida." }) { StatusCode = 401 };

        // Personal académico ve cualquier inscripción; el estudiante solo las suyas.
        bool esPersonal = rol == "admin" || rol == "docente" || rol == "coordinador";
        bool esDueno = rol == "estudiante" &&
                       _context.ExtraInscripciones.Any(i => i.IdExtraInscripcion == idInscripcion && i.IdEstudiante == idSesion.Value);

        if (!esPersonal && !esDueno)
            return new JsonResult(new { success = false, message = "No tienes permiso para ver estos documentos." }) { StatusCode = 403 };

        _context.AsegurarEsquemaExtracurriculares();

        var docs = _context.ExtraDocumentosInscripciones
            .Where(d => d.IdExtraInscripcion == idInscripcion)
            .Select(d => new
            {
                d.IdDocumento,
                d.TipoDocumento,
                d.ArchivoNombre,
                d.ArchivoRuta,
                d.Estado,
                FechaSubida = d.FechaSubida.ToString("dd/MM/yyyy HH:mm")
            })
            .ToList();

        return Json(new { success = true, documentos = docs });
    }

    // ===============================================
    // ACCIONES DE MEJORA Y SEGUIMIENTO (ESTUDIANTE)
    // ===============================================

    [HttpGet]
    [RequierePermiso(Permisos.UsarPanelEstudiante)]
    public IActionResult ObtenerAccionesMejoraEstudiante(string? materia)
    {
        _context.AsegurarEsquemaAccionesMejora();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "Sesión no válida." });
        }

        var query = _context.AccionesMejora
            .Where(a => a.IdEstudiante == userId.Value && a.Activo);

        if (!string.IsNullOrWhiteSpace(materia) && materia.ToLower() != "todas")
        {
            query = query.Where(a => a.Materia == materia);
        }

        var mejoras = query
            .OrderByDescending(a => a.FechaRegistro)
            .Select(a => new
            {
                idMejora = a.IdMejora,
                idEstudiante = a.IdEstudiante,
                nombreEstudiante = a.NombreEstudiante,
                materia = a.Materia,
                grado = a.Grado,
                periodo = a.Periodo,
                aspectoMejorar = a.AspectoMejorar,
                compromisoEstudiante = a.CompromisoEstudiante,
                fechaRegistro = a.FechaRegistro.ToString("dd/MM/yyyy"),
                fechaCompromiso = a.FechaCompromiso.HasValue ? a.FechaCompromiso.Value.ToString("dd/MM/yyyy") : null,
                estadoSeguimiento = a.EstadoSeguimiento,
                observacionSeguimiento = a.ObservacionSeguimiento,
                fechaSeguimiento = a.FechaSeguimiento.HasValue ? a.FechaSeguimiento.Value.ToString("dd/MM/yyyy HH:mm") : null,
                respuestaEstudiante = a.RespuestaEstudiante,
                fechaRespuestaEstudiante = a.FechaRespuestaEstudiante.HasValue ? a.FechaRespuestaEstudiante.Value.ToString("dd/MM/yyyy HH:mm") : null,
                docente = a.Docente
            })
            .ToList();

        return Json(new { success = true, mejoras = mejoras });
    }

    [HttpPost]
    [RequierePermiso(Permisos.ParticiparComoEstudiante)]
    public IActionResult RegistrarAvanceMejora([FromBody] AvanceEstudianteMejoraDto dto)
    {
        _context.AsegurarEsquemaAccionesMejora();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "Sesión no válida." });
        }

        if (dto == null || dto.IdMejora <= 0 || string.IsNullOrWhiteSpace(dto.RespuestaEstudiante))
        {
            return Json(new { success = false, message = "Por favor escribe tu compromiso / avance de mejora." });
        }

        var mejora = _context.AccionesMejora.Find(dto.IdMejora);
        if (mejora == null || !mejora.Activo)
        {
            return Json(new { success = false, message = "Acción de mejora no encontrada." });
        }

        if (mejora.IdEstudiante != userId.Value)
        {
            return Json(new { success = false, message = "No tienes permiso para modificar este registro." });
        }

        mejora.RespuestaEstudiante = dto.RespuestaEstudiante.Trim();
        mejora.FechaRespuestaEstudiante = DateTime.Now;

        _context.SaveChanges();

        return Json(new { success = true, message = "¡Tu avance y compromiso han sido registrados con éxito!" });
    }
}