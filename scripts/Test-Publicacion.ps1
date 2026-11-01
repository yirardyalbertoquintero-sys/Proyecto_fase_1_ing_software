param(
    [Parameter(Mandatory=$true)][string]$UrlBase,
    [string]$UrlHttp,
    [string]$Salida
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$raizProyecto = Split-Path -Parent $PSScriptRoot
$base = $null
if (![Uri]::TryCreate($UrlBase,[UriKind]::Absolute,[ref]$base) -or $base.Scheme -ne 'https' -or
    $base.UserInfo -or $base.Query -or $base.Fragment -or $base.AbsolutePath -ne '/') {
    throw 'Indica la raiz HTTPS del sitio, sin credenciales, parametros ni subcarpetas.'
}
if (!$UrlHttp) { $UrlHttp = 'http://' + $base.DnsSafeHost + '/' }
$http = $null
if (![Uri]::TryCreate($UrlHttp,[UriKind]::Absolute,[ref]$http) -or $http.Scheme -ne 'http' -or
    $http.DnsSafeHost -ne $base.DnsSafeHost -or $http.UserInfo -or $http.Query -or $http.Fragment -or $http.AbsolutePath -ne '/') {
    throw 'La URL HTTP debe ser la raiz del mismo servidor, sin credenciales.'
}
if (!$Salida) { $Salida = Join-Path $raizProyecto ('artifacts/comprobaciones/' + [Guid]::NewGuid().ToString('N') + '.json') }
$salidaReal = [IO.Path]::GetFullPath($Salida)
if (Test-Path -LiteralPath $salidaReal) { throw 'El informe de salida ya existe; selecciona otro nombre.' }
$manejador = New-Object Net.Http.HttpClientHandler
$manejador.AllowAutoRedirect = $false
$cliente = New-Object -TypeName Net.Http.HttpClient -ArgumentList $manejador
$cliente.Timeout = [TimeSpan]::FromSeconds(20)
$cliente.MaxResponseContentBufferSize = 1MB
$cliente.DefaultRequestHeaders.UserAgent.ParseAdd('ImpactoTextil-Comprobacion/1.0')
$resultados = New-Object 'System.Collections.Generic.List[object]'
function Consultar([string]$Ruta, [Uri]$Raiz, [bool]$LeerCuerpo=$true) {
    $modo = [Net.Http.HttpCompletionOption]::ResponseHeadersRead
    if ($LeerCuerpo) { $modo = [Net.Http.HttpCompletionOption]::ResponseContentRead }
    $respuesta = $cliente.GetAsync((New-Object -TypeName Uri -ArgumentList $Raiz,$Ruta),$modo).GetAwaiter().GetResult()
    try {
        $cuerpo = ''
        if ($LeerCuerpo) {
            # ResponseContentRead limita tiempo y tamano tambien mientras se recibe el cuerpo.
            $cuerpo = $respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        }
        return [pscustomobject]@{ estado=[int]$respuesta.StatusCode; cuerpo=$cuerpo
            ubicacion=$respuesta.Headers.Location; sinCache=($respuesta.Headers.CacheControl -and $respuesta.Headers.CacheControl.NoStore)
            nosniff=($respuesta.Headers.Contains('X-Content-Type-Options') -and ($respuesta.Headers.GetValues('X-Content-Type-Options') -join ',') -eq 'nosniff') }
    } finally { $respuesta.Dispose() }
}
function Comprobar([string]$Nombre,[scriptblock]$Prueba) {
    try {
        $correcta = [bool](& $Prueba)
        $resultados.Add([pscustomobject]@{ comprobacion=$Nombre; aprobada=$correcta })
        if ($correcta) { Write-Output "PASS: $Nombre" } else { Write-Output "FAIL: $Nombre" }
    } catch {
        # No registrar cuerpos, tokens, conexiones ni respuestas de un servidor externo.
        $resultados.Add([pscustomobject]@{ comprobacion=$Nombre; aprobada=$false })
        Write-Output "FAIL: $Nombre (no se pudo completar la consulta)"
    }
}
try {
    Comprobar 'Certificado HTTPS y pantalla de registro' { $r=Consultar '/' $base; $r.estado -eq 200 -and $r.cuerpo.Contains('formulario-registro') -and $r.nosniff }
    Comprobar 'Pantalla del catalogo publico' { $r=Consultar '/catalogo.html' $base; $r.estado -eq 200 -and $r.cuerpo.Contains('lista-prendas') }
    Comprobar 'JavaScript del catalogo publicado' { $r=Consultar '/catalogo.js' $base; $r.estado -eq 200 -and $r.cuerpo.Contains('/api/productos') }
    Comprobar 'Conexion SQL disponible' { $r=Consultar '/api/disponibilidad' $base; $r.estado -eq 200 -and ($r.cuerpo | ConvertFrom-Json).estado -eq 'Disponible' }
    Comprobar 'Consulta del esquema y catalogo real' {
        $r=Consultar '/api/productos' $base
        if ($r.estado -ne 200 -or !$r.sinCache) { return $false }
        $null = ConvertFrom-Json -InputObject $r.cuerpo
        $r.cuerpo.TrimStart().StartsWith('[') -and $r.cuerpo.TrimEnd().EndsWith(']')
    }
    Comprobar 'Sesion privada denegada al visitante' { $r=Consultar '/api/sesion' $base; $r.estado -eq 401 }
    Comprobar 'Administracion denegada al visitante' { $r=Consultar '/api/administracion/productos' $base; $r.estado -eq 401 }
    foreach ($ruta in @('/appsettings.json','/appsettings.Local.json','/.env','/.git/config','/database/actualizar.sql','/storage/catalogo/no-publicar.json')) {
        $rutaActual = $ruta
        Comprobar "Ruta no publica $rutaActual" { $r=Consultar $rutaActual $base $false; $r.estado -eq 404 -or $r.estado -eq 403 }
    }
    Comprobar 'HTTP redirige al mismo sitio HTTPS' {
        $r=Consultar '/' $http $false
        if ($r.estado -notin @(301,302,307,308) -or !$r.ubicacion) { return $false }
        $destino = New-Object -TypeName Uri -ArgumentList $http,$r.ubicacion
        $destino.Scheme -eq 'https' -and $destino.DnsSafeHost -eq $base.DnsSafeHost -and $destino.Port -eq $base.Port
    }
} finally {
    $cliente.Dispose(); $manejador.Dispose()
}
$fallidas = @($resultados | Where-Object { !$_.aprobada }).Count
$informe = [ordered]@{ creadoUtc=[DateTime]::UtcNow.ToString('o'); sitio=$base.GetLeftPart([UriPartial]::Authority)
    tipo='Comprobacion publica de lectura'; aprobadas=$resultados.Count-$fallidas; fallidas=$fallidas
    resultados=@($resultados.ToArray()); limites=@('No prueba escrituras, reinicios ni descargas privadas autorizadas.','No acredita RNF de carga, rendimiento o disponibilidad mensual.') }
[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($salidaReal))
[IO.File]::WriteAllText($salidaReal,($informe | ConvertTo-Json -Depth 6),(New-Object Text.UTF8Encoding($false)))
Write-Output "Informe: $salidaReal"
if ($fallidas -gt 0) { throw 'La publicacion tiene comprobaciones pendientes o fallidas. Revisa el informe.' }
