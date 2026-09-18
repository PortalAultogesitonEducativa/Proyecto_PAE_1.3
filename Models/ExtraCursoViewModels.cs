using System;
using System.Collections.Generic;

namespace ProyectoPAE.Models
{
    public class ExtraCursoItemViewModel
    {
        public int IdExtraCurso { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Instructor { get; set; }
        public int CuposTotales { get; set; }
        public int CuposDisponibles { get; set; }
        public int CuposOcupados => CuposTotales - CuposDisponibles;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime FechaInicioInscripcion { get; set; }
        public DateTime FechaFinInscripcion { get; set; }
        public bool Activo { get; set; }
        public int? GradoMin { get; set; }
        public int? GradoMax { get; set; }
        public string? Horario { get; set; }
        public int? IdDocente { get; set; }
        public string? TipoCurso { get; set; } = "Deportivo";
        public string? DocumentosRequeridos { get; set; }

        // Estado del estudiante actual respecto a este curso
        public bool EstaInscrito { get; set; }
        public int? IdInscripcion { get; set; }
        public DateTime? FechaInscripcionEstudiante { get; set; }
        public List<ExtraDocumentoInscripcion> DocumentosSubidos { get; set; } = new List<ExtraDocumentoInscripcion>();

        // Validación de estado de inscripción
        public bool PeriodoInscripcionAbierto => DateTime.Now.Date >= FechaInicioInscripcion.Date && DateTime.Now.Date <= FechaFinInscripcion.Date;
        public bool TieneCupos => CuposDisponibles > 0;
        public bool EsGradoValido(int? gradoEstudiante)
        {
            if (!gradoEstudiante.HasValue || gradoEstudiante.Value <= 0) return true;
            if (GradoMin.HasValue && gradoEstudiante.Value < GradoMin.Value) return false;
            if (GradoMax.HasValue && gradoEstudiante.Value > GradoMax.Value) return false;
            return true;
        }

        public List<string> DocumentosRequeridosLista => ObtenerListaDocumentosRequeridos();

        public List<string> ObtenerListaDocumentosRequeridos()
        {
            if (string.IsNullOrWhiteSpace(DocumentosRequeridos))
            {
                if (TipoCurso?.ToLower() == "deportivo")
                    return new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Certificado Médico de Aptitud Física", "Copia de EPS o Seguro Estudiantil" };
                else if (TipoCurso?.ToLower() == "servicio social y comunitario" || TipoCurso?.ToLower() == "servicio social")
                    return new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Copia de EPS o Seguro Estudiantil", "Paz y Salvo Institucional" };
                else
                    return new List<string> { "Documento de Identidad (TI / RC)", "Formato de Autorización de Padres / Acudientes", "Copia de EPS o Seguro Estudiantil" };
            }

            try
            {
                if (DocumentosRequeridos.Trim().StartsWith("["))
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(DocumentosRequeridos);
                    if (parsed != null && parsed.Any()) return parsed;
                }
            }
            catch { }

            return DocumentosRequeridos.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim()).ToList();
        }
    }

    public class ExtraEstudianteInscritoDto
    {
        public int IdInscripcion { get; set; }
        public int IdEstudiante { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public DateTime FechaInscripcion { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
