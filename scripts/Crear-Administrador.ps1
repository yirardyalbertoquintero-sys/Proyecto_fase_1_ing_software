param([string]$Servidor = 'localhost')
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path -Parent $PSScriptRoot
Push-Location $raizProyecto
try {
    $constructor = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $constructor['Data Source'] = $Servidor
    $constructor['Initial Catalog'] = 'ImpactoTextil'
    $constructor['Integrated Security'] = $true
    $constructor['Encrypt'] = $true
    $constructor['TrustServerCertificate'] = $true
    $env:ConnectionStrings__ImpactoTextil = $constructor.ConnectionString
    dotnet run --project src/backend/ImpactoTextil.Api --no-launch-profile -- --crear-administrador
    if ($LASTEXITCODE -ne 0) { throw 'No se completo la inicializacion del Administrador.' }
} finally { Pop-Location }
