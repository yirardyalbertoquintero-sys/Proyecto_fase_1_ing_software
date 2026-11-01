# Correcciones del trabajo preparatorio de Carlos

Fecha: 30 de octubre de 2026. Yirardy autorizó realizar este trabajo directamente en `Carlos_Borrell`. El SRS aprobado conserva su huella `9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`.

## Historial y alcance

El commit raíz de Carlos, `3d99eb1`, contenía servicios preparatorios pero no compartía ancestros con `264d78a` de Yirardy. El commit de integración `a398e47` conserva ambos como padres. No reescribe el commit de Carlos ni modifica main, Dylan o Yirardy. La operación fue deliberada, con resolución individual de los cuatro conflictos, no una sustitución automática de carpetas.

Se conservaron los servicios originales de Carlos. Se recuperaron el arranque que detecta ejecuciones duplicadas, sus mensajes de compilación/SQL separados, las tareas y las instrucciones de la base de Yirardy. `.gitignore` mantiene las reglas generales de Carlos y añade las exclusiones específicas de secretos, archivos privados y resultados. `.env.vscode` salió del índice sin borrar el archivo local; su versión histórica usa autenticación integrada sin contraseña y permanece en el historial conservado.

Los cambios locales sin confirmar de Yirardy se resguardaron antes de cambiar de rama. Este cierre no incorpora esos borradores ni el Word personal al commit de Carlos.

## Correcciones del código

| Área | Resultado |
| --- | --- |
| Imágenes privadas | Se requiere el archivo completo, se comprueba el cierre y se decodifica JPG/PNG con StbImageSharp. Una cabecera aislada o contenido truncado no basta. |
| PDF privado | Lectura estructural con PDFsharp 6.2.4, cierre EOF y al menos una página; se rechazan documentos falsos/truncados/no legibles. No es un análisis antimalware ni una verificación comercial del comprobante. |
| Originales | El archivo se mueve del temporal al destino sin recodificarlo. Se conservan todos sus bytes. La descarga valida y vuelve a situar el flujo en el inicio. |
| Fallos y cancelación | Se retira únicamente el temporal de esa operación al fallar. Se comprueba cancelación después de decodificar y antes de mover el archivo o devolver el flujo. |
| Importes JSON | Los decimales salen como cadenas exactas, como precioBase en el catálogo. Las entradas aceptan texto o número; IDs/cantidades continúan siendo números y los valores pendientes continúan siendo null. |
| Expediente | Cabecera, pagos e historiales se leen dentro de una transacción Serializable, tomando primero el mismo bloqueo padre que los escritores. No requiere alterar tablas ni habilitar Snapshot en SQL. Su comportamiento con cambios concurrentes debe comprobarse en la etapa de integración SQL. |

### Recursos y dependencia añadida

El máximo del SRS sigue siendo inclusivo: **10,000,000 bytes por archivo**. La decodificación incorpora además un presupuesto técnico de **12 millones de píxeles por imagen**, equivalente al empleado por el catálogo para acotar memoria. Este umbral adicional no figura como regla comercial aprobada en el SRS: queda identificado para revisión del equipo antes de conectar la carga real con Dylan y comprobar el alojamiento. Una imagen que lo supere se rechaza con un mensaje específico; el servidor no la reduce ni altera silenciosamente. El almacén limita la validación a un archivo a la vez para contener recursos.

Se fijó PDFsharp **6.2.4**, cuya licencia MIT consta en el paquete y en la [ficha de esa versión](https://www.nuget.org/packages/PDFsharp/6.2.4). No se actualiza automáticamente a otra versión/licencia. Los archivos packages.lock.json de API y pruebas se regeneraron y se verificó la restauración con `--locked-mode`. El texto de licencia distribuido está en THIRD_PARTY_NOTICES.md. No se añadió un framework de frontend ni se cambió el motor, SDK o alojamiento.

## Comprobaciones ejecutadas

- Compilación Release de la solución: correcta, sin advertencias ni errores.
- **18 pruebas nuevas aprobadas, 0 fallidas y 0 omitidas**, filtradas a archivos privados y serialización; no utilizan SQL ni servicios de pagos.
- Casos: JPG/PNG/PDF reales; firmas falsas y archivos truncados; archivo vacío; rechazo de PDF como diseño; conservación exacta de originales; PNG válido de exactamente 10,000,000 bytes y rechazo de un byte adicional; archivo alterado en disco; rutas externas y raíz pública denegadas; presupuesto de píxeles; centavos grandes, IDs numéricos y valores pendientes.
- La primera ejecución detectó que PDFsharp usa Exception base para ciertos errores xref; se convirtió ese caso en un rechazo de archivo comprensible y se repitió la comprobación corregida.
- Evidencia final local: `artifacts/revision-carlos-20261030/final/correcciones-carlos.trx`. No se versiona el TRX.
- Auditoría NuGet transitiva de esta fecha: sin vulnerabilidades conocidas reportadas por los orígenes consultados. No garantiza resultados futuros.
- Revisión independiente de IA: observaciones de cancelación incorporadas; no sustituye revisión humana.

Comando ejecutado:

```powershell
dotnet test tests/ImpactoTextil.Api.Tests/ImpactoTextil.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~ArchivosPrivadosTests|FullyQualifiedName~FormatoImportesAdminTests' --logger 'trx;LogFileName=correcciones-carlos.trx' --results-directory artifacts/revision-carlos-20261030/final
```

Las 51 pruebas de la base tienen su resultado anterior documentado. No se repitió la suite SQL completa en este cierre y no se suman esos resultados históricos para afirmar una ejecución nueva de 69 pruebas.

## Pendientes conservados por decisión del líder

CU-10/CU-11/CU-12/CU-13 siguen en preparación: no se registran los servicios en Program.cs ni se añaden controladores o pantallas aquí. Se mantienen aplazados los contratos con Dylan, nuevas migraciones, pruebas funcionales de pagos/concurrencia SQL, main, alojamiento e informe final. La política de entrega con saldo cero continúa identificada como propuesta académica pendiente de validación comercial.

Antes de abrir esos módulos al usuario, verificar identidad desde la sesión y permisos en servidor, CSRF, carga/descarga autorizada, concurrencia y respuestas HTTP con el modelo existente. Las pruebas de formatos de este cierre no acreditan esas funciones completas.
