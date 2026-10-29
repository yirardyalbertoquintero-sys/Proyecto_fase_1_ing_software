param(
    [string]$Servidor = 'localhost',
    [switch]$SoloPreparar,
    [switch]$PrepararVSCode
)
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path -Parent $PSScriptRoot
$proyectoApi = Join-Path $raizProyecto 'src/backend/ImpactoTextil.Api'
Push-Location $raizProyecto
try {
    $constructor = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $constructor['Data Source'] = $Servidor
    $constructor['Initial Catalog'] = 'ImpactoTextil'
    $constructor['Integrated Security'] = $true
    $constructor['Encrypt'] = $true
    $constructor['TrustServerCertificate'] = $true
    $env:ConnectionStrings__ImpactoTextil = $constructor.ConnectionString
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo restaurar la herramienta de migraciones.' }
    dotnet restore ImpactoTextil.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron restaurar las dependencias.' }
    dotnet ef database update --project $proyectoApi
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar la base propia. Revisa SQL Server y los permisos.' }
    if ($PrepararVSCode) {
        # Configuracion local ignorada por Git; no escribir conexiones en launch.json.
        $rutaConfiguracion = Join-Path $raizProyecto '.env.vscode'
        $contenidoConfiguracion = 'ConnectionStrings__ImpactoTextil=' + $constructor.ConnectionString
        [System.IO.File]::WriteAllText($rutaConfiguracion, $contenidoConfiguracion + [Environment]::NewLine, (New-Object System.Text.UTF8Encoding($false)))
    }
    if ($SoloPreparar) {
        Write-Output 'Preparacion completada. VS Code puede iniciar la aplicacion.'
        return
    }
    Write-Output 'Abre https://localhost:7105 para registrarte y usa Inicia sesion para acceder. Ctrl+C detiene la aplicacion.'
    dotnet run --project $proyectoApi --no-build --launch-profile https
} finally {
    Pop-Location
}
