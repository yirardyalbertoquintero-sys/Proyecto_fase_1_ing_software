param([string]$Servidor = 'localhost')
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path -Parent $PSScriptRoot
$baseTemporal = 'ImpactoTextil_Prueba_' + [Guid]::NewGuid().ToString('N')
$creadaPorEstaEjecucion = $false
function Invoke-SqlPrueba([string[]]$ArgumentosSql) {
    & sqlcmd -S $Servidor -E -C -I -l 15 -t 60 -b -r 1 -f 65001 @ArgumentosSql
    if ($LASTEXITCODE -ne 0) { throw "Falló sqlcmd (código $LASTEXITCODE)." }
}
try {
    Invoke-SqlPrueba -ArgumentosSql @('-d','master','-Q',"CREATE DATABASE [$baseTemporal];")
    $creadaPorEstaEjecucion = $true
    Invoke-SqlPrueba -ArgumentosSql @('-d',$baseTemporal,'-i',(Join-Path $raizProyecto 'database/001_modelo_fase3.sql'))
    Invoke-SqlPrueba -ArgumentosSql @('-d',$baseTemporal,'-i',(Join-Path $raizProyecto 'database/pruebas_modelo_fase3.sql'))
} finally {
    if ($creadaPorEstaEjecucion -and $baseTemporal -match '^ImpactoTextil_Prueba_[a-f0-9]{32}$') {
        Invoke-SqlPrueba -ArgumentosSql @('-d','master','-Q',"ALTER DATABASE [$baseTemporal] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$baseTemporal];")
        Write-Output 'Base temporal creada por esta ejecución eliminada.'
    }
}
