using System;
using ProyectoPAE.Models;

namespace ProyectoPAE.Servicios
{
    /// <summary>Resultado de evaluar el plazo de una solicitud.</summary>
    public sealed class EstadoPlazo
    {
        /// <summary>vencida, por-vencer, en-plazo, entregada-a-tiempo, entregada-con-retraso o cancelada.</summary>
        public string Clave { get; init; } = string.Empty;
        public string Texto { get; init; } = string.Empty;
        /// <summary>Color de Bootstrap: success, warning, danger o secondary.</summary>
        public string Clase { get; init; } = "secondary";
        public string Icono { get; init; } = "bi-circle";
        /// <summary>0 = más urgente. Sirve para ordenar la lista.</summary>
        public int Prioridad { get; init; } = 3;
        public int? DiasHabilesRestantes { get; init; }
    }

    /// <summary>
    /// Reglas del plazo de entrega de certificados. El plazo se cuenta en días hábiles (lunes a viernes;
    /// los festivos no se descuentan). El vencimiento no se guarda en la base de datos: se calcula al mostrar.
    /// </summary>
    public static class PlazoCertificado
    {
        /// <summary>Días hábiles que se dan, por defecto, para entregar un certificado.</summary>
        public const int DiasHabilesPlazo = 5;

        /// <summary>Se avisa que la solicitud está próxima a vencer cuando faltan estos días hábiles o menos.</summary>
        public const int DiasHabilesAviso = 2;

        public static bool EsDiaHabil(DateTime fecha)
        {
            return fecha.DayOfWeek != DayOfWeek.Saturday && fecha.DayOfWeek != DayOfWeek.Sunday;
        }

        /// <summary>Fecha resultante de sumar días hábiles a partir de una fecha de inicio.</summary>
        public static DateTime SumarDiasHabiles(DateTime inicio, int dias)
        {
            var fecha = inicio.Date;
            int agregados = 0;
            while (agregados < dias)
            {
                fecha = fecha.AddDays(1);
                if (EsDiaHabil(fecha))
                    agregados++;
            }
            return fecha;
        }

        /// <summary>
        /// Días hábiles que faltan desde hoy hasta la fecha límite.
        /// 0 significa que vence hoy y un valor negativo que ya está vencida.
        /// </summary>
        public static int DiasHabilesRestantes(DateTime hoy, DateTime limite)
        {
            hoy = hoy.Date;
            limite = limite.Date;

            if (limite == hoy)
                return 0;

            if (limite > hoy)
            {
                int faltan = 0;
                for (var f = hoy.AddDays(1); f <= limite; f = f.AddDays(1))
                {
                    if (EsDiaHabil(f))
                        faltan++;
                }
                return faltan;
            }

            int atraso = 0;
            for (var f = limite.AddDays(1); f <= hoy; f = f.AddDays(1))
            {
                if (EsDiaHabil(f))
                    atraso++;
            }
            // Si la fecha límite pasó pero solo cayeron días no hábiles, igual cuenta como vencida.
            return -Math.Max(1, atraso);
        }

        public static bool EstaAbierta(SolicitudCertificado solicitud)
        {
            return solicitud.Estado == EstadoSolicitudCertificado.Pendiente
                || solicitud.Estado == EstadoSolicitudCertificado.EnProceso;
        }

        public static EstadoPlazo Evaluar(SolicitudCertificado solicitud, DateTime? ahora = null)
        {
            var hoy = (ahora ?? DateTime.Now).Date;

            if (solicitud.Estado == EstadoSolicitudCertificado.Cancelada)
            {
                return new EstadoPlazo { Clave = "cancelada", Texto = "Cancelada", Clase = "secondary", Icono = "bi-x-circle-fill", Prioridad = 3 };
            }

            if (solicitud.Estado == EstadoSolicitudCertificado.Entregada)
            {
                bool aTiempo = solicitud.FechaEntrega.HasValue && solicitud.FechaEntrega.Value.Date <= solicitud.FechaLimite.Date;
                return aTiempo
                    ? new EstadoPlazo { Clave = "entregada-a-tiempo", Texto = "Entregada a tiempo", Clase = "success", Icono = "bi-check-circle-fill", Prioridad = 3 }
                    : new EstadoPlazo { Clave = "entregada-con-retraso", Texto = "Entregada con retraso", Clase = "warning", Icono = "bi-clock-history", Prioridad = 3 };
            }

            int restantes = DiasHabilesRestantes(hoy, solicitud.FechaLimite);

            if (restantes < 0)
            {
                int atraso = -restantes;
                return new EstadoPlazo
                {
                    Clave = "vencida",
                    Texto = atraso == 1 ? "Vencida hace 1 día hábil" : $"Vencida hace {atraso} días hábiles",
                    Clase = "danger",
                    Icono = "bi-exclamation-octagon-fill",
                    Prioridad = 0,
                    DiasHabilesRestantes = restantes
                };
            }

            if (restantes <= DiasHabilesAviso)
            {
                string texto = restantes == 0
                    ? "Vence hoy"
                    : restantes == 1 ? "Vence en 1 día hábil" : $"Vence en {restantes} días hábiles";

                return new EstadoPlazo
                {
                    Clave = "por-vencer",
                    Texto = texto,
                    Clase = "warning",
                    Icono = "bi-exclamation-triangle-fill",
                    Prioridad = 1,
                    DiasHabilesRestantes = restantes
                };
            }

            return new EstadoPlazo
            {
                Clave = "en-plazo",
                Texto = $"En plazo ({restantes} días hábiles)",
                Clase = "success",
                Icono = "bi-hourglass-split",
                Prioridad = 2,
                DiasHabilesRestantes = restantes
            };
        }
    }
}