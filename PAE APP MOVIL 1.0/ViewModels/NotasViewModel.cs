using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PAE_APP_MOVIL_1._0.Models;
using PAE_APP_MOVIL_1._0.Services;

namespace PAE_APP_MOVIL_1._0.ViewModels
{
    public partial class NotasViewModel : ObservableObject
    {
        // Nota a partir de la cual se pinta en verde; por debajo, en rojo. Ajustar al reglamento del colegio.
        private const decimal NotaMinimaAprobatoria = 3.0m;

        private readonly NotasService _notasService;
        private readonly SessionService _session;
        private readonly IDialogService _dialogs;

        private List<Calificacion> _notas = new();

        public NotasViewModel(NotasService notasService, SessionService session, IDialogService dialogs)
        {
            _notasService = notasService;
            _session = session;
            _dialogs = dialogs;
        }

        public ObservableCollection<PeriodoItem> Periodos { get; } = new();

        public ObservableCollection<MateriaResumen> Materias { get; } = new();

        [ObservableProperty]
        private string _titulo = "Calificaciones";

        [ObservableProperty]
        private string _subtitulo = string.Empty;

        [ObservableProperty]
        private PeriodoItem? _periodoSeleccionado;

        [ObservableProperty]
        private int _totalMaterias;

        [ObservableProperty]
        private int _totalNotas;

        [ObservableProperty]
        private string _promedioGeneralTexto = "-";

        [ObservableProperty]
        private Color _promedioColor = PaeColores.Navy;

        [ObservableProperty]
        private string _periodoTexto = "-";

        [ObservableProperty]
        private bool _sinNotas;

        [ObservableProperty]
        private bool _isBusy;

        partial void OnPeriodoSeleccionadoChanged(PeriodoItem? value)
        {
            Reagrupar();
        }

        [RelayCommand]
        private async Task CargarAsync()
        {
            var estudiante = _session.EstudianteActivo;

            if (estudiante == null)
                return;

            Subtitulo = estudiante.NombreCompleto;
            IsBusy = true;

            try
            {
                _notas = await _notasService.ObtenerNotasAsync(estudiante.IdUsuario);

                var periodoAnterior = PeriodoSeleccionado;
                var numeros = _notas.Select(n => n.Periodo).Distinct().OrderBy(p => p).ToList();

                Periodos.Clear();
                foreach (var numero in numeros)
                    Periodos.Add(new PeriodoItem(numero));

                SinNotas = numeros.Count == 0;

                // Conserva el periodo elegido; si no existe, abre el más reciente.
                PeriodoSeleccionado = Periodos.FirstOrDefault(p => p == periodoAnterior) ?? Periodos.LastOrDefault();
                Reagrupar();
            }
            catch (Exception ex)
            {
                await _dialogs.AlertAsync("Error", ex.GetBaseException().Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Reagrupar()
        {
            Materias.Clear();

            if (PeriodoSeleccionado is null)
            {
                TotalMaterias = 0;
                TotalNotas = 0;
                PromedioGeneralTexto = "-";
                PromedioColor = PaeColores.Navy;
                PeriodoTexto = "-";
                return;
            }

            int periodo = PeriodoSeleccionado.Numero;

            var grupos = _notas
                .Where(n => n.Periodo == periodo)
                .GroupBy(n => n.Materia ?? string.Empty)
                .OrderBy(g => g.Key);

            foreach (var grupo in grupos)
            {
                var ordenadas = grupo.OrderBy(n => n.FechaRegistro).ToList();

                // Los ceros cuentan como nota, igual que en el portal.
                decimal promedio = Math.Round(ordenadas.Average(n => n.Nota), 2);

                var detalle = ordenadas
                    .Select((n, i) => new NotaDetalle(
                        $"Nota {i + 1}",
                        n.Nota.ToString("0.0", CultureInfo.InvariantCulture),
                        n.FechaRegistro.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                        ColorPara(n.Nota)))
                    .ToList();

                Materias.Add(new MateriaResumen(grupo.Key, promedio, ColorPara(promedio), detalle));
            }

            TotalMaterias = Materias.Count;
            TotalNotas = Materias.Sum(m => m.Detalle.Count);
            PeriodoTexto = PeriodoSeleccionado.Texto;

            if (Materias.Count == 0)
            {
                PromedioGeneralTexto = "-";
                PromedioColor = PaeColores.Navy;
                return;
            }

            // Promedio general: media de los promedios de cada materia, para que una materia con muchas notas no pese más.
            decimal general = Math.Round(Materias.Average(m => m.Promedio), 2);
            PromedioGeneralTexto = general.ToString("0.00", CultureInfo.InvariantCulture);
            PromedioColor = ColorPara(general);
        }

        private static Color ColorPara(decimal nota)
        {
            return nota >= NotaMinimaAprobatoria ? PaeColores.Verde : PaeColores.Rojo;
        }
    }
}
