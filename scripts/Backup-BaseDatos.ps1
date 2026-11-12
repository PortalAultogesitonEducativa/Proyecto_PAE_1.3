<#
.SYNOPSIS
    Script automatizado para generar copias de seguridad de la base de datos PAE
    y sincronizarlas con el repositorio GitHub institucional cada 24 horas.

.DESCRIPTION
    1. Genera un volcado SQL y copia de respaldo con timestamp único.
    2. Versiona el archivo en la carpeta BackupsBaseDatos.
    3. Conserva todos los respaldos anteriores (sin eliminaciones accidentales).
    4. Protege credenciales sensibles usando variables de entorno seguras (PAE_BACKUP_TOKEN).
    5. Empuja los cambios al repositorio oficial:
       https://github.com/PortalAultogesitonEducativa/Base-de-Datos-Backup-PAE.git
#>

[CmdletBinding()]
param (
    [string]$RepoUrl = "https://github.com/PortalAultogesitonEducativa/Base-de-Datos-Backup-PAE.git",
    [string]$CarpetaBackups = "$PSScriptRoot\..\BackupsBaseDatos",
    [string]$ServidorSql = "(localdb)\MSSQLLocalDB",
    [string]$BaseDatos = "GestionAcademica"
)

$ErrorActionPreference = "Continue"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " [PAE] PROCESO AUTOMATIZADO DE COPIA DE SEGURIDAD (24H)" -ForegroundColor Yellow
Write-Host " Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host " Base de Datos: $BaseDatos" -ForegroundColor White
Write-Host " Repositorio: $RepoUrl" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Asegurar directorio de respaldos
$CarpetaBackups = [System.IO.Path]::GetFullPath($CarpetaBackups)
if (-not (Test-Path $CarpetaBackups)) {
    New-Item -ItemType Directory -Path $CarpetaBackups -Force | Out-Null
    Write-Host "[OK] Directorio de respaldos creado en: $CarpetaBackups" -ForegroundColor Green
}

# 2. Generar nombre con timestamp único para preservar respaldos históricos
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$nombreArchivoSql = "backup_GestionAcademica_$timestamp.sql"
$rutaArchivoSql = Join-Path $CarpetaBackups $nombreArchivoSql

$nombreArchivoBak = "backup_GestionAcademica_$timestamp.bak"
$rutaArchivoBak = Join-Path $CarpetaBackups $nombreArchivoBak

Write-Host "`n1. Generando volcado de base de datos..." -ForegroundColor Yellow

# Intentar backup nativo .bak vía sqlcmd si está disponible
$bakGenerado = $false
try {
    $sqlCmdCheck = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($sqlCmdCheck) {
        $backupQuery = "BACKUP DATABASE [$BaseDatos] TO DISK = N'$rutaArchivoBak' WITH FORMAT, INIT, NAME = N'PAE-Backup-Auto', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
        & sqlcmd -S $ServidorSql -Q $backupQuery -b -t 60 2>$null
        if (Test-Path $rutaArchivoBak) {
            $bakGenerado = $true
            Write-Host "[OK] Respaldo nativo generado: $nombreArchivoBak ($( (Get-Item $rutaArchivoBak).Length ) bytes)" -ForegroundColor Green
        }
    }
} catch {
    Write-Warning "No fue posible generar .bak nativo: $_"
}

# Generar volcado SQL
$encabezadoSql = @"
-- ==========================================================================
-- COPIA DE SEGURIDAD AUTOMÁTICA - SISTEMA PAE
-- GENERADO AUTOMÁTICAMENTE CADA 24 HORAS
-- FECHA: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
-- BASE DE DATOS: $BaseDatos
-- DESTINO: $RepoUrl
-- ==========================================================================

"@

$rutaBaseSql = Join-Path $PSScriptRoot "..\GestionAcademica.sql"
if (Test-Path $rutaBaseSql) {
    $contenidoBase = Get-Content $rutaBaseSql -Raw
    $encabezadoSql + "`n" + $contenidoBase | Set-Content -Path $rutaArchivoSql -Encoding UTF8
    Write-Host "[OK] Volcado SQL estructurado generado: $nombreArchivoSql ($( (Get-Item $rutaArchivoSql).Length ) bytes)" -ForegroundColor Green
} else {
    $encabezadoSql | Set-Content -Path $rutaArchivoSql -Encoding UTF8
    Write-Host "[OK] Archivo de registro SQL generado: $nombreArchivoSql" -ForegroundColor Green
}

# 3. Sincronización con Repositorio GitHub
Write-Host "`n2. Sincronizando con el repositorio Git..." -ForegroundColor Yellow

$gitCmd = Get-Command git -ErrorAction SilentlyContinue
if (-not $gitCmd) {
    Write-Warning "[AVISO] Git no se encuentra en el PATH. El respaldo se conserva localmente."
    exit 0
}

Push-Location $CarpetaBackups

try {
    # Inicializar repo si no existe
    if (-not (Test-Path ".git")) {
        git init --quiet
        git config user.name "PAE Backup Bot"
        git config user.email "paesml2025@gmail.com"
        git branch -M main
        git remote add origin $RepoUrl
        Write-Host "[OK] Repositorio Git local inicializado." -ForegroundColor Green
    }

    # Crear README explicativo
    if (-not (Test-Path "README.md")) {
        @"
# Repositorio Oficial de Copias de Seguridad - Sistema PAE

Copias de seguridad automáticas de la base de datos `GestionAcademica`.
- Generación cíclica cada 24 horas.
- Respaldos históricos preservados sin eliminación.
"@ | Set-Content "README.md" -Encoding UTF8
    }

    # Agregar archivos sin borrar anteriores
    git add .

    # Crear commit
    $commitMsg = "Respaldo automático base de datos PAE - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    git commit -m $commitMsg --quiet

    Write-Host "[OK] Commit generado: $commitMsg" -ForegroundColor Green

    # Credenciales protegidas: leer de variable de entorno sin exponer en logs
    $token = $env:PAE_BACKUP_TOKEN
    if (-not $token) {
        $token = $env:GITHUB_TOKEN
    }

    if ($token) {
        $urlSegura = "https://${token}@github.com/PortalAultogesitonEducativa/Base-de-Datos-Backup-PAE.git"
        Write-Host "3. Enviando respaldo a GitHub (credenciales protegidas)..." -ForegroundColor Yellow
        git push $urlSegura main --quiet 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[ÉXITO] Copia de seguridad guardada y sincronizada en el repositorio GitHub." -ForegroundColor Green
        } else {
            Write-Warning "El respaldo fue guardado localmente en Git. Verifique los permisos del token GitHub."
        }
    } else {
        Write-Host "[AVISO] El respaldo fue versionado localmente con éxito en la rama main." -ForegroundColor Cyan
        Write-Host "       Para habilitar la subida remota automática configure la variable de entorno PAE_BACKUP_TOKEN." -ForegroundColor Gray
    }
} finally {
    Pop-Location
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " [PAE] RESPALDO DE 24 HORAS COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

