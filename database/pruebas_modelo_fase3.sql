/* Datos ficticios y comprobaciones del prototipo relacional.
   Se ejecuta mediante scripts/Test-Modelo.ps1 en una base temporal propia.
   No acredita funcionamiento de los CU, permisos HTTP ni pruebas de carga. */
SET NOCOUNT ON;
GO
CREATE PROCEDURE dbo.EsperarError
    @nombre nvarchar(120), @instruccion nvarchar(max), @codigo int, @alternativo int = NULL
AS
BEGIN
    BEGIN TRY
        EXEC sys.sp_executesql @instruccion;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> @codigo AND (@alternativo IS NULL OR ERROR_NUMBER() <> @alternativo)
            THROW;
        PRINT N'PASS: ' + @nombre;
        RETURN;
    END CATCH;
    THROW 52000, N'Falló la prueba: una operación inválida fue aceptada.', 1;
END;
GO
DECLARE @ahora datetimeoffset(0) = '2026-10-27T12:00:00-06:00';
-- Hash de formato ficticio: solo comprueba el contrato del esquema, no permite iniciar sesión.
DECLARE @hash varchar(60) = '$2b$12$' + REPLICATE('A', 53);
INSERT dbo.Usuario(nombre,correo,passwordHash,rol,activo,aceptacionTerminos,fechaAceptacion)
VALUES (N'Cliente de prueba',N'cliente@example.test',@hash,N'Cliente',1,1,@ahora),
       (N'Administrador de prueba',N'admin@example.test',@hash,N'Administrador',1,0,NULL);
DECLARE @cliente int = (SELECT id FROM dbo.Usuario WHERE correo=N'cliente@example.test');
DECLARE @admin int = (SELECT id FROM dbo.Usuario WHERE correo=N'admin@example.test');
EXEC dbo.EsperarError N'correo único sin distinguir mayúsculas',
    N'INSERT dbo.Usuario(nombre,correo,passwordHash,rol,activo,aceptacionTerminos,fechaAceptacion) SELECT nombre,N''CLIENTE@example.test'',passwordHash,rol,activo,aceptacionTerminos,fechaAceptacion FROM dbo.Usuario WHERE correo=N''cliente@example.test'';',2627;
EXEC dbo.EsperarError N'consentimiento obligatorio del Cliente',
    N'INSERT dbo.Usuario(nombre,correo,passwordHash,rol,activo,aceptacionTerminos) SELECT N''Otro'',N''otro@example.test'',passwordHash,N''Cliente'',1,0 FROM dbo.Usuario WHERE correo=N''cliente@example.test'';',547;

INSERT dbo.Producto(nombre,color,talla,tela,imagenURL,precioBase,activo)
VALUES(N'Camiseta de prueba',N'Blanco',N'M',N'Poliéster',N'/imagenes/prueba.png',150,1);
DECLARE @producto int = SCOPE_IDENTITY();
EXEC dbo.EsperarError N'precio de producto positivo', N'UPDATE dbo.Producto SET precioBase=0;',547;
INSERT dbo.Pedido(codigo,cliente,fechaOrden,modalidadEntrega,confirmacion,huellaSolicitud)
VALUES (N'PRUEBA-1',@cliente,@ahora,N'Retiro',NEWID(),HASHBYTES('SHA2_256','solicitud-1')),
       (N'PRUEBA-2',@cliente,@ahora,N'Retiro',NEWID(),HASHBYTES('SHA2_256','solicitud-2')),
       (N'PRUEBA-CENTAVO',@cliente,@ahora,N'Retiro',NEWID(),HASHBYTES('SHA2_256','solicitud-3'));
DECLARE @pedido int = (SELECT id FROM dbo.Pedido WHERE codigo=N'PRUEBA-1');
DECLARE @otro int = (SELECT id FROM dbo.Pedido WHERE codigo=N'PRUEBA-2');
DECLARE @centavo int = (SELECT id FROM dbo.Pedido WHERE codigo=N'PRUEBA-CENTAVO');
EXEC dbo.EsperarError N'confirmación repetida no crea otro pedido',
    N'INSERT dbo.Pedido(codigo,cliente,fechaOrden,modalidadEntrega,confirmacion,huellaSolicitud) SELECT N''DUPLICADO'',cliente,fechaOrden,modalidadEntrega,confirmacion,huellaSolicitud FROM dbo.Pedido WHERE codigo=N''PRUEBA-1'';',2627;
EXEC dbo.EsperarError N'domicilio exige dirección',
    N'UPDATE dbo.Pedido SET modalidadEntrega=N''Domicilio'' WHERE codigo=N''PRUEBA-1'';',547;

INSERT dbo.DetallePedido(pedido,producto,cantidad,talla,color,tela,empaque,ideaDiseno,precioBaseReferencia)
VALUES (@pedido,@producto,2,N'M',N'Blanco',N'Poliéster',N'Sin empaque',N'Sin personalización',150),
       (@otro,@producto,1,N'M',N'Blanco',N'Poliéster',N'Sin empaque',N'Sin personalización',150),
       (@centavo,@producto,1,N'M',N'Blanco',N'Poliéster',N'Sin empaque',N'Sin personalización',150);
DECLARE @detalle int=(SELECT id FROM dbo.DetallePedido WHERE pedido=@pedido);
DECLARE @otroDetalle int=(SELECT id FROM dbo.DetallePedido WHERE pedido=@otro);
DECLARE @detalleCentavo int=(SELECT id FROM dbo.DetallePedido WHERE pedido=@centavo);
IF EXISTS(SELECT 1 FROM dbo.vPedido WHERE id=@pedido AND (total IS NOT NULL OR anticipoExigible IS NOT NULL OR saldoPendiente IS NOT NULL OR totalReferencial<>300))
    THROW 52001,N'Los importes definitivos deben ser NULL antes del acuerdo.',1;
PRINT N'PASS: referencia 300 y valores definitivos pendientes';
EXEC dbo.EsperarError N'cantidad positiva',N'UPDATE dbo.DetallePedido SET cantidad=0;',547;

INSERT dbo.VersionDiseno(numeroVersion,detalle,descripcion,fechaRegistro,responsableRegistro)
VALUES(1,@detalle,N'Sin personalización',@ahora,@cliente),(1,@otroDetalle,N'Sin personalización',@ahora,@cliente);
DECLARE @diseno int=(SELECT id FROM dbo.VersionDiseno WHERE detalle=@detalle);
DECLARE @otroDiseno int=(SELECT id FROM dbo.VersionDiseno WHERE detalle=@otroDetalle);
EXEC dbo.EsperarError N'diseño pendiente no puede ser vigente',
    N'UPDATE d SET versionDisenoVigente=v.id FROM dbo.DetallePedido d JOIN dbo.VersionDiseno v ON v.detalle=d.id;',51002;
UPDATE dbo.VersionDiseno SET decision=N'Aprobada',fechaDecision=@ahora,responsableDecision=@admin,referenciaDecision=N'Constancia ficticia de prueba';
UPDATE dbo.DetallePedido SET versionDisenoVigente=@diseno WHERE id=@detalle;
EXEC dbo.EsperarError N'diseño de otra prenda denegado',
    N'UPDATE d SET versionDisenoVigente=(SELECT v.id FROM dbo.VersionDiseno v JOIN dbo.DetallePedido x ON x.id=v.detalle JOIN dbo.Pedido p ON p.id=x.pedido WHERE p.codigo=N''PRUEBA-2'') FROM dbo.DetallePedido d JOIN dbo.Pedido p ON p.id=d.pedido WHERE p.codigo=N''PRUEBA-1'';',51002;
EXEC dbo.EsperarError N'decisión de diseño inmutable',N'UPDATE dbo.VersionDiseno SET decision=N''Rechazada'';',51005;

INSERT dbo.VersionCotizacion(numeroVersion,pedido,fechaRegistro,responsableRegistro)
VALUES(1,@pedido,@ahora,@admin),(2,@pedido,@ahora,@admin),(1,@centavo,@ahora,@admin);
DECLARE @v1 int=(SELECT id FROM dbo.VersionCotizacion WHERE pedido=@pedido AND numeroVersion=1);
DECLARE @v2 int=(SELECT id FROM dbo.VersionCotizacion WHERE pedido=@pedido AND numeroVersion=2);
DECLARE @vCentavo int=(SELECT id FROM dbo.VersionCotizacion WHERE pedido=@centavo);
INSERT dbo.DetalleCotizacion(versionCotizacion,detalle,precioUnitarioAcordado,versionDiseno)
VALUES(@v1,@detalle,500,@diseno),(@v2,@detalle,700,@diseno),(@vCentavo,@detalleCentavo,100.01,NULL);
EXEC dbo.EsperarError N'cotización no incluye prendas ajenas',
    N'INSERT dbo.DetalleCotizacion(versionCotizacion,detalle,precioUnitarioAcordado) SELECT v.id,d.id,20 FROM dbo.VersionCotizacion v JOIN dbo.Pedido p ON p.id=v.pedido CROSS JOIN dbo.DetallePedido d JOIN dbo.Pedido otro ON otro.id=d.pedido WHERE p.codigo=N''PRUEBA-1'' AND v.numeroVersion=1 AND otro.codigo=N''PRUEBA-2'';',51003;
EXEC dbo.EsperarError N'cotización propuesta no puede ser vigente',
    N'UPDATE p SET versionCotizacionVigente=v.id FROM dbo.Pedido p JOIN dbo.VersionCotizacion v ON v.pedido=p.id WHERE p.codigo=N''PRUEBA-1'' AND v.numeroVersion=1;',51001;
UPDATE dbo.VersionCotizacion SET decision=N'Aceptada',fechaDecision=@ahora,responsableDecision=@admin,referenciaDecision=N'Aceptación ficticia' WHERE id IN(@v1,@vCentavo);
UPDATE dbo.Pedido SET versionCotizacionVigente=@v1 WHERE id=@pedido;
UPDATE dbo.Pedido SET versionCotizacionVigente=@vCentavo WHERE id=@centavo;
IF (SELECT anticipoExigible FROM dbo.vPedido WHERE id=@centavo)<>50.01
    THROW 52002,N'El anticipo no redondea hacia arriba al centavo.',1;
PRINT N'PASS: anticipo de 100.01 igual a 50.01';
EXEC dbo.EsperarError N'cotización vigente de otro pedido denegada',
    N'UPDATE dbo.Pedido SET versionCotizacionVigente=(SELECT versionCotizacionVigente FROM dbo.Pedido WHERE codigo=N''PRUEBA-1'') WHERE codigo=N''PRUEBA-2'';',51001;

INSERT dbo.Pago(pedido,monto,fecha,medio,referencia,comprobanteURL,confirmacion)
VALUES(@pedido,200,@ahora,N'Transferencia',N'RECHAZADO-1',N'privado/evidencia-prueba.png',NEWID());
DECLARE @rechazado int=SCOPE_IDENTITY();
IF (SELECT saldoPendiente FROM dbo.vPedido WHERE id=@pedido)<>1000 THROW 52003,N'Pendiente descontó saldo.',1;
PRINT N'PASS: evidencia pendiente no descuenta saldo';
UPDATE dbo.Pago SET estadoVerificacion=N'Rechazado',fechaVerificacion=@ahora,responsableVerificacion=@admin WHERE id=@rechazado;
IF (SELECT saldoPendiente FROM dbo.vPedido WHERE id=@pedido)<>1000 THROW 52004,N'Rechazado descontó saldo.',1;
PRINT N'PASS: rechazo conservado sin acreditar dinero';
EXEC dbo.EsperarError N'rechazo administrativo conserva su decisión',N'UPDATE dbo.Pago SET estadoVerificacion=N''Verificado'' WHERE estadoVerificacion=N''Rechazado'';',51011;
INSERT dbo.Pago(pedido,monto,fecha,medio,referencia,comprobanteURL,confirmacion)
VALUES(@pedido,500,@ahora,N'Transferencia',N'REF-500',N'privado/evidencia-500.png',NEWID());
DECLARE @pago int=SCOPE_IDENTITY();
UPDATE dbo.Pago SET estadoVerificacion=N'Verificado',fechaVerificacion=@ahora,responsableVerificacion=@admin WHERE id=@pago;
IF (SELECT saldoPendiente FROM dbo.vPedido WHERE id=@pedido)<>500 THROW 52005,N'La verificación calculó mal el saldo.',1;
PRINT N'PASS: el mismo pago verificado deja saldo 500';
EXEC dbo.EsperarError N'no reescribir un pago ya verificado',N'UPDATE dbo.Pago SET monto=600 WHERE estadoVerificacion=N''Verificado'';',51011;
EXEC dbo.EsperarError N'referencia verificada duplicada denegada',
    N'INSERT dbo.Pago(pedido,monto,fecha,medio,referencia,estadoVerificacion,fechaVerificacion,responsableVerificacion,confirmacion) SELECT pedido,monto,fecha,medio,referencia,estadoVerificacion,fechaVerificacion,responsableVerificacion,NEWID() FROM dbo.Pago WHERE estadoVerificacion=N''Verificado'';',2601,2627;
EXEC dbo.EsperarError N'pago sin pedido denegado',
    N'INSERT dbo.Pago(pedido,monto,fecha,medio,referencia,comprobanteURL,confirmacion) VALUES(-1,10,SYSDATETIMEOFFSET(),N''Efectivo'',N''X'',N''privado/x.png'',NEWID());',547;

UPDATE dbo.Pedido SET estado=N'En producción' WHERE id=@pedido;
UPDATE dbo.VersionCotizacion SET decision=N'Aceptada',fechaDecision=@ahora,responsableDecision=@admin,referenciaDecision=N'Aumento ficticio aceptado' WHERE id=@v2;
UPDATE dbo.Pedido SET versionCotizacionVigente=@v2 WHERE id=@pedido;
IF NOT EXISTS(SELECT 1 FROM dbo.vPedido WHERE id=@pedido AND total=1400 AND anticipoExigible=700 AND saldoPendiente=900 AND estado=N'En producción')
    THROW 52006,N'La cotización superior no recalculó los importes esperados.',1;
IF (SELECT total FROM dbo.vVersionCotizacion WHERE id=@v1)<>1000 THROW 52007,N'Se perdió la cotización histórica.',1;
PRINT N'PASS: aumento 1000 a 1400 conserva pagos y estado; faltan 200 de anticipo';
EXEC dbo.EsperarError N'no sobrescribir importes de una versión',N'UPDATE dbo.DetalleCotizacion SET precioUnitarioAcordado=1;',51004;
EXEC dbo.EsperarError N'no modificar una decisión de cotización',N'UPDATE dbo.VersionCotizacion SET decision=N''Rechazada'' WHERE decision=N''Aceptada'';',51007;
UPDATE dbo.Producto SET precioBase=999,activo=0 WHERE id=@producto;
IF NOT EXISTS(SELECT 1 FROM dbo.vPedido WHERE id=@pedido AND totalReferencial=300 AND total=1400)
    THROW 52008,N'El catálogo alteró los importes históricos.',1;
PRINT N'PASS: cambiar y desactivar catálogo conserva referencia y cotización';
EXEC dbo.EsperarError N'cantidad original conserva la fotografía de todas las cotizaciones',N'UPDATE dbo.DetallePedido SET cantidad=cantidad+1;',51015;
IF (SELECT COUNT(*) FROM dbo.Constancia WHERE tipo=N'Cotización' AND pedido=@pedido)<>2
    THROW 52009,N'No se exponen ambas constancias de cotización.',1;
PRINT N'PASS: constancias de ambas versiones accesibles sin duplicar decisiones';
EXEC dbo.EsperarError N'archivo administrativo incompatible con producción',
    N'UPDATE dbo.Pedido SET condicionAdministrativa=N''Archivada'' WHERE codigo=N''PRUEBA-1'';',547;
EXEC dbo.EsperarError N'no eliminar producto con historial',N'DELETE FROM dbo.Producto;',547;
PRINT N'FIN: comprobaciones del prototipo relacional completadas.';
GO
