using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Services;
using Rotativa.AspNetCore;
using ProyectoPAE.Servicios;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Localización (i18n: Español / Inglés)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Configuración de límites de formularios para planillas masivas de notas
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueCountLimit = int.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
});
// 1. Configuración de la base de datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
// 2. CONFIGURAR EL SERVICIO DE SESIONES (NUEVO)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15); //Tiempo de inactividad
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. REGISTRAR SERVICIO DE CORREO
builder.Services.AddScoped<ServicioEmail>();

// CERTIFICADOS
builder.Services.AddScoped<CertificadoService>();

var app = builder.Build();

// Configuración de idiomas soportados (Español e Inglés)
var supportedCultures = new[]
{
    new CultureInfo("es"),
    new CultureInfo("es-CO"),
    new CultureInfo("en"),
    new CultureInfo("en-US")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

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
// 4. HABILITAR EL USO DE SESIONES (NUEVO)
app.UseSession();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();