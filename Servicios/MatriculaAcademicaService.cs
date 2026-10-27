using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;

namespace ProyectoPAE.Servicios
{
    public class MatriculaAcademicaService
    {
        private readonly ApplicationDbContext _context;

        public MatriculaAcademicaService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el grado numérico (1-11) a partir del nombre del curso (ej: "601 M" -> 6, "1002 T" -> 10)
        /// o del identificador de grado.
        /// </summary>
        public static int ExtraerGrado(string? nombreCurso, int fallback = 6)
        {
            if (string.IsNullOrWhiteSpace(nombreCurso)) return fallback;
            string numStr = "";
            foreach (char c in nombreCurso.Trim())
            {
                if (char.IsDigit(c)) numStr += c;
                else break;
            }
            if (int.TryParse(numStr, out int num))
            {
                if (num >= 600 && num < 700) return 6;
                if (num >= 700 && num < 800) return 7;
                if (num >= 800 && num < 900) return 8;
                if (num >= 900 && num < 1000) return 9;
                if (num >= 1000 && num < 1100) return 10;
                if (num >= 1100 && num < 1200) return 11;
                if (num >= 1 && num <= 11) return num;
            }
            return fallback;
        }

        /// <summary>
        /// Obtiene la lista completa de grados aprobados por el estudiante
        /// (tanto de matrículas institucionales con estado académico 'Aprobado'
        /// como de certificados externos con estado 'Verificado').
        /// </summary>
        public async Task<List<int>> ObtenerGradosAprobadosAsync(int idEstudiante)
        {
            var gradosAprobados = new HashSet<int>();

            // 1. Grados de matrículas institucionales aprobadas
            var matsAprobadas = await _context.Matriculas
                .Where(m => m.id_estudiante == idEstudiante && m.estado_academico == "Aprobado")
                .Include(m => m.Curso)
                .ToListAsync();

            foreach (var mat in matsAprobadas)
            {
                int gr = mat.grado_solicitado ?? (mat.Curso != null ? ExtraerGrado(mat.Curso.nombre_curso) : (mat.id_curso ?? 0));
                if (gr >= 1 && gr <= 11)
                {
                    gradosAprobados.Add(gr);
                }
            }

            // 2. Grados de certificados externos verificados
            var certsVerificados = await _context.CertificadosEstudioExterno
                .Where(c => c.IdEstudiante == idEstudiante && c.Activo && c.EstadoVerificacion == "Verificado")
                .ToListAsync();

            foreach (var cert in certsVerificados)
            {
                if (cert.GradoAprobado >= 1 && cert.GradoAprobado <= 11)
                {
                    gradosAprobados.Add(cert.GradoAprobado);
                }
            }

            return gradosAprobados.OrderBy(g => g).ToList();
        }

        /// <summary>
        /// Obtiene el historial completo académico y administrativo de un estudiante.
        /// </summary>
        public async Task<HistorialAcademicoEstudianteViewModel> ObtenerHistorialAcademicoAsync(int idEstudiante)
        {
            var estudiante = await _context.ESTUDIANTE.FindAsync(idEstudiante);
            var historialVm = new HistorialAcademicoEstudianteViewModel
            {
                IdEstudiante = idEstudiante,
                NombreCompleto = estudiante != null ? $"{estudiante.nombre} {estudiante.apellido}".Trim() : "Estudiante",
                CodigoEstudiante = estudiante?.codigo_estudiante ?? "",
                Documento = estudiante?.codigo_estudiante ?? ""
            };

            var items = new List<HistorialItemViewModel>();

            // 1. Matrículas institucionales
            var matriculas = await _context.Matriculas
                .Where(m => m.id_estudiante == idEstudiante)
                .Include(m => m.Curso)
                .OrderByDescending(m => m.ano)
                .ThenByDescending(m => m.fecha_matricula)
                .ToListAsync();

            foreach (var m in matriculas)
            {
                int grado = m.grado_solicitado ?? (m.Curso != null ? ExtraerGrado(m.Curso.nombre_curso) : (m.id_curso ?? 0));
                items.Add(new HistorialItemViewModel
                {
                    IdRegistro = m.id_matricula,
                    Tipo = "Matrícula Institucional",
                    Grado = grado,
                    CursoOInstitucion = m.Curso != null ? m.Curso.nombre_curso : $"Grado {grado}° (Curso no asignado)",
                    AnioLectivo = m.ano,
                    FechaRegistro = m.fecha_matricula,
                    EstadoMatricula = m.estado ?? "Registrada",
                    ResultadoAcademico = m.estado_academico ?? "Pendiente de resultado",
                    EstadoDocumental = m.estado_documental ?? "Pendiente de revisión",
                    Observaciones = m.observaciones,
                    EsExterno = false,
                    EstadoVerificacion = null,
                    RevisadoPor = m.revisado_por,
                    FechaRevision = m.fecha_revision_documental,
                    ArchivoRuta = null
                });
            }

            // 2. Certificados externos registrados
            var certificados = await _context.CertificadosEstudioExterno
                .Where(c => c.IdEstudiante == idEstudiante && c.Activo)
                .OrderByDescending(c => c.AnoLectivo)
                .ThenByDescending(c => c.GradoAprobado)
                .ToListAsync();

            foreach (var c in certificados)
            {
                items.Add(new HistorialItemViewModel
                {
                    IdRegistro = c.IdCertificadoExterno,
                    Tipo = "Certificado Externo",
                    Grado = c.GradoAprobado,
                    CursoOInstitucion = c.InstitucionOrigen,
                    AnioLectivo = c.AnoLectivo,
                    FechaRegistro = c.FechaRegistro,
                    EstadoMatricula = "N/A (Externo)",
                    ResultadoAcademico = c.EstadoVerificacion == "Verificado" ? "Aprobado" : "Pendiente de verificación",
                    EstadoDocumental = "Acreditación Externa",
                    Observaciones = c.Observaciones,
                    EsExterno = true,
                    EstadoVerificacion = c.EstadoVerificacion,
                    RevisadoPor = c.RevisadoPor,
                    FechaRevision = c.FechaVerificacion,
                    ArchivoRuta = c.ArchivoRuta
                });
            }

            historialVm.Registros = items
                .OrderByDescending(i => i.AnioLectivo)
                .ThenByDescending(i => i.FechaRegistro)
                .ToList();

            var gradosAprobados = await ObtenerGradosAprobadosAsync(idEstudiante);
            if (gradosAprobados.Any())
            {
                historialVm.UltimoGradoAprobado = gradosAprobados.Max();
                historialVm.SiguienteGradoHabilitado = Math.Min(11, (historialVm.UltimoGradoAprobado ?? 5) + 1);
                historialVm.EstadoAcademicoGeneral = $"Último grado aprobado: {historialVm.UltimoGradoAprobado}°. Habilitado para solicitar Grado {historialVm.SiguienteGradoHabilitado}°.";
            }
            else
            {
                historialVm.UltimoGradoAprobado = null;
                historialVm.SiguienteGradoHabilitado = 6; // Por defecto inicio bachillerato si no hay antecedentes
                historialVm.EstadoAcademicoGeneral = "Sin historial previo registrado. Debe acreditar certificados anteriores en secretaría.";
            }

            return historialVm;
        }

        /// <summary>
        /// Valida el avance de grado antes de registrar o modificar una matrícula.
        /// Reglas:
        /// 1. No duplicar matrícula para el mismo estudiante, grado y año lectivo.
        /// 2. Si ya aprobó el grado, no puede matricularse nuevamente en él.
        /// 3. Si reprobó el grado anterior, no puede avanzar al siguiente.
        /// 4. Si el resultado académico del grado anterior está pendiente, no autoriza el avance.
        /// 5. Para avanzar al grado G (G > 1), debe tener aprobado G-1 (vía institucional o certificado externo verificado).
        /// </summary>
        public async Task<(bool Valido, string Mensaje)> ValidarAvanceGradoEstudianteAsync(
            int idEstudiante, 
            int gradoSolicitado, 
            int anioLectivo, 
            int? idMatriculaActual = null,
            bool permitirExcepcionRepeticion = false)
        {
            // 1. Verificación de duplicados para el mismo año y grado
            var duplicada = await _context.Matriculas
                .Where(m => m.id_estudiante == idEstudiante && m.ano == anioLectivo)
                .Where(m => idMatriculaActual == null || m.id_matricula != idMatriculaActual)
                .FirstOrDefaultAsync();

            if (duplicada != null)
            {
                return (false, $"El estudiante ya tiene una matrícula registrada para el año lectivo {anioLectivo} (ID #{duplicada.id_matricula}). No se permiten matrículas duplicadas en el mismo año lectivo.");
            }

            // 2. Grados ya aprobados
            var gradosAprobados = await ObtenerGradosAprobadosAsync(idEstudiante);

            if (gradosAprobados.Contains(gradoSolicitado) && !permitirExcepcionRepeticion)
            {
                return (false, $"El estudiante ya aprobó satisfactoriamente el Grado {gradoSolicitado}°. No debe cursarlo nuevamente salvo excepción institucional debidamente autorizada.");
            }

            // 3. Revisar el resultado de la última matrícula institucional cursada
            var ultimaMatricula = await _context.Matriculas
                .Where(m => m.id_estudiante == idEstudiante)
                .Where(m => idMatriculaActual == null || m.id_matricula != idMatriculaActual)
                .OrderByDescending(m => m.ano)
                .ThenByDescending(m => m.fecha_matricula)
                .FirstOrDefaultAsync();

            if (ultimaMatricula != null)
            {
                int gradoUltima = ultimaMatricula.grado_solicitado ?? (ultimaMatricula.id_curso ?? 0);

                // Si la última matrícula está pendiente de resultado académico
                if (ultimaMatricula.estado_academico == "Pendiente de resultado" && gradoSolicitado > gradoUltima)
                {
                    return (false, $"El resultado académico de la matrícula {ultimaMatricula.ano} (Grado {gradoUltima}°) se encuentra 'Pendiente de resultado'. El sistema no autoriza avanzar automáticamente a un grado superior hasta consolidar las notas.");
                }

                // Si reprobó el grado anterior
                if (ultimaMatricula.estado_academico == "Reprobado" && gradoSolicitado > gradoUltima)
                {
                    return (false, $"El estudiante reprobó el Grado {gradoUltima}° en el año {ultimaMatricula.ano}. No puede avanzar al Grado {gradoSolicitado}°; debe repetir el grado reprobado.");
                }
            }

            // 4. Validación de prerrequisito (grado anterior aprobado)
            // En Colombia: Grados 1 a 11. Para Grado 6 (bachillerato), se requiere haber aprobado Grado 5.
            if (gradoSolicitado > 1)
            {
                int gradoPrerrequisito = gradoSolicitado - 1;
                bool prerrequisitoAprobado = gradosAprobados.Contains(gradoPrerrequisito);

                // Si no está aprobado en registros internos ni certificados verificados,
                // verificamos si hay algún certificado externo en estado pendiente
                if (!prerrequisitoAprobado)
                {
                    var certPendiente = await _context.CertificadosEstudioExterno
                        .Where(c => c.IdEstudiante == idEstudiante && c.GradoAprobado == gradoPrerrequisito && c.Activo && c.EstadoVerificacion == "Pendiente de verificación")
                        .FirstOrDefaultAsync();

                    if (certPendiente != null)
                    {
                        return (false, $"El certificado del Grado {gradoPrerrequisito}° procedente de '{certPendiente.InstitucionOrigen}' se encuentra 'Pendiente de verificación'. La secretaría debe revisar y verificar el documento antes de autorizar la matrícula en Grado {gradoSolicitado}°.");
                    }

                    // Caso especial: estudiantes antiguos existentes antes de la digitalización que ya tienen matrículas históricas
                    bool tieneMatriculaPreviaEnColegio = await _context.Matriculas
                        .AnyAsync(m => m.id_estudiante == idEstudiante && (idMatriculaActual == null || m.id_matricula != idMatriculaActual));

                    if (!tieneMatriculaPreviaEnColegio && gradoSolicitado >= 6)
                    {
                        return (false, $"El estudiante no cuenta con historial de primaria en el sistema ni certificados externos verificados del Grado {gradoPrerrequisito}°. La secretaría debe registrar y verificar los certificados de estudio anteriores (ej: 1° a 5°).");
                    }
                    else if (tieneMatriculaPreviaEnColegio && !gradosAprobados.Contains(gradoPrerrequisito))
                    {
                        return (false, $"No se encontró registro de aprobación para el Grado {gradoPrerrequisito}°. Verifique el estado académico o registre el certificado correspondiente.");
                    }
                }
            }

            return (true, "Validación académica satisfactoria.");
        }

        /// <summary>
        /// Asigna definitivamente a un estudiante a un curso.
        /// Solo procede si la matrícula cumple validaciones documentales, académicas y administrativas.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> AsignarCursoDefinitivoAsync(int idMatricula, int idCurso, string usuarioResponsable)
        {
            var matricula = await _context.Matriculas.FindAsync(idMatricula);
            if (matricula == null)
                return (false, "No se encontró la matrícula especificada.");

            var curso = await _context.Cursos.FindAsync(idCurso);
            if (curso == null)
                return (false, "No se encontró el curso seleccionado.");

            int gradoCurso = ExtraerGrado(curso.nombre_curso);
            int gradoSolicitado = matricula.grado_solicitado ?? gradoCurso;

            if (gradoCurso != gradoSolicitado)
            {
                return (false, $"El curso seleccionado ({curso.nombre_curso}) corresponde a Grado {gradoCurso}°, pero la matrícula solicita Grado {gradoSolicitado}°.");
            }

            // REQUISITO CRÍTICO: La revisión documental debe estar Aprobada
            if (matricula.estado_documental != "Aprobada")
            {
                return (false, $"No se puede asignar curso definitivo: La revisión documental se encuentra en '{matricula.estado_documental}'. La secretaría debe verificar y aprobar los documentos primero.");
            }

            // REQUISITO CRÍTICO: El estado de la matrícula no puede ser Rechazada ni Cancelada
            if (matricula.estado == "Cancelada" || matricula.estado == "Rechazada")
            {
                return (false, $"No se puede asignar curso a una matrícula con estado '{matricula.estado}'.");
            }

            // Actualizar MATRICULA
            matricula.id_curso = idCurso;
            matricula.estado = "Activa";
            matricula.revisado_por = usuarioResponsable;

            // Actualizar asignación definitiva en ESTUDIANTE
            var estudiante = await _context.ESTUDIANTE.FindAsync(matricula.id_estudiante);
            if (estudiante != null)
            {
                estudiante.id_curso = idCurso;
                estudiante.curso_asignado = curso.nombre_curso;
            }

            await _context.SaveChangesAsync();
            return (true, $"Estudiante asignado exitosamente al curso {curso.nombre_curso} con matrícula Activa.");
        }

        /// <summary>
        /// Realiza la revisión documental y administrativa de una matrícula.
        /// La actualización documental NUNCA borra ni modifica el resultado académico.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> RevisarDocumentosAsync(
            int idMatricula, 
            string nuevoEstadoDocumental, 
            string? documentosPendientes, 
            string? observaciones, 
            string usuarioResponsable)
        {
            var matricula = await _context.Matriculas.FindAsync(idMatricula);
            if (matricula == null)
                return (false, "Matrícula no encontrada.");

            matricula.estado_documental = nuevoEstadoDocumental;
            matricula.documentos_pendientes = documentosPendientes;
            matricula.fecha_revision_documental = DateTime.Now;
            matricula.revisado_por = usuarioResponsable;

            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                matricula.observaciones = string.IsNullOrWhiteSpace(matricula.observaciones) 
                    ? $"[{DateTime.Now:yyyy-MM-dd}] {observaciones}" 
                    : $"{matricula.observaciones}\n[{DateTime.Now:yyyy-MM-dd}] {observaciones}";
            }

            // Si se rechaza administrativamente y no estaba ya finalizada
            if (nuevoEstadoDocumental == "Rechazada" && matricula.estado != "Finalizada")
            {
                matricula.estado = "Rechazada";
                // Desasignar curso preventivamente si estaba rechazado
                if (matricula.id_curso != null)
                {
                    var estudiante = await _context.ESTUDIANTE.FindAsync(matricula.id_estudiante);
                    if (estudiante != null && estudiante.id_curso == matricula.id_curso)
                    {
                        estudiante.id_curso = null;
                        estudiante.curso_asignado = null;
                    }
                    matricula.id_curso = null;
                }
            }
            else if (nuevoEstadoDocumental == "Aprobada" && (matricula.estado == "Pendiente" || matricula.estado == "Rechazada"))
            {
                matricula.estado = "Registrada";
            }

            await _context.SaveChangesAsync();
            return (true, $"Revisión documental actualizada a '{nuevoEstadoDocumental}'.");
        }

        /// <summary>
        /// Actualiza el resultado académico de una matrícula (Aprobado, Reprobado, Pendiente de resultado).
        /// Independiente de la revisión documental.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> ActualizarEstadoAcademicoAsync(
            int idMatricula, 
            string nuevoEstadoAcademico, 
            string? observaciones, 
            string usuarioResponsable)
        {
            var matricula = await _context.Matriculas.FindAsync(idMatricula);
            if (matricula == null)
                return (false, "Matrícula no encontrada.");

            matricula.estado_academico = nuevoEstadoAcademico;
            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                matricula.observaciones = string.IsNullOrWhiteSpace(matricula.observaciones)
                    ? $"[Académico {DateTime.Now:yyyy-MM-dd}] {observaciones}"
                    : $"{matricula.observaciones}\n[Académico {DateTime.Now:yyyy-MM-dd}] {observaciones}";
            }

            await _context.SaveChangesAsync();
            return (true, $"Resultado académico actualizado a '{nuevoEstadoAcademico}'.");
        }

        /// <summary>
        /// Registra un antecedente académico externo certificado por secretaría.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> RegistrarCertificadoExternoAsync(
            CertificadoEstudioExterno certificado, 
            string usuarioResponsable)
        {
            if (certificado.GradoAprobado < 1 || certificado.GradoAprobado > 11)
                return (false, "El grado aprobado debe estar entre 1° y 11°.");

            if (certificado.AnoLectivo < 1990 || certificado.AnoLectivo > DateTime.Now.Year)
                return (false, $"El año lectivo debe ser válido (entre 1990 y {DateTime.Now.Year}).");

            if (string.IsNullOrWhiteSpace(certificado.InstitucionOrigen))
                return (false, "Debe especificar la institución educativa de procedencia.");

            var yaExiste = await _context.CertificadosEstudioExterno
                .AnyAsync(c => c.IdEstudiante == certificado.IdEstudiante 
                            && c.GradoAprobado == certificado.GradoAprobado 
                            && c.Activo 
                            && c.EstadoVerificacion == "Verificado");

            if (yaExiste)
                return (false, $"El estudiante ya tiene acreditado y verificado el Grado {certificado.GradoAprobado}°.");

            certificado.FechaRegistro = DateTime.Now;
            certificado.Activo = true;
            if (string.IsNullOrWhiteSpace(certificado.EstadoVerificacion))
            {
                certificado.EstadoVerificacion = "Pendiente de verificación";
            }
            if (certificado.EstadoVerificacion == "Verificado")
            {
                certificado.FechaVerificacion = DateTime.Now;
                certificado.RevisadoPor = usuarioResponsable;
            }

            _context.CertificadosEstudioExterno.Add(certificado);
            await _context.SaveChangesAsync();

            return (true, $"Certificado de Grado {certificado.GradoAprobado}° registrado con estado '{certificado.EstadoVerificacion}'.");
        }

        /// <summary>
        /// Verifica o rechaza un certificado externo de estudio.
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> VerificarCertificadoExternoAsync(
            int idCertificado, 
            string nuevoEstado, 
            string? observaciones, 
            string usuarioResponsable)
        {
            var cert = await _context.CertificadosEstudioExterno.FindAsync(idCertificado);
            if (cert == null || !cert.Activo)
                return (false, "Certificado no encontrado.");

            cert.EstadoVerificacion = nuevoEstado;
            cert.FechaVerificacion = DateTime.Now;
            cert.RevisadoPor = usuarioResponsable;
            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                cert.Observaciones = observaciones;
            }

            await _context.SaveChangesAsync();
            return (true, $"Certificado del Grado {cert.GradoAprobado}° marcado como '{nuevoEstado}'.");
        }
    }
}
