param([string]$Servidor = 'localhost')
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path -Parent $PSScriptRoot
$conexionPrevia = $env:ConnectionStrings__ImpactoTextil
$entornoPrevio = $env:ASPNETCORE_ENVIRONMENT
Push-Location $raizProyecto
try {
    Write-Host 'Se agregarán hasta tres pedidos FICTICIOS a la base local ImpactoTextil. Se conservarán los existentes.'
    $respuestaDemo = Read-Host 'Escribe DEMO para continuar'
    if ($respuestaDemo -cne 'DEMO') { Write-Host 'No se modificaron datos.'; return }
    $constructor = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $constructor['Data Source'] = $Servidor
    $constructor['Initial Catalog'] = 'ImpactoTextil'
    $constructor['Integrated Security'] = $true
    $constructor['Encrypt'] = $true
    $constructor['TrustServerCertificate'] = $true
    $env:ConnectionStrings__ImpactoTextil = $constructor.ConnectionString
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --project src/backend/ImpactoTextil.Api -c Release --no-launch-profile -- --crear-pedidos-demostracion
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo completar la preparación de los pedidos ficticios.' }
} finally {
    $env:ConnectionStrings__ImpactoTextil = $conexionPrevia
    $env:ASPNETCORE_ENVIRONMENT = $entornoPrevio
    Pop-Location
}
