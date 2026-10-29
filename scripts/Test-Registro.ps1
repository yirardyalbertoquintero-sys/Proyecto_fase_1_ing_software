param([string]$Servidor = 'localhost')
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path -Parent $PSScriptRoot
$conexionAnterior = $env:IMPACTOTEXTIL_PRUEBAS_SQL
try {
    $constructor = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $constructor['Data Source'] = $Servidor
    $constructor['Initial Catalog'] = 'master'
    $constructor['Integrated Security'] = $true
    $constructor['Encrypt'] = $true
    $constructor['TrustServerCertificate'] = $true
    $env:IMPACTOTEXTIL_PRUEBAS_SQL = $constructor.ConnectionString
    dotnet test (Join-Path $raizProyecto 'ImpactoTextil.slnx') --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw 'Una comprobacion del registro no se completo correctamente.' }
} finally {
    $env:IMPACTOTEXTIL_PRUEBAS_SQL = $conexionAnterior
}
