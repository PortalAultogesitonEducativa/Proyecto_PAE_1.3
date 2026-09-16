using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using Microsoft.EntityFrameworkCore;

public class EstudianteController : Controller
{
    private readonly ApplicationDbContext _context;
    public EstudianteController(ApplicationDbContext context) { _context = context; }
    // Solo debe existir una versión de este método en el archivo
    public IActionResult MisNotas(int? idEstudiante)
    {
        int userId;
        // Intentamos obtener el ID de la sesión del usuario actual
        var userIdStr = HttpContext.Session.GetString("UserIdStr");

        if (idEstudiante.HasValue)
        {
            // CASO ACUDIENTE: Se usa el ID del hijo pasado por la URL
            userId = idEstudiante.Value;
        }
        else if (int.TryParse(userIdStr, out int sessionUserId))
        {
            // CASO ESTUDIANTE: Se usa el ID de quien inició sesión
            userId = sessionUserId;
        }
        else
        {
            // Si no hay ID ni sesión, redirigir al Login
            return RedirectToAction("Index", "Login");
        }

        // Consulta a la base de datos filtrando por el ID determinado
        var notas = _context.Calificaciones
                            .Where(n => n.ID_Estudiante == userId)
                            .ToList();

        // RF 5.2: Cálculo automático del promedio para la vista
        double promedio = notas.Any() ? notas.Average(n => (double)n.Nota) : 0.0;
        ViewBag.PromedioGeneral = promedio;

        return View(notas);
    }

    // ===============================================
    // INSCRIPCIÓN A CURSOS EXTRACURRICULARES (ESTUDIANTE)
    // ===============================================

    [HttpPost]
    public IActionResult InscribirseCursoExtra(int idCurso)
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

        // Verificar si ya está inscrito
        var inscripcionExistente = _context.ExtraInscripciones
            .FirstOrDefault(i => i.IdEstudiante == userId.Value && i.IdExtraCurso == idCurso && i.Estado == "Inscrito");

        if (inscripcionExistente != null)
        {
            return Json(new { success = false, message = "Ya te encuentras inscrito en este curso extracurricular." });
        }

        // Verificar grado del estudiante si el curso tiene rango definido
        var usuarioActual = _context.Usuarios.Find(userId.Value);
        var estudianteObj = _context.ESTUDIANTE
            .FirstOrDefault(e => e.id_usuario == userId.Value || (usuarioActual != null && e.email == usuarioActual.CORREO_ELECTRONICO));

        int? gradoEstudiante = null;
        if (estudianteObj != null)
        {
            var matricula = _context.Matriculas
                .FirstOrDefault(m => m.id_estudiante == estudianteObj.id_estudiante && m.estado != null && (m.estado.ToLower() == "activa" || m.estado.ToLower() == "activo"));
            if (matricula != null)
            {
                gradoEstudiante = matricula.id_curso;
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

        // Descontar cupo
        curso.CuposDisponibles -= 1;
        if (curso.CuposDisponibles < 0) curso.CuposDisponibles = 0;

        // Registrar inscripción
        var nuevaInscripcion = new ExtraInscripcion
        {
            IdEstudiante = userId.Value,
            IdExtraCurso = idCurso,
            FechaInscripcion = DateTime.Now,
            Estado = "Inscrito"
        };

        _context.ExtraInscripciones.Add(nuevaInscripcion);
        _context.SaveChanges();

        return Json(new { 
            success = true, 
            message = $"¡Te has inscrito exitosamente a '{curso.Nombre}'! Cupos restantes: {curso.CuposDisponibles}.",
            cuposDisponibles = curso.CuposDisponibles
        });
    }

    [HttpPost]
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

        if (inscripcion.Estado == "Cancelado")
        {
            return Json(new { success = false, message = "Esta inscripción ya fue cancelada previamente." });
        }

        inscripcion.Estado = "Cancelado";
        inscripcion.FechaCancelacion = DateTime.Now;
        inscripcion.MotivoCancelacion = "Cancelado voluntariamente por el estudiante";

        // Devolver el cupo al curso
        var curso = _context.ExtraCursos.Find(inscripcion.IdExtraCurso);
        if (curso != null)
        {
            if (curso.CuposDisponibles < curso.CuposTotales)
            {
                curso.CuposDisponibles += 1;
            }
        }

        _context.SaveChanges();

        return Json(new { 
            success = true, 
            message = "Has cancelado tu inscripción. El cupo ha sido liberado exitosamente." 
        });
    }

    // ===============================================
    // MÓDULO: ACTIVIDADES Y ENTREGAS (ESTUDIANTE)
    // ===============================================

    /// <summary>
    /// Retorna las actividades activas con el estado de entrega del estudiante (JSON).
    /// </summary>
    [HttpGet]
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
                vencida = vencida
            };
        }).ToList();

        return Json(new { success = true, actividades = resultado });
    }

    /// <summary>
    /// Recibe la entrega de un estudiante para una actividad (archivo + comentario).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubirEntregaActividad(int idActividad, string? comentario, IFormFile? archivo)
    {
        _context.AsegurarEsquemaActividades();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Json(new { success = false, message = "Sesión inválida. Por favor inicia sesión nuevamente." });

        var actividad = _context.Actividades.Find(idActividad);
        if (actividad == null || !actividad.Activo)
            return Json(new { success = false, message = "La actividad no existe o está inactiva." });

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

    // ===============================================
    // SUBIDA DE DOCUMENTOS PARA CURSOS EXTRACURRICULARES
    // ===============================================

    [HttpPost]
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
}