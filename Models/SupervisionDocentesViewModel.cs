using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoPAE.Models
{
    public class SupervisionDocentesViewModel
    {
        /// <summary>Una entrega sin calificar que lleva más de estos días se considera atrasada.</summary>
        public const int DiasEntregaAtrasada = 7;

        public List<SupervisionDocenteItem> Docentes { get; set; } = new();

        public List<PeriodoSubidaNota> Periodos { get; set; } = new();
        public int PeriodoActivo { get; set; } = 1;

        /// <summary>Año escolar de referencia: el más reciente que tiene horarios cargados.</summary>
        public int? AnioEscolar { get; set; }

        /// <summary>Materias del plan de cada curso que no tienen ningún horario con docente.</summary>
        public List<MateriaSinDocenteItem> MateriasSinDocente { get; set; } = new();

        /// <summary>Clases de un mismo docente que se cruzan en día y hora.</summary>
        public List<ChoqueHorarioItem> Choques { get; set; } = new();

        public int TotalClases => Docentes.Sum(d => d.Clases);
        public int TotalActividadesVigentes => Docentes.Sum(d => d.ActividadesVigentes);
        public int TotalEntregasSinCalificar => Docentes.Sum(d => d.EntregasSinCalificar);
        public int TotalEstudiantes { get; set; }

        public int DocentesConNotasAlDia => Docentes.Count(d => d.Clases == 0 || d.TieneNotasRegistradas);
        public int DocentesConNotasPendientes => Docentes.Count(d => d.Clases > 0 && !d.TieneNotasRegistradas);
        public int DocentesConAlerta => Docentes.Count(d => d.NivelAlerta > 0);
        public int DocentesSinClases => Docentes.Count(d => d.Clases == 0);
    }

    public class SupervisionDocenteItem
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int Clases { get; set; }
        public double HorasSemanales { get; set; }
        public int ChoquesHorario { get; set; }
        public List<string> Materias { get; set; } = new();
        public List<string> Cursos { get; set; } = new();
        public int ActividadesVigentes { get; set; }
        public int ActividadesVencidas { get; set; }
        public int EntregasSinCalificar { get; set; }
        public int EntregasSinCalificarAtrasadas { get; set; }
        public int PlanesMejoraActivos { get; set; }
        public int PlanesMejoraEnProceso { get; set; }
        public int PlanesMejoraVencidos { get; set; }
        public DateTime? UltimaActividad { get; set; }

        public int NotasRegistradasPeriodoActivo { get; set; }
        public bool TieneNotasRegistradas => NotasRegistradasPeriodoActivo > 0;

        /// <summary>0 = al día, 1 = requiere atención, 2 = crítico.</summary>
        public int NivelAlerta
        {
            get
            {
                if (ChoquesHorario > 0 || EntregasSinCalificarAtrasadas > 0) return 2;
                if (Clases == 0 || PlanesMejoraVencidos > 0 || EntregasSinCalificar > 0 || !TieneNotasRegistradas) return 1;
                return 0;
            }
        }

        public string EstadoTexto
        {
            get
            {
                if (Clases == 0) return "Sin clases asignadas";
                if (ChoquesHorario > 0) return "Choque de horario";
                if (EntregasSinCalificarAtrasadas > 0) return "Calificaciones atrasadas";
                if (PlanesMejoraVencidos > 0) return "Planes de mejora vencidos";
                if (EntregasSinCalificar > 0) return "Entregas pendientes";
                if (!TieneNotasRegistradas) return "Notas pendientes";
                return "Al día";
            }
        }

        /// <summary>Color de Bootstrap para la insignia de estado: danger, warning o success.</summary>
        public string EstadoClase => NivelAlerta == 2 ? "danger" : (NivelAlerta == 1 ? "warning" : "success");
    }

    public class MateriaSinDocenteItem
    {
        public string Curso { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
    }

    public class ChoqueHorarioItem
    {
        public string Docente { get; set; } = string.Empty;
        public string Dia { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
    }
}
