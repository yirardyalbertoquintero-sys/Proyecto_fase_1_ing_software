# Integración de las tres ramas a main

Fecha: 1 de noviembre de 2026. Yirardy autorizó pasar el trabajo de las tres ramas a main mediante pull requests. Esta decisión reemplaza el aplazamiento anterior de PR/main, conservado en los informes de cada corte. No incorpora autorización de alojamiento ni convierte las pruebas automatizadas en aceptación humana.

## Orden y procedencia

La comparación remota confirmó una cadena de ancestros: main → Yirardy_Quintero → Carlos_Borrell → Dylan_Ruiz. Cada rama posterior ya contenía la anterior; se integran en ese orden para mostrar únicamente los cambios restantes en cada PR.

| PR | Origen | Alcance |
| --- | --- | --- |
| [#4](https://github.com/yirardyalbertoquintero-sys/Proyecto_fase_1_ing_software/pull/4) | Yirardy_Quintero, a90020e | Base de acceso, catálogo, datos, diseño y organización documental. |
| [#5](https://github.com/yirardyalbertoquintero-sys/Proyecto_fase_1_ing_software/pull/5) | Carlos_Borrell, 937cf6d | Administración, historial, pagos/producción y solicitudes CU-05/CU-06 adelantadas por Yirardy. |
| [#6](https://github.com/yirardyalbertoquintero-sys/Proyecto_fase_1_ing_software/pull/6) | Dylan_Ruiz, aplicación validada 7a2e125 | CU-07/CU-08, migración de huella y consolidación de las instrucciones de integración. |

GitHub conserva el estado final y el commit de fusión de cada PR. Se utiliza merge normal, sin squash, rebase, force-push ni eliminación de ramas. Las contribuciones mantienen sus autores; la continuación de los bloques de Carlos y Dylan realizada por Yirardy no se atribuye ficticiamente a ellos.

## Validación

- La revisión independiente de otro asistente no encontró conflictos ni pérdida de commits exclusivos.
- Se repitió `./scripts/Test-Registro.ps1` en Release, con restore --locked-mode: **124 aprobadas, cero fallidas y cero omitidas**, incluidas 86 con SQL real. TRX local: `artifacts/pruebas/20261101T212134Z-8c97f5bc/resultado.trx`; huella y árboles de código en la [evidencia](../evidencias/integracion-main-20261101.json).
- El código integrado corresponde al commit 7a2e125 ya probado; el cierre añade documentación y normaliza una línea vacía final de un informe histórico, sin cambiar su contenido.
- Se comprueba tras la fusión que los árboles de main y Dylan coincidan, y que main contenga los commits de las tres ramas. Esta comprobación no requiere recrear bases ni reescribir datos del usuario.
- El DOCX aprobado conserva SHA-256 `9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`. Los 24 antecedentes del inventario conservan tamaño y huella; los Word anteriores son traslados sin modificación. El gitlink vacío retirado mantiene su referencia documentada.
- No se incluyen `.env.vscode`, conexiones por entorno, contraseñas, certificados ni `docs/seguimiento/Avances y pendientes.docx`. La base local del usuario y los archivos privados quedan fuera de Git.

## Continuación del equipo

Main es la base consolidada para revisar. Antes de nuevos cambios, partir de esa base en la rama personal correspondiente y volver a proponer un PR. No desarrollar simultáneamente sobre copias distintas del proyecto ni borrar las ramas para simular una integración.

Yirardy debe completar su revisión manual del recorrido real. Dylan realizará después la revisión cruzada y registrará observaciones y dictamen. Ambas permanecen pendientes de ejecución y evidencia; la revisión de IA no las sustituye. El informe Word 0.1 conserva el corte de ocho CU y debe consolidarse con los documentos 16/17 y las figuras posteriores antes de la entrega final. También siguen pendientes alojamiento, comprobaciones RNF/SMART y aceptación comercial. La integración del código no certifica una aplicación lista para operar con datos reales del negocio.
