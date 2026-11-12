<#
.SYNOPSIS
    Registra una tarea programada en Windows (Task Scheduler) para ejecutar
    el script de copias de seguridad de la base de datos PAE cada 24 horas.
#>

$ErrorActionPreference = "Stop"

$taskName = "PAE_Backup_BaseDatos_24H"
$scriptPath = Join-Path $PSScriptRoot "Backup-BaseDatos.ps1"
$scriptPath = [System.IO.Path]::GetFullPath($scriptPath)

Write-Host "Configurando disparador de 24 horas para copias de seguridad PAE..." -ForegroundColor Cyan
Write-Host "Script a ejecutar: $scriptPath" -ForegroundColor White

$action = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""

# Crear o actualizar la tarea programada con schtasks cada 24 horas a las 02:00 AM
try {
    & schtasks /create /tn $taskName /tr $action /sc daily /st 02:00 /f | Out-Null
    Write-Host "[ÉXITO] Tarea programada '$taskName' registrada exitosamente." -ForegroundColor Green
    Write-Host "        Se ejecutará automáticamente cada 24 horas a las 02:00 AM." -ForegroundColor White
} catch {
    Write-Warning "No fue posible registrar la tarea programada en el Programador de Tareas: $_"
    Write-Host "Nota: Puede ejecutar manualmente el script cuando lo requiera o dejar que el servicio en segundo plano de ASP.NET Core maneje el ciclo de 24 horas." -ForegroundColor Yellow
}

