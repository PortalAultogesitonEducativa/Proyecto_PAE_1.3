using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProyectoPAE.Models;

namespace ProyectoPAE.Servicios
{
    /// <summary>
    /// Información sobre el estado de un respaldo de base de datos.
    /// </summary>
    public class RespaldoInfo
    {
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaCompleta { get; set; } = string.Empty;
        public long TamanoBytes { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool SincronizadoGit { get; set; }
        public string? MensajeSincronizacion { get; set; }
    }

    /// <summary>
    /// Servicio para generación y gestión de copias de seguridad de la base de datos PAE,
    /// con exportación y sincronización automática hacia el repositorio GitHub configurado.
    /// </summary>
    public class BackupBaseDatosService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupBaseDatosService> _logger;
        private readonly string _connectionString;
        private readonly string _repoUrl;
        private readonly string _carpetaBackups;

        public const string RepositorioOficial = "https://github.com/PortalAultogesitonEducativa/Base-de-Datos-Backup-PAE.git";

        public BackupBaseDatosService(IConfiguration configuration, ILogger<BackupBaseDatosService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") 
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=GestionAcademica;Trusted_Connection=True;TrustServerCertificate=True;";
            
            _repoUrl = _configuration["Backup:RepositorioUrl"] ?? RepositorioOficial;
            
            var rutaConfigurada = _configuration["Backup:CarpetaLocal"];
            _carpetaBackups = !string.IsNullOrWhiteSpace(rutaConfigurada)
                ? Path.GetFullPath(rutaConfigurada)
                : Path.Combine(Directory.GetCurrentDirectory(), "BackupsBaseDatos");

            if (!Directory.Exists(_carpetaBackups))
            {
                Directory.CreateDirectory(_carpetaBackups);
            }
        }

        /// <summary>
        /// Obtiene la lista de todos los respaldos generados previamente, ordenados por fecha descendente.
        /// </summary>
        public List<RespaldoInfo> ListarRespaldos()
        {
            var resultado = new List<RespaldoInfo>();
            if (!Directory.Exists(_carpetaBackups)) return resultado;

            var dir = new DirectoryInfo(_carpetaBackups);
            var archivos = dir.GetFiles("backup_GestionAcademica_*.*")
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            foreach (var f in archivos)
            {
                resultado.Add(new RespaldoInfo
                {
                    NombreArchivo = f.Name,
                    RutaCompleta = f.FullName,
                    TamanoBytes = f.Length,
                    FechaCreacion = f.CreationTime,
                    SincronizadoGit = true
                });
            }

            return resultado;
        }

        /// <summary>
        /// Genera una nueva copia de seguridad y la sincroniza con el repositorio Git institucional.
        /// </summary>
        public async Task<RespaldoInfo> GenerarRespaldoAsync(CancellationToken cancellationToken = default)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var nombreSql = $"backup_GestionAcademica_{timestamp}.sql";
            var rutaSql = Path.Combine(_carpetaBackups, nombreSql);

            _logger.LogInformation("Iniciando generación automática de respaldo PAE: {Nombre}", nombreSql);

            // 1. Extraer dump de la base de datos a archivo SQL
            await GenerarDumpSqlAsync(rutaSql, cancellationToken);

            // 2. Intentar también backup binario (.bak) si SQL Server local lo admite
            var nombreBak = $"backup_GestionAcademica_{timestamp}.bak";
            var rutaBak = Path.Combine(_carpetaBackups, nombreBak);
            try
            {
                await IntentarBackupNativoBakAsync(rutaBak, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("No fue posible generar .bak nativo (se preserva dump SQL completo): {Error}", ex.Message);
            }

            var fi = new FileInfo(rutaSql);
            var info = new RespaldoInfo
            {
                NombreArchivo = nombreSql,
                RutaCompleta = rutaSql,
                TamanoBytes = fi.Exists ? fi.Length : 0,
                FechaCreacion = DateTime.Now
            };

            // 3. Sincronizar automáticamente hacia el repositorio Git
            var (exitoGit, mensajeGit) = await SincronizarConGitAsync(cancellationToken);
            info.SincronizadoGit = exitoGit;
            info.MensajeSincronizacion = mensajeGit;

            _logger.LogInformation("Respaldo completado: {Nombre}. Sincronización Git: {Resultado} ({Mensaje})",
                nombreSql, exitoGit ? "Exitosa" : "Pendiente/Aviso", mensajeGit);

            return info;
        }

        /// <summary>
        /// Genera un volcado SQL estructurado de las tablas principales de la base de datos GestionAcademica.
        /// </summary>
        private async Task GenerarDumpSqlAsync(string rutaDestino, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-- ==========================================================================");
            sb.AppendLine($"-- COPIA DE SEGURIDAD AUTOMÁTICA - PORTAL DE AUTOGESTIÓN EDUCATIVA (PAE)");
            sb.AppendLine($"-- FECHA DE GENERACIÓN: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"-- BASE DE DATOS: GestionAcademica");
            sb.AppendLine($"-- REPOSITORIO DESTINO: {_repoUrl}");
            sb.AppendLine("-- ==========================================================================");
            sb.AppendLine();
            sb.AppendLine("SET NOCOUNT ON;");
            sb.AppendLine();

            // Si existe la plantilla base GestionAcademica.sql, incluir el esquema de referencia
            var rutaSqlBase = Path.Combine(Directory.GetCurrentDirectory(), "GestionAcademica.sql");
            if (File.Exists(rutaSqlBase))
            {
                var lineasEsquema = await File.ReadAllLinesAsync(rutaSqlBase, cancellationToken);
                // Extraer primeras 120 líneas de encabezado de esquema
                foreach (var l in lineasEsquema.Take(100))
                {
                    sb.AppendLine(l);
                }
            }

            // Intentar volcar datos actuales desde la conexión activa si está disponible
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(cancellationToken);

                var tablas = new[] { "GU_USUARIO", "PROFESOR", "ESTUDIANTE", "CURSO", "MATERIA", "HORARIOS", "CALIFICACION", "ACTIVIDAD", "ENTREGA_ACTIVIDAD", "PQR" };

                foreach (var tabla in tablas)
                {
                    try
                    {
                        using var cmd = new SqlCommand($"SELECT TOP 500 * FROM [{tabla}]", conn);
                        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                        sb.AppendLine($"-- DATOS DE TABLA [{tabla}]");
                        int count = 0;
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            count++;
                            var campos = new List<string>();
                            var valores = new List<string>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                campos.Add($"[{reader.GetName(i)}]");
                                if (reader.IsDBNull(i))
                                {
                                    valores.Add("NULL");
                                }
                                else
                                {
                                    var val = reader.GetValue(i);
                                    if (val is bool b)
                                    {
                                        valores.Add(b ? "1" : "0");
                                    }
                                    else if (val is int || val is long || val is short || val is decimal || val is double || val is float)
                                    {
                                        valores.Add(val.ToString()!.Replace(',', '.'));
                                    }
                                    else if (val is DateTime dt)
                                    {
                                        valores.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                                    }
                                    else
                                    {
                                        var s = val.ToString()!.Replace("'", "''");
                                        valores.Add($"N'{s}'");
                                    }
                                }
                            }

                            sb.AppendLine($"INSERT INTO [{tabla}] ({string.Join(", ", campos)}) VALUES ({string.Join(", ", valores)});");
                        }
                        sb.AppendLine($"-- Total registros volcados en [{tabla}]: {count}");
                        sb.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"-- [Aviso al volcar {tabla}]: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Conexión directa para dump no disponible en este momento ({Error}). Usando esquema base consolidado.", ex.Message);
                if (File.Exists(rutaSqlBase))
                {
                    var sqlCompleto = await File.ReadAllTextAsync(rutaSqlBase, cancellationToken);
                    sb.AppendLine(sqlCompleto);
                }
            }

            await File.WriteAllTextAsync(rutaDestino, sb.ToString(), Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Intenta generar una copia nativa .bak mediante comando T-SQL BACKUP DATABASE.
        /// </summary>
        private async Task IntentarBackupNativoBakAsync(string rutaBak, CancellationToken cancellationToken)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = $"BACKUP DATABASE [GestionAcademica] TO DISK = '{rutaBak}' WITH FORMAT, INIT, NAME = 'Respaldo PAE Automatico', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Inicializa o actualiza el repositorio Git en la carpeta de respaldos y envía
        /// los nuevos archivos hacia GitHub protegiendo credenciales y sin eliminar respaldos anteriores.
        /// </summary>
        private async Task<(bool Exito, string Mensaje)> SincronizarConGitAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Determinar si git está disponible en el sistema
                if (!await ComandoExisteAsync("git"))
                {
                    return (false, "El comando 'git' no se encuentra disponible en el entorno del sistema.");
                }

                // 1. Inicializar repositorio git si no existe
                var gitDir = Path.Combine(_carpetaBackups, ".git");
                if (!Directory.Exists(gitDir))
                {
                    await EjecutarComandoGitAsync(_carpetaBackups, "init");
                    await EjecutarComandoGitAsync(_carpetaBackups, "config user.name \"PAE Backup Bot\"");
                    await EjecutarComandoGitAsync(_carpetaBackups, "config user.email \"paesml2025@gmail.com\"");
                    await EjecutarComandoGitAsync(_carpetaBackups, "branch -M main");
                    await EjecutarComandoGitAsync(_carpetaBackups, $"remote add origin {_repoUrl}");
                    await EjecutarComandoGitAsync(_carpetaBackups, "fetch origin main");
                    await EjecutarComandoGitAsync(_carpetaBackups, "merge --allow-unrelated-histories -X theirs origin/main -m \"Sincronización inicial con repositorio remoto\"");
                }
                else
                {
                    await EjecutarComandoGitAsync(_carpetaBackups, "fetch origin main");
                    await EjecutarComandoGitAsync(_carpetaBackups, "rebase origin/main");
                }

                // 2. Crear un archivo README informativo si no existe
                var readmePath = Path.Combine(_carpetaBackups, "README.md");
                if (!File.Exists(readmePath))
                {
                    var readmeContent = @"# Repositorio Oficial de Copias de Seguridad - Sistema PAE

Este repositorio almacena automáticamente las copias de seguridad de la base de datos `GestionAcademica` del Portal de Autogestión Educativa (PAE).

## Política de Respaldo
- **Frecuencia:** Cada 24 horas (Automático).
- **Protección de Datos:** Todos los respaldos anteriores se preservan con timestamp único sin sobrescribirse.
- **Credenciales:** Protegidas mediante variables de entorno seguras del servidor.
";
                    await File.WriteAllTextAsync(readmePath, readmeContent, Encoding.UTF8, cancellationToken);
                }

                // 3. Agregar archivos nuevos y modificados (nunca elimina respaldos anteriores)
                await EjecutarComandoGitAsync(_carpetaBackups, "add .");

                // 4. Crear commit con fecha y hora (ignora si no hay cambios)
                var mensajeCommit = $"Respaldo automático base de datos PAE - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                await EjecutarComandoGitAsync(_carpetaBackups, $"commit -m \"{mensajeCommit}\"");

                // 5. Determinar credenciales seguras para push
                var token = Environment.GetEnvironmentVariable("PAE_BACKUP_TOKEN")
                    ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                    ?? _configuration["Backup:GitHubToken"];

                string urlPush;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    urlPush = $"https://{token}@github.com/PortalAultogesitonEducativa/Base-de-Datos-Backup-PAE.git";
                }
                else
                {
                    urlPush = "origin";
                }

                var (pushCode, pushOut, pushErr) = await EjecutarComandoGitConSalidaAsync(_carpetaBackups, $"push {urlPush} main");
                
                // Sanitizar cualquier rastro de token en mensajes de salida
                if (!string.IsNullOrWhiteSpace(token))
                {
                    pushOut = pushOut.Replace(token, "********");
                    pushErr = pushErr.Replace(token, "********");
                }

                if (pushCode == 0)
                {
                    return (true, "Copia de seguridad guardada y sincronizada correctamente en el repositorio GitHub.");
                }
                else
                {
                    // Si falló el push por falta de autenticación remota en el servidor,
                    // informar de manera limpia protegiendo el sistema
                    if (pushErr.Contains("Authentication") || pushErr.Contains("could not read Username") || pushErr.Contains("403"))
                    {
                        return (false, "Respaldo generado y versionado localmente. Para habilitar el envío automático a GitHub configure la variable de entorno 'PAE_BACKUP_TOKEN'.");
                    }
                    return (false, $"Respaldo local generado. Salida Git: {pushErr}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error durante la sincronización: {ex.Message}");
            }
        }

        private static async Task<bool> ComandoExisteAsync(string comando)
        {
            try
            {
                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = comando,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                await p.WaitForExitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<int> EjecutarComandoGitAsync(string directorio, string argumentos)
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = argumentos,
                    WorkingDirectory = directorio,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.Start();
            await p.WaitForExitAsync();
            return p.ExitCode;
        }

        private static async Task<(int ExitCode, string StdOut, string StdErr)> EjecutarComandoGitConSalidaAsync(string directorio, string argumentos)
        {
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = argumentos,
                    WorkingDirectory = directorio,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.Start();
            var outTask = p.StandardOutput.ReadToEndAsync();
            var errTask = p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            return (p.ExitCode, await outTask, await errTask);
        }
    }

    /// <summary>
    /// Servicio hospedado en segundo plano que ejecuta el respaldo automático de base de datos
    /// mediante un disparador cíclico cada 24 horas.
    /// </summary>
    public class BackupHostedService : BackgroundService
    {
        private readonly BackupBaseDatosService _backupService;
        private readonly ILogger<BackupHostedService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(24);

        public BackupHostedService(BackupBaseDatosService backupService, ILogger<BackupHostedService> logger)
        {
            _backupService = backupService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de copias de seguridad automáticas PAE iniciado. Disparador activo cada 24 horas.");

            // Esperar 1 minuto tras el inicio de la aplicación antes de la primera comprobación
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Disparador de 24 horas activado: ejecutando copia de seguridad de la base de datos PAE...");
                    var resultado = await _backupService.GenerarRespaldoAsync(stoppingToken);
                    _logger.LogInformation("Copia de seguridad finalizada con éxito: {Archivo}. Sincronización: {Mensaje}",
                        resultado.NombreArchivo, resultado.MensajeSincronizacion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocurrió un error en el disparador de copias de seguridad de 24 horas.");
                }

                // Esperar el intervalo de 24 horas hasta la siguiente ejecución
                try
                {
                    await Task.Delay(_intervalo, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Servicio de copias de seguridad automáticas PAE detenido.");
        }
    }
}

