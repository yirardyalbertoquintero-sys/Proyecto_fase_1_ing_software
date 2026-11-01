# Guía de revisión cruzada del bloque administrativo

Preparada el 1 de noviembre de 2026 para un integrante distinto de Yirardy, que no haya implementado el cambio revisado. Estado: **pendiente de ejecución y dictamen humano**. No se ha enviado una solicitud a terceros ni registrado una aprobación ficticia.

## Preparación

1. Revisar la rama `Carlos_Borrell` y anotar el resultado de `git rev-parse HEAD`. Coordinar con Yirardy si se usa su equipo; no cambiar de rama con archivos pendientes sin resguardo.
2. Usar una base local propia. Si se utiliza el equipo de Yirardy, no borrar ni reiniciar sus pedidos de demostración; preparar un entorno separado con ayuda del líder.
3. Seguir [ejecución en VS Code](../ejecutar-en-vscode.md) y [datos de demostración](../fase-3/15-administracion-pedidos.md). Crear un Administrador de prueba mediante el comando interactivo, sin credenciales en Git. La herramienta DEMO conserva códigos existentes; no restablece un escenario ya modificado.
4. Utilizar datos y archivos ficticios. Registrar captura, pasos y mensaje exacto si algo no coincide. No marcar un caso que no se haya ejecutado.

## Casos que debe ejecutar el compañero

| ID | Pasos | Resultado esperado |
| --- | --- | --- |
| RC-01 | En DEMO-ADMIN-01 aprobar el diseño, aceptar cotización de C$1,000 y rechazar una posterior de C$1,400. | Conservar el acuerdo de C$1,000 y las dos decisiones/versiones. |
| RC-02 | Descargar diseño original, versión y comprobante de DEMO-ADMIN-02. | Abrir los archivos del pedido correcto. En pedidos nuevos, el comprobante es legible y rotulado ficticio. |
| RC-03 | En DEMO-ADMIN-02 consultar saldo con Pago Pendiente, intentar producir y luego verificar ese mismo pago por C$500. | El pendiente no descuenta; el primer intento se deniega; verificar acredita una vez y no cambia automáticamente el estado. |
| RC-04 | Iniciar producción, autorizar sublimación y aceptar total de C$1,400; intentar autorizar de nuevo; abonar C$200 y comprobar otra vez. | Antes del abono faltan C$200; después puede autorizarse con versiones aprobadas. Se conservan pagos y estado. |
| RC-05 | Pasar a Listo para entrega, intentar entregar con saldo C$700, liquidar y confirmar recepción. | Bloqueo con saldo; después Entregado con fecha real y saldo cero. |
| RC-06 | Archivar DEMO-ADMIN-03 por desistimiento, consultar Archivada y recargar. | Mantener Pendiente/Archivada, motivo, responsable, fecha e historial; no permitir nuevas operaciones. |

## Registro que completa el revisor

Revisor: **pendiente**. Fecha: **pendiente**. Commit: **pendiente**. Entorno y base de prueba: **pendiente**.

| Caso | Ejecutado | Resultado observado | Captura o defecto |
| --- | --- | --- | --- |
| RC-01 | Pendiente | Pendiente | Pendiente |
| RC-02 | Pendiente | Pendiente | Pendiente |
| RC-03 | Pendiente | Pendiente | Pendiente |
| RC-04 | Pendiente | Pendiente | Pendiente |
| RC-05 | Pendiente | Pendiente | Pendiente |
| RC-06 | Pendiente | Pendiente | Pendiente |

Dictamen: **sin emitir**. El revisor indica aprobado, aprobado con observaciones o requiere correcciones, y explica el alcance. Yirardy registra la corrección y la repetición afectada. Esta guía no sustituye la prueba cliente→administrador con Dylan, ni las pruebas de carga, alojamiento o aceptación comercial.
