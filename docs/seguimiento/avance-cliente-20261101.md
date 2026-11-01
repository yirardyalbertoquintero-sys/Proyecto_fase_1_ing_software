# Avance de solicitudes del cliente — 1 de noviembre de 2026

Yirardy autorizó adelantar trabajo del reparto original de Dylan. Se completaron localmente CU-05 y CU-06 en Carlos_Borrell, conservando la fuente de requisitos y los avances anteriores. No se modificó la rama de Dylan; el nombre de la rama de trabajo tampoco cambia la autoría de Yirardy. El [documento 16](../fase-3/16-solicitudes-cliente.md) contiene ejecución desde VS Code, contrato, decisiones y límites.

## Entrega de este bloque

- Pantalla responsive enlazada al catálogo y a Mi sesión, con acceso Cliente dentro de la propia página.
- Carrito solo en memoria, archivos sin subida anticipada, cantidad/variante, idea o imagen, opción Sin personalización, Retiro/Domicilio y confirmación del importe de referencia.
- Bolsa estándar como única opción para solicitudes nuevas, por aclaración expresa del líder; sin alterar históricos ni el Word aprobado.
- API/servicio/Repository/SQL conectados: solicitud completa, titularidad, revalidación por variante, prevención de duplicados, versiones iniciales Pendientes, almacenamiento privado y recuperación de fallos.
- Integración observada con el expediente administrativo existente: listado real, detalle, versiones y descarga original exacta. Sin cambiar el esquema ni añadir dependencias.
- Quince nuevas pruebas SQL y recorrido de navegador con API/SQL reales. Resultado final y huellas en [evidencia](../evidencias/solicitudes-cliente-20261101.json).

La revisión por un segundo asistente encontró dos problemas que se corrigieron: exigir concordancia de extensión/contenido y vincular cada confirmación a la cuenta que preparó el carrito. El navegador detectó además el recibo de un reintento con escala decimal mayor que dos; se normalizó la salida a centavos y se añadió la comprobación del JSON. Preparar otra solicitud se bloquea mientras termina el envío anterior. El recorrido posterior pasó sin errores JavaScript.

## Siguiente trabajo concreto

| Pendiente | Resultado que debe entregarse |
| --- | --- |
| CU-07 | Cliente aporta el comprobante privado a su propio pedido, se crea Pago Pendiente, no baja el saldo, se controla tamaño/contenido y duplicación; CU-13 revisa ese mismo Pago sin crear otro. Pantalla/API/SQL y pruebas de titularidad y concurrencia. |
| CU-08 | Cliente consulta únicamente sus pedidos y expediente permitido: estado, cotización/diseño, importes y condición administrativa conforme al SRS. Aislar datos y archivos ajenos por servidor. Pantalla/API/SQL y pruebas. |
| Revisión funcional humana | Yirardy recorre el nuevo formulario con prendas activas reales de la demostración y registra sus resultados; otra persona realiza la revisión cruzada independiente. Las pruebas de IA no sustituyen esas aceptaciones. |
| Consolidación | Incorporar el módulo cliente y sus clases al informe/figuras finales. El Word versión 0.1 conserva el corte previo de ocho CU. Revisar contratos reales con cualquier aporte posterior de Dylan antes de integrarlo. |
| Integración y defensa | Revisar diferencias y evidencias antes del PR. PR/main siguen aplazados. Alojamiento, validación de cuotas, configuración real, aceptación comercial y fecha de defensa siguen pendientes. |

El corte pasa de ocho a diez CU implementados localmente de los doce previstos. Los seis CU principales del criterio académico tienen implementación local, pero esto por sí solo no certifica la entrega docente, el cierre del diseño completo, la revisión humana ni un producto listo para operar en el negocio. La fase 3 del curso corresponde al diseño y la fase 4 a construcción/pruebas; el avance de código se está realizando junto a sus documentos de diseño.
