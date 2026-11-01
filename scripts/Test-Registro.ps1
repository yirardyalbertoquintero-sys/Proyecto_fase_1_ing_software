param([string]$Servidor = 'localhost')
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path -Parent $PSScriptRoot
$conexionAnterior = $env:IMPACTOTEXTIL_PRUEBAS_SQL
$identificadorEjecucion = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$rutaResultados = Join-Path $raizProyecto ('artifacts/pruebas/' + $identificadorEjecucion)
Push-Location $raizProyecto
try {
    $constructor = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $constructor['Data Source'] = $Servidor
    $constructor['Initial Catalog'] = 'master'
    $constructor['Integrated Security'] = $true
    $constructor['Encrypt'] = $true
    $constructor['TrustServerCertificate'] = $true
    $env:IMPACTOTEXTIL_PRUEBAS_SQL = $constructor.ConnectionString
    dotnet restore ImpactoTextil.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron restaurar las dependencias fijadas para las pruebas.' }
    dotnet test ImpactoTextil.slnx --no-restore --configuration Release --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=resultado.trx' --results-directory $rutaResultados
    if ($LASTEXITCODE -ne 0) { throw 'Una comprobacion de la aplicacion no se completo correctamente.' }
    $rutaTrx = Join-Path $rutaResultados 'resultado.trx'
    if (-not (Test-Path -LiteralPath $rutaTrx -PathType Leaf)) { throw 'No se genero la evidencia TRX de las pruebas.' }
    [xml]$trx = Get-Content -LiteralPath $rutaTrx -Raw
    $contadores = $trx.TestRun.ResultSummary.Counters
    if ([int]$contadores.total -lt 1 -or [int]$contadores.failed -ne 0 -or [int]$contadores.notExecuted -ne 0 -or
        [int]$contadores.passed -ne [int]$contadores.total) {
        throw 'La comprobacion requiere todas las pruebas aprobadas, sin fallos ni omisiones. Revisa resultado.trx.'
    }
    Write-Output ('PASS: ' + $contadores.passed + ' pruebas aprobadas; 0 fallidas; 0 omitidas. Configuracion: Release')
    Write-Output ('Evidencia local: ' + $rutaTrx)
} finally {
    $env:IMPACTOTEXTIL_PRUEBAS_SQL = $conexionAnterior
    Pop-Location
}
