using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Services;
using ProyectoPAE.Servicios;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews();

// CONFIGURACIÓN DE PROXY INVERSO (CLOUDFLARE TUNNEL)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 1. Configuración de la base de datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. CONFIGURAR EL SERVICIO DE SESIONES Y POLÍTICAS DE COOKIES SEGURAS
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15); // Tiempo de inactividad
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Forzar HTTPS estricto para cookies
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always; // Forzar que todas las cookies se envíen únicamente por HTTPS
});

// 3. REGISTRAR SERVICIO DE CORREO Y GESTIÓN DOCUMENTAL
builder.Services.AddScoped<ServicioEmail>();
builder.Services.AddScoped<GestorDocumentalService>();

// CERTIFICADOS Y MATRÍCULAS
builder.Services.AddScoped<CertificadoService>();
builder.Services.AddScoped<MatriculaAcademicaService>();
builder.Services.AddScoped<SolicitudCertificadoService>();
builder.Services.AddScoped<CambioCorreoService>();

// 4. SERVICIOS DE COPIAS DE SEGURIDAD (DISPARADOR AUTOMÁTICO 24 HORAS)
builder.Services.AddSingleton<BackupBaseDatosService>();
builder.Services.AddHostedService<BackupHostedService>();

var app = builder.Build();

// DEBE IR PRIMERO: Procesar encabezados de Cloudflare antes de redirecciones o rutas
app.UseForwardedHeaders();

// Configuración de Rotativa para encontrar el ejecutable en wwwroot/Rotativa
IWebHostEnvironment env = app.Services.GetRequiredService<IWebHostEnvironment>();
RotativaConfiguration.Setup(env.WebRootPath, "Rotativa");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 5. INTERNACIONALIZACIÓN (Español por defecto + Inglés opcional)
var culturasSoportadas = new[] { new CultureInfo("es"), new CultureInfo("en") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es"),
    SupportedCultures = culturasSoportadas,
    SupportedUICultures = culturasSoportadas
};
// Solo usar la cookie explícita del usuario para que por defecto SIEMPRE arranque en Español
localizationOptions.RequestCultureProviders.Clear();
localizationOptions.RequestCultureProviders.Add(new CookieRequestCultureProvider());
app.UseRequestLocalization(localizationOptions);

// 6. HABILITAR POLÍTICAS DE COOKIES, SESIONES Y AUTORIZACIÓN
app.UseCookiePolicy();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();