using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoPAE.Models
{
    public class SupervisionDocentesViewModel
    {
        public List<SupervisionDocenteItem> Docentes { get; set; } = new();

        public int TotalClases => Docentes.Sum(d => d.Clases);
        public int TotalActividadesVigentes => Docentes.Sum(d => d.ActividadesVigentes);
        public int TotalEntregasSinCalificar => Docentes.Sum(d => d.EntregasSinCalificar);
        public int TotalEstudiantes { get; set; }
    }

    public class SupervisionDocenteItem
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int Clases { get; set; }
        public List<string> Materias { get; set; } = new();
        public List<string> Cursos { get; set; } = new();
        public int ActividadesVigentes { get; set; }
        public int ActividadesVencidas { get; set; }
        public int EntregasSinCalificar { get; set; }
        public DateTime? UltimaActividad { get; set; }
    }
}
