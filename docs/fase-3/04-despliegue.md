# Despliegue previsto y validación técnica

## Topología propuesta

El navegador utiliza el subdominio HTTPS que se habilite en MonsterASP.NET Free. La aplicación ASP.NET Core publica HTML, CSS y JavaScript y atiende la API. La aplicación se conecta a SQL Server y guarda diseños y comprobantes en una ubicación privada permitida por el alojamiento. No se fija Nginx, Apache, CDN ni otro componente por copiar los ejemplos de la guía: se documentará la infraestructura realmente utilizada.

El diagrama de despliegue distingue la estación de desarrollo del alojamiento objetivo. La disponibilidad real de .NET, SQL Server, HTTPS y almacenamiento privado debe verificarse con una cuenta del proveedor antes de declarar el despliegue terminado.

## Entorno local observado

- SDK instalado: .NET 10.0.402.
- Motor consultado: SQL Server 17.0.1000.7, Standard Developer Edition, instancia local predeterminada.
- El documento propone Express para desarrollo; esta prueba utiliza el motor SQL Server disponible sin funciones exclusivas de edición. No se afirma que se haya instalado Express.
- El esquema se ensaya en una base temporal con nombre aleatorio que el propio script elimina al terminar.

## Configuración de la aplicación

La aplicación lee `ConnectionStrings__ImpactoTextil` desde el entorno, o el secreto equivalente del proveedor. No existe una cadena real en appsettings del repositorio. Para la futura persistencia privada se acordará una variable como `Almacenamiento__RaizPrivada`, fuera del contenido servido públicamente. Ese componente aún no está implementado.

El entorno publicado usará Production y HTTPS. En desarrollo se puede emplear el certificado local de dotnet. La aceptación de certificados de prueba en las comprobaciones locales no debe trasladarse al certificado público del alojamiento.

## Secuencia de publicación

1. Obtener acceso a la cuenta y al sitio de MonsterASP.NET autorizado para el proyecto.
2. Confirmar runtime .NET 10, características del SQL Server, límites de archivos y una ruta privada persistente.
3. Configurar la base y la conexión como secreto; acordar y probar el esquema o la migración que se publicará.
4. Generar el paquete con `dotnet publish` y registrar el commit exacto.
5. Publicar el paquete mediante el mecanismo ofrecido por el proveedor y activar HTTPS.
6. Comprobar `/api/disponibilidad` sobre HTTPS y después los recorridos implementados.
7. Probar archivo privado, descarga autorizada y recuperación tras reinicio; confirmar que no quedan accesos públicos directos.
8. Registrar la evidencia y el procedimiento de reversión del paquete y de los cambios de datos. No asumir que revertir código revierte una base de datos.

## Estado real

La publicación en MonsterASP.NET, HTTPS público y archivos privados en ese proveedor están pendientes: esta entrega no dispone de una cuenta o sitio configurado para efectuar esas operaciones. El servicio HTTP y la conectividad local se comprueban por separado y no sustituyen esa prueba.

RNF-02 requiere medir transferencias de archivos completas; RNF-05 requiere el ensayo concurrente acordado; RNF-06 necesita un mes de observación. Ninguno se marca cumplido por compilar o responder al endpoint de disponibilidad.
