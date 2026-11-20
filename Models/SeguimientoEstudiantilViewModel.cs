using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoPAE.Models
{
    /// <summary>Datos de la pantalla de seguimiento estudiantil del coordinador (solo lectura).</summary>
    public class SeguimientoEstudiantilViewModel
    {
        /// <summary>Nota por debajo de la cual el estudiante se considera en riesgo (escala de 1.0 a 5.0).</summary>
        public const decimal NotaMinimaAprobar = 3.0m;

        /// <summary>Si en un curso no se registra asistencia hace mas de estos dias, se marca como alerta.</summary>
        public const int DiasSinAsistenciaAlerta = 7;

        /// <summary>Periodo academico (1 a 4) que se esta revisando.</summary>
        public int Periodo { get; set; } = 1;

        /// <summary>Ano de las calificaciones usadas: el mas reciente que tenga notas registradas.</summary>
        public int? AnioNotas { get; set; }

        public List<EstudianteRiesgoItem> EstudiantesEnRiesgo { get; set; } = new();
        public List<CoberturaNotasItem> CoberturaNotas { get; set; } = new();
        public List<AsistenciaCursoItem> AsistenciaPorCurso { get; set; } = new();

        public int TotalNotasPeriodo { get; set; }

        public int EnRiesgoSinPlan => EstudiantesEnRiesgo.Count(e => !e.TienePlan);
        public int EstudiantesEnRiesgoUnicos => EstudiantesEnRiesgo.Select(e => e.IdEstudiante).Distinct().Count();
        public int ClasesConNotasIncompletas => CoberturaNotas.Count(c => c.Porcentaje < 100);
        public int CursosSinAsistenciaReciente => AsistenciaPorCurso.Count(a => a.EnAlerta);
    }

    /// <summary>Una nota baja de un estudiante en una materia, con el estado de su plan de mejora.</summary>
    public class EstudianteRiesgoItem
    {
        public int IdEstudiante { get; set; }
        public string Estudiante { get; set; } = string.Empty;
        public string Curso { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public decimal Nota { get; set; }
        public bool TienePlan { get; set; }
        public string EstadoPlan { get; set; } = string.Empty;
        public bool PlanVencido { get; set; }
        public string Docente { get; set; } = string.Empty;
    }

    /// <summary>Cuantos estudiantes de un curso ya tienen nota en una materia durante el periodo revisado.</summary>
    public class CoberturaNotasItem
    {
        public string Curso { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public string Docente { get; set; } = string.Empty;
        public int Estudiantes { get; set; }
        public int ConNota { get; set; }
        public int Faltan => Estudiantes - ConNota;
        public int Porcentaje => Estudiantes == 0 ? 0 : (int)Math.Round(ConNota * 100.0 / Estudiantes);
    }

    /// <summary>Ultima asistencia registrada entre los estudiantes de un curso.</summary>
    public class AsistenciaCursoItem
    {
        public string Curso { get; set; } = string.Empty;
        public int Estudiantes { get; set; }
        public int ConRegistro { get; set; }
        public DateTime? UltimaFecha { get; set; }

        public int? DiasDesdeUltimo => UltimaFecha.HasValue ? (int)(DateTime.Now.Date - UltimaFecha.Value.Date).TotalDays : (int?)null;
        public bool EnAlerta => !UltimaFecha.HasValue || DiasDesdeUltimo > SeguimientoEstudiantilViewModel.DiasSinAsistenciaAlerta;
    }
}
