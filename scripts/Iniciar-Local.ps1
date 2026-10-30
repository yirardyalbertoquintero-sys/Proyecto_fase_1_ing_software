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
    # Una segunda depuracion bloquea la DLL y no puede usar los mismos puertos.
    $dllDepuracion = Join-Path $proyectoApi 'bin\Debug\net10.0\ImpactoTextil.Api.dll'
    $exeDepuracion = Join-Path $proyectoApi 'bin\Debug\net10.0\ImpactoTextil.Api.exe'
    $instanciasAbiertas = @(Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe' OR Name = 'ImpactoTextil.Api.exe'" |
        Where-Object {
            ($_.CommandLine -and $_.CommandLine.IndexOf($dllDepuracion, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -or
            [string]::Equals($_.ExecutablePath, $exeDepuracion, [System.StringComparison]::OrdinalIgnoreCase)
        })
    if ($instanciasAbiertas.Count -gt 0) {
        throw 'Impacto Textil ya esta abierto en Debug. Deten TODAS sus sesiones con el boton cuadrado o Shift+F5; si se inicio en una terminal, usa Ctrl+C. Despues pulsa F5 una sola vez.'
    }
    $puertosOcupados = @([System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners() |
        Where-Object { $_.Port -in @(7105, 5138) } | Select-Object -ExpandProperty Port -Unique)
    if ($puertosOcupados.Count -gt 0) {
        throw ('No se puede iniciar Impacto Textil: los puertos ' + ($puertosOcupados -join ', ') + ' estan ocupados. Deten la ejecucion anterior o identifica el programa que los usa antes de volver a pulsar F5.')
    }
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
    dotnet build $proyectoApi --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion. Revisa los errores anteriores; si la DLL esta bloqueada, deten las sesiones de depuracion antes de volver a ejecutar. No se han aplicado migraciones en esta preparacion.' }
    dotnet ef database update --project $proyectoApi --no-build
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
