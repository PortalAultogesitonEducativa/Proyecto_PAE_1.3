using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PAE_APP_MOVIL_1._0.ViewModels
{
    public record PeriodoItem(int Numero)
    {
        public string Texto => $"Periodo {Numero}";
    }

    public record NotaDetalle(string Etiqueta, string NotaTexto, string FechaTexto, Color ColorNota);

    // Una materia (con su curso) dentro de un periodo, con el promedio de todas sus notas.
    public partial class MateriaResumen : ObservableObject
    {
        public MateriaResumen(string materia, decimal promedio, Color colorEstado, List<NotaDetalle> detalle)
        {
            // La base guarda "Matemáticas - 6°A": se separa el nombre de la materia del curso.
            int separador = materia.LastIndexOf(" - ", StringComparison.Ordinal);
            string titulo = separador > 0 ? materia[..separador] : materia;
            string curso = separador > 0 ? materia[(separador + 3)..] : string.Empty;
            string cantidad = detalle.Count == 1 ? "1 nota" : $"{detalle.Count} notas";

            Titulo = titulo;
            Subtitulo = curso.Length > 0 ? $"{curso} • {cantidad}" : cantidad;
            Promedio = promedio;
            PromedioTexto = promedio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            ColorEstado = colorEstado;
            Detalle = detalle;
        }

        public string Titulo { get; }

        public string Subtitulo { get; }

        public decimal Promedio { get; }

        public string PromedioTexto { get; }

        public Color ColorEstado { get; }

        public IReadOnlyList<NotaDetalle> Detalle { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Flecha))]
        private bool _isExpanded;

        public string Flecha => IsExpanded ? "▲" : "▼";

        [RelayCommand]
        private void AlternarDetalle()
        {
            IsExpanded = !IsExpanded;
        }
    }
}
