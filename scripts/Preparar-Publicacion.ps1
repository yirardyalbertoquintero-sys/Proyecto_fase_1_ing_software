param(
    [string]$ServidorPruebas = 'localhost',
    [switch]$PermitirCambiosLocales
)
$ErrorActionPreference = 'Stop'
$raizProyecto = Split-Path -Parent $PSScriptRoot
$conexionPruebasAnterior = $env:IMPACTOTEXTIL_PRUEBAS_SQL
$conexionDisenoAnterior = $env:ConnectionStrings__ImpactoTextil
Push-Location $raizProyecto
try {
    $commit = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo identificar la version del repositorio.' }
    $rama = (git branch --show-current).Trim()
    $rutasConstruccion = @('src','tests','scripts','docs/despliegue','docs/FUENTE_VIGENTE.md','docs/Impacto textil fase 1 y 2.docx','ImpactoTextil.slnx','Directory.Build.props','global.json','dotnet-tools.json','.gitignore')
    $cambios = @(git status --porcelain -- @rutasConstruccion)
    if ($cambios.Count -gt 0 -and !$PermitirCambiosLocales) {
        throw 'Hay cambios de construccion sin guardar en Git. Guarda el avance o usa -PermitirCambiosLocales para un ensayo identificado como tal.'
    }
    $fuente = Join-Path $raizProyecto 'docs/Impacto textil fase 1 y 2.docx'
    $huellaFuente = (Get-FileHash -LiteralPath $fuente -Algorithm SHA256).Hash.ToLowerInvariant()
    $referenciaFuente = Get-Content -LiteralPath (Join-Path $raizProyecto 'docs/FUENTE_VIGENTE.md') -Raw
    if ($referenciaFuente -notmatch [regex]::Escape($huellaFuente)) { throw 'La fuente vigente no coincide con su huella documentada.' }
    $idPaquete = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '-' + $commit.Substring(0,7) + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
    $salida = Join-Path $raizProyecto ('artifacts/entregas/' + $idPaquete)
    if (Test-Path -LiteralPath $salida) { throw 'La carpeta del paquete ya existe.' }
    [void](New-Item -ItemType Directory -Path $salida)
    $pendiente = Join-Path $salida 'NO-PUBLICAR.txt'
    [IO.File]::WriteAllText($pendiente,'Preparacion incompleta. No utilizar esta carpeta como entrega.')
    $rutaTrx = Join-Path $salida 'pruebas'
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'No se restauraron las herramientas fijadas.' }
    dotnet restore ImpactoTextil.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'No se restauraron las dependencias fijadas.' }
    $conexionPruebas = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $conexionPruebas['Data Source'] = $ServidorPruebas
    $conexionPruebas['Initial Catalog'] = 'master'
    $conexionPruebas['Integrated Security'] = $true
    $conexionPruebas['Encrypt'] = $true
    $servidorLocal = ($ServidorPruebas -split '[,\\]')[0]
    $conexionPruebas['TrustServerCertificate'] = @('.','localhost','127.0.0.1','(local)',$env:COMPUTERNAME) -contains $servidorLocal
    $env:IMPACTOTEXTIL_PRUEBAS_SQL = $conexionPruebas.ConnectionString
    dotnet test ImpactoTextil.slnx -c Release --no-restore --logger 'trx;LogFileName=resultado.trx' --results-directory $rutaTrx
    if ($LASTEXITCODE -ne 0) { throw 'Una prueba fallo. No se genero un paquete listo para publicar.' }
    [xml]$trx = Get-Content -LiteralPath (Join-Path $rutaTrx 'resultado.trx') -Raw
    $contadores = $trx.TestRun.ResultSummary.Counters
    if ([int]$contadores.total -lt 1 -or [int]$contadores.failed -ne 0 -or [int]$contadores.notExecuted -ne 0 -or
        [int]$contadores.passed -ne [int]$contadores.total) { throw 'Existen pruebas fallidas u omitidas. No se prepara la entrega.' }
    $app = Join-Path $salida 'app'
    dotnet publish src/backend/ImpactoTextil.Api -c Release --no-build --no-restore -o $app
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar la publicacion.' }
    $exigidos = @('web.config','ImpactoTextil.Api.dll','ImpactoTextil.Api.runtimeconfig.json','wwwroot/index.html','wwwroot/catalogo.html','wwwroot/catalogo.js','wwwroot/administracion.html')
    foreach ($archivo in $exigidos) {
        if (!(Test-Path -LiteralPath (Join-Path $app $archivo) -PathType Leaf)) { throw "Falta un archivo requerido: $archivo" }
    }
    $archivosApp = @(Get-ChildItem -LiteralPath $app -Recurse -File)
    foreach ($archivo in $archivosApp) {
        $relativa = $archivo.FullName.Substring($app.Length + 1).Replace([IO.Path]::DirectorySeparatorChar,[char]47)
        if ($relativa -match '(^|/)(storage|\.git|database)(/|$)' -or $archivo.Name -match '^appsettings\..*\.json$|^\.env|\.pfx$|\.publishsettings$|\.pubxml(\.user)?$') {
            throw "Se detecto un archivo que no debe publicarse: $relativa"
        }
    }
    $configuracion = Get-Content -LiteralPath (Join-Path $app 'appsettings.json') -Raw | ConvertFrom-Json
    if ($configuracion.PSObject.Properties.Name -contains 'ConnectionStrings') { throw 'La publicacion contiene conexiones en appsettings. Usa configuracion externa.' }
    $baseDatos = Join-Path $salida 'database'
    [void](New-Item -ItemType Directory -Path $baseDatos)
    # La fabrica de diseno exige una conexion para seleccionar SQL Server; generar el script no lo ejecuta.
    $conexionDiseno = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($conexionPruebas.ConnectionString)
    $conexionDiseno['Initial Catalog'] = 'ImpactoTextil_Diseno_Publicacion'
    $env:ConnectionStrings__ImpactoTextil = $conexionDiseno.ConnectionString
    dotnet ef migrations script --idempotent --project src/backend/ImpactoTextil.Api --configuration Release --no-build -o (Join-Path $baseDatos 'actualizar.sql')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar el SQL desde las migraciones.' }
    $sdk = (dotnet --version).Trim()
    if ((git rev-parse HEAD).Trim() -ne $commit) { throw 'El commit cambio durante la preparacion. Repite desde una version estable.' }
    $cambiosFinales = @(git status --porcelain -- @rutasConstruccion)
    if ($cambiosFinales.Count -gt 0 -and !$PermitirCambiosLocales) { throw 'Aparecieron cambios de construccion durante la preparacion.' }
    $manifiesto = [ordered]@{
        formato = 1; creadoUtc = [DateTime]::UtcNow.ToString('o'); commit = $commit; rama = $rama
        cambiosLocales = ($cambios.Count -gt 0 -or $cambiosFinales.Count -gt 0); sdk = $sdk; framework = 'net10.0'
        fuenteSha256 = $huellaFuente; pruebas = [ordered]@{ total = [int]$contadores.total; aprobadas = [int]$contadores.passed; fallidas = 0; omitidas = 0 }
        alcance = @('CU-01','CU-02','CU-04','CU-09'); alojamientoValidado = $false
    }
    [IO.File]::WriteAllText((Join-Path $salida 'manifiesto.json'), ($manifiesto | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
    Copy-Item -LiteralPath (Join-Path $raizProyecto 'docs/despliegue/INSTALAR-MONSTERASP.md') -Destination (Join-Path $salida 'LEEME-INSTALAR.md')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($app, (Join-Path $salida 'sitio.zip'))
    $inventario = @(Get-ChildItem -LiteralPath $salida -Recurse -File | Where-Object { $_.FullName -ne $pendiente } | ForEach-Object {
        [pscustomobject]@{ archivo = $_.FullName.Substring($salida.Length + 1).Replace([IO.Path]::DirectorySeparatorChar,[char]47); bytes = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    })
    $inventario | Export-Csv -LiteralPath (Join-Path $salida 'SHA256.csv') -NoTypeInformation -Encoding UTF8
    Remove-Item -LiteralPath $pendiente
    Write-Output "Paquete preparado: $salida"
    Write-Output 'sitio.zip contiene solo la aplicacion. SQL, manifiesto y pruebas se conservan fuera de la raiz web.'
    if ($manifiesto.cambiosLocales) { Write-Output 'ENSAYO CON CAMBIOS LOCALES: el commit no representa por si solo todos los archivos del paquete.' }
} finally {
    $env:IMPACTOTEXTIL_PRUEBAS_SQL = $conexionPruebasAnterior
    $env:ConnectionStrings__ImpactoTextil = $conexionDisenoAnterior
    Pop-Location
}
