using PAE_APP_MOVIL_1._0.Services;
using PAE_APP_MOVIL_1._0.ViewModels;
using PAE_APP_MOVIL_1._0;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // URL del portal PAE en Cloudflare (https://pae-portal.lat), editable desde "Configurar servidor".
        builder.Services.AddSingleton<ServidorConfigService>();
        builder.Services.AddSingleton<ApiService>();

        // Servicios de datos
        builder.Services.AddTransient<AuthService>();
        builder.Services.AddTransient<EmailService>();
        builder.Services.AddTransient<NotasService>();
        builder.Services.AddTransient<AsistenciaService>();
        builder.Services.AddTransient<CitacionesService>();
        builder.Services.AddTransient<AcudienteService>();
        builder.Services.AddTransient<HorarioService>();
        builder.Services.AddTransient<ActividadesService>();

        // Servicios de infraestructura MVVM (únicos para toda la app)
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<NotasViewModel>();
        builder.Services.AddTransient<ServidorViewModel>();

        // Páginas
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ServidorPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<ResetPasswordPage>();
        builder.Services.AddTransient<StudentDashboardPage>();
        builder.Services.AddTransient<AcudienteDashboardPage>();
        builder.Services.AddTransient<NotasPage>();
        builder.Services.AddTransient<AsistenciaPage>();
        builder.Services.AddTransient<CitacionesPage>();
        builder.Services.AddTransient<HorarioPage>();
        builder.Services.AddTransient<ActividadesPage>();

        return builder.Build();
    }
}
