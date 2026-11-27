using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;

namespace ProyectoPAE.Controllers
{
    [ApiController]
    [Route("api/movil")]
    public class ApiMovilController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiMovilController(ApplicationDbContext context)
        {
            _context = context;
        }

        public record LoginRequest(string UsuarioOCorreo, string Password);
        public record RecuperarRequest(string UsuarioOCorreo);
        public record CambiarPasswordRequest(int IdUsuario, string NuevaPassword, string Token);

        public class LoginResultadoDto
        {
            public int Codigo { get; set; }
            public string Mensaje { get; set; } = string.Empty;
            public int? IdUsuario { get; set; }
        }

        public class RecuperacionResultadoDto
        {
            public string Codigo { get; set; } = string.Empty;
            public string Mensaje { get; set; } = string.Empty;
            public string? Token { get; set; }
        }

        public class CambioPasswordResultadoDto
        {
            public string Codigo { get; set; } = string.Empty;
            public string Mensaje { get; set; } = string.Empty;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            bool dbOk = await _context.Database.CanConnectAsync();
            if (!dbOk)
                return StatusCode(503, new { ok = false, mensaje = "No hay conexión con la base de datos." });

            return Ok(new { ok = true, servicio = "PAE API Móvil", fecha = DateTime.UtcNow });
        }

        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UsuarioOCorreo) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Ok(new LoginResultadoDto
                {
                    Codigo = 2,
                    Mensaje = "Usuario o contraseña incorrectos."
                });
            }

            string? nombreUsuarioReal = await _context.Usuarios
                .Where(u => u.NOMBRE_USUARIO == request.UsuarioOCorreo || u.CORREO_ELECTRONICO == request.UsuarioOCorreo)
                .Select(u => u.NOMBRE_USUARIO)
                .FirstOrDefaultAsync();

            if (nombreUsuarioReal == null)
            {
                return Ok(new LoginResultadoDto
                {
                    Codigo = 2,
                    Mensaje = "Usuario o contraseña incorrectos."
                });
            }

            var resultado = await _context.Database
                .SqlQuery<LoginResultadoDto>($"EXEC sp_LoginUsuario @NombreUsuario={nombreUsuarioReal}, @PasswordHash={request.Password}")
                .ToListAsync();

            var respuesta = resultado.FirstOrDefault() ?? new LoginResultadoDto
            {
                Codigo = 2,
                Mensaje = "No se pudo validar el usuario."
            };

            return Ok(respuesta);
        }

        [HttpGet("auth/usuario/{idUsuario:int}")]
        public async Task<IActionResult> ObtenerUsuario(int idUsuario)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.ID_Usuario == idUsuario)
                .Select(u => new
                {
                    u.ID_Usuario,
                    u.NOMBRE_USUARIO,
                    CONTRASEÑA = "",
                    u.NOMBRES,
                    u.APELLIDOS,
                    u.CORREO_ELECTRONICO,
                    u.FECHA_CREACION,
                    u.ACTIVO,
                    u.ROL,
                    u.TELEFONO,
                    u.DIRECCION,
                    u.FOTO_URL,
                    u.TIPO_DOCUMENTO,
                    u.NUM_DOCUMENTO,
                    u.FECHA_NACIMIENTO,
                    u.GENERO,
                    u.LUGAR_NACIMIENTO,
                    u.CIUDAD,
                    u.BARRIO,
                    u.AREA_ASIGNATURA,
                    u.COLEGIO_PROCEDENCIA,
                    u.TIPO_SANGRE,
                    u.EPS,
                    u.ALERGIAS,
                    u.CONDICIONES_MEDICAS,
                    u.OBSERVACIONES,
                    u.FORZAR_CAMBIO_CLAVE
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
                return NotFound();

            return Ok(usuario);
        }

        [HttpPost("auth/recuperar")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] RecuperarRequest request)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.NOMBRE_USUARIO == request.UsuarioOCorreo || u.CORREO_ELECTRONICO == request.UsuarioOCorreo)
                .Select(u => new { u.ID_Usuario, u.CORREO_ELECTRONICO, u.NOMBRES })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return Ok(new
                {
                    codigo = "Error",
                    mensaje = "Usuario o correo no encontrado.",
                    token = (string?)null,
                    idUsuario = (int?)null,
                    correo = (string?)null,
                    nombre = (string?)null
                });
            }

            var resultado = await _context.Database
                .SqlQuery<RecuperacionResultadoDto>($"EXEC sp_RecuperarPassword @IdUsuario={usuario.ID_Usuario}")
                .ToListAsync();

            var r = resultado.FirstOrDefault() ?? new RecuperacionResultadoDto
            {
                Codigo = "Error",
                Mensaje = "No se pudo generar el token."
            };

            return Ok(new
            {
                codigo = r.Codigo,
                mensaje = r.Mensaje,
                token = r.Token,
                idUsuario = usuario.ID_Usuario,
                correo = usuario.CORREO_ELECTRONICO,
                nombre = usuario.NOMBRES
            });
        }

        [HttpPost("auth/cambiar-password")]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
        {
            var resultado = await _context.Database
                .SqlQuery<CambioPasswordResultadoDto>($"EXEC sp_CambiarPassword @IdUsuario={request.IdUsuario}, @NuevoHash={request.NuevaPassword}, @Token={request.Token}")
                .ToListAsync();

            var r = resultado.FirstOrDefault() ?? new CambioPasswordResultadoDto
            {
                Codigo = "Error",
                Mensaje = "No se pudo cambiar la contraseña."
            };

            return Ok(r);
        }

        [HttpGet("notas/{idUsuarioEstudiante:int}")]
        public async Task<IActionResult> ObtenerNotas(int idUsuarioEstudiante)
        {
            var notas = await _context.Calificaciones
                .AsNoTracking()
                .Where(c => c.ID_Estudiante == idUsuarioEstudiante)
                .OrderByDescending(c => c.FechaRegistro)
                .Select(c => new
                {
                    c.ID_Calificacion,
                    c.ID_Estudiante,
                    c.Materia,
                    c.Nota,
                    c.Periodo,
                    c.FechaRegistro
                })
                .ToListAsync();

            return Ok(notas);
        }

        [HttpGet("notas/{idUsuarioEstudiante:int}/promedio")]
        public async Task<IActionResult> ObtenerPromedio(int idUsuarioEstudiante)
        {
            var notas = await _context.Calificaciones
                .AsNoTracking()
                .Where(c => c.ID_Estudiante == idUsuarioEstudiante)
                .Select(c => new { c.Materia, c.Periodo, c.Nota })
                .ToListAsync();

            if (notas.Count == 0)
                return Ok(new { promedio = (decimal?)null });

            int periodoActual = notas.Max(n => n.Periodo);
            var promediosPorMateria = notas
                .Where(n => n.Periodo == periodoActual)
                .GroupBy(n => n.Materia)
                .Select(g => g.Average(n => n.Nota))
                .ToList();

            decimal promedio = Math.Round(promediosPorMateria.Average(), 2);
            return Ok(new { promedio = (decimal?)promedio });
        }

        [HttpGet("asistencia/{idUsuarioEstudiante:int}")]
        public async Task<IActionResult> ObtenerAsistencia(int idUsuarioEstudiante)
        {
            int? idEstudiante = await ResolverIdEstudianteAsync(idUsuarioEstudiante);
            if (idEstudiante == null)
                return Ok(Array.Empty<object>());

            var lista = await _context.ASISTENCIAS
                .AsNoTracking()
                .Where(a => a.id_estudiante == idEstudiante)
                .OrderByDescending(a => a.fecha)
                .Select(a => new
                {
                    a.id_asistencia,
                    a.id_estudiante,
                    a.fecha,
                    estado = a.estado ?? string.Empty,
                    a.justificacion
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("asistencia/{idUsuarioEstudiante:int}/fallas")]
        public async Task<IActionResult> ContarFallas(int idUsuarioEstudiante)
        {
            int? idEstudiante = await ResolverIdEstudianteAsync(idUsuarioEstudiante);
            if (idEstudiante == null)
                return Ok(new { fallas = 0 });

            int fallas = await _context.ASISTENCIAS
                .AsNoTracking()
                .Where(a => a.id_estudiante == idEstudiante && a.estado == "Ausente")
                .CountAsync();

            return Ok(new { fallas });
        }

        [HttpGet("citaciones/{idUsuarioEstudiante:int}")]
        public async Task<IActionResult> ObtenerCitaciones(int idUsuarioEstudiante)
        {
            int? idEstudiante = await ResolverIdEstudianteAsync(idUsuarioEstudiante);
            if (idEstudiante == null)
                return Ok(Array.Empty<object>());

            var lista = await _context.CITACIONES
                .AsNoTracking()
                .Where(c => c.id_estudiante == idEstudiante)
                .OrderByDescending(c => c.fecha)
                .Select(c => new
                {
                    c.id_citacion,
                    c.id_estudiante,
                    c.remitente,
                    c.asunto,
                    c.mensaje,
                    c.fecha
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("acudiente/{idUsuarioAcudiente:int}/hijos")]
        public async Task<IActionResult> ObtenerHijos(int idUsuarioAcudiente)
        {
            var padre = await _context.PADRE_TUTOR
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id_usuario == idUsuarioAcudiente);

            if (padre == null)
                return Ok(Array.Empty<object>());

            var idsEstudiantes = await _context.ESTUDIANTE_PADRE
                .AsNoTracking()
                .Where(ep => ep.ID_PADRE == padre.id_padre)
                .Select(ep => ep.ID_ESTUDIANTE)
                .ToListAsync();

            if (idsEstudiantes.Count == 0)
                return Ok(Array.Empty<object>());

            var hijos = await _context.ESTUDIANTE
                .AsNoTracking()
                .Where(e => idsEstudiantes.Contains(e.id_estudiante) && e.id_usuario != null)
                .Select(e => new
                {
                    IdUsuario = e.id_usuario!.Value,
                    NombreCompleto = e.nombre + " " + e.apellido
                })
                .ToListAsync();

            return Ok(hijos);
        }

        [HttpGet("horario/{idUsuarioEstudiante:int}")]
        public async Task<IActionResult> ObtenerHorario(int idUsuarioEstudiante)
        {
            int? idEstudiante = await ResolverIdEstudianteAsync(idUsuarioEstudiante);
            if (idEstudiante == null)
                return Ok(Array.Empty<object>());

            var idsCursos = await _context.Matriculas
                .AsNoTracking()
                .Where(m => m.id_estudiante == idEstudiante && m.estado == "Activa" && m.id_curso != null)
                .Select(m => m.id_curso!.Value)
                .ToListAsync();

            if (idsCursos.Count == 0)
                return Ok(Array.Empty<object>());

            var resultado = await (
                from h in _context.HORARIOS.AsNoTracking()
                where idsCursos.Contains(h.id_curso)
                join c in _context.Cursos.AsNoTracking() on h.id_curso equals c.id_curso
                join a in _context.AULA.AsNoTracking() on h.id_aula equals a.id_aula into aulaJoin
                from a in aulaJoin.DefaultIfEmpty()
                join p in _context.PROFESOR.AsNoTracking() on h.id_profesor equals p.id_profesor into profJoin
                from p in profJoin.DefaultIfEmpty()
                orderby h.dia, h.hora_inicio
                select new
                {
                    Dia = h.dia,
                    HoraInicio = h.hora_inicio,
                    HoraFin = h.hora_fin,
                    NombreCurso = c.nombre_curso,
                    Aula = a != null ? a.codigo_aula : null,
                    Profesor = p != null ? (p.nombre + " " + p.apellido) : null
                }
            ).ToListAsync();

            return Ok(resultado);
        }

        [HttpGet("actividades/{idUsuarioEstudiante:int}")]
        public async Task<IActionResult> ObtenerActividades(int idUsuarioEstudiante)
        {
            int? idEstudiante = await ResolverIdEstudianteAsync(idUsuarioEstudiante);
            if (idEstudiante == null)
                return Ok(Array.Empty<object>());

            var idsCursos = await _context.Matriculas
                .AsNoTracking()
                .Where(m => m.id_estudiante == idEstudiante && m.estado == "Activa" && m.id_curso != null)
                .Select(m => m.id_curso!.Value)
                .ToListAsync();

            if (idsCursos.Count == 0)
                return Ok(Array.Empty<object>());

            var actividades = await _context.Documentos
                .AsNoTracking()
                .Where(d => d.IdCurso != null
                            && idsCursos.Contains(d.IdCurso.Value)
                            && d.EstadoDocumento == "publicado"
                            && (d.TipoDocumento == "tarea" || d.TipoDocumento == "proyecto"))
                .OrderByDescending(d => d.FechaSubida)
                .Select(d => new
                {
                    id_documento = d.IdDocumento,
                    titulo = d.Titulo,
                    descripcion = d.Descripcion,
                    tipo_documento = d.TipoDocumento,
                    estado_documento = d.EstadoDocumento,
                    fecha_subida = d.FechaSubida,
                    id_curso = d.IdCurso
                })
                .ToListAsync();

            return Ok(actividades);
        }

        private async Task<int?> ResolverIdEstudianteAsync(int idUsuarioEstudiante)
        {
            return await _context.ESTUDIANTE
                .AsNoTracking()
                .Where(e => e.id_usuario == idUsuarioEstudiante)
                .Select(e => (int?)e.id_estudiante)
                .FirstOrDefaultAsync();
        }
    }
}
