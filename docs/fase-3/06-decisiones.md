# Decisiones del diseño y asuntos pendientes

| ID | Decisión propuesta por Yirardy | Motivo y estado |
| --- | --- | --- |
| D-01 | DOCX único conservado y huella registrada. | Instrucción expresa del líder; aplicado. |
| D-02 | MonsterASP.NET Free como destino. | Coincide con el SRS; publicación pendiente de cuenta y prueba. |
| D-03 | Una aplicación ASP.NET Core con frontend del mismo origen. | Sigue la propuesta de 5.2 y 6.3; revisar con el equipo. |
| D-04 | Totales, saldo y anticipo en vistas o consultas calculadas. | Evita copias divergentes; probado en SQL. |
| D-05 | Detalle original inmutable y cantidad recuperada por referencia en cada cotización. | Preserva la fotografía y evita dependencia parcial; probado y pendiente de revisión conjunta. |
| D-06 | Constancia lógica de cotización proyectada desde su decisión; entrega persistida aparte. | Conserva los campos del SRS sin duplicar decisiones; probado y pendiente de revisión conjunta. |
| D-07 | Dos relaciones para registrar contexto de una comprobación de sublimación. | Materializa el registro de 4.6; no agrega estado ni CU. Servicio pendiente. |
| D-08 | Clave de confirmación y hash semántico para CU-06. | Prevención de duplicados solicitada por RF-05; unicidad probada, flujo HTTP pendiente. |
| D-09 | .NET 10, SDK 10.0.402 y paquetes fijados en los proyectos. | Compatible con el entorno local observado; falta validación del alojamiento. |
| D-10 | Fuente SQL de fase 3 como prototipo de integridad. | No compite con migraciones definitivas: después de revisar el diseño, EF Core se convierte en la secuencia de cambios y se genera desde ella el SQL de distribución. |

Los siguientes puntos ya están identificados como propuestas o pendientes en el SRS y no se presentan como reglas comerciales confirmadas: política de contraseña, duración de sesión, opciones exactas de empaque, límites añadidos de texto, máximo de detalles y tamaño total de una petición, y autorización comercial de entrega con saldo cero. Las longitudes propuestas se usan para hacer comprobable el prototipo y deben ratificarse antes de fijar el contrato final.

La contraseña se validará antes de aplicar BCrypt. El diseño de esa validación debe considerar también el límite de entrada en bytes de la biblioteca elegida, no solo contar caracteres; no se implementa truncamiento silencioso. El registro real esperará esa decisión.

El plan docente exige 80 % de los CU principales; el objetivo sigue siendo construir los seis principales y sus apoyos hasta completar los doce previstos. La referencia académica se registra al consolidar las fases 3 y 4. La fecha de defensa sigue pendiente de confirmación.
