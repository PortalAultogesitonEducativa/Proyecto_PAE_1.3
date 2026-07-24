using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;
using ProyectoPAE.Services;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();
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

var app = builder.Build();
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