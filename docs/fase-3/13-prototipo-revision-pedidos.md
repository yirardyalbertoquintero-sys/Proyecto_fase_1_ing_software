# CU-10: prototipo navegable de revisión y cotización

Fecha: 30 de octubre de 2026. Responsable de continuación: Yirardy, según [reparto actualizado](../planes/REPARTO_ACTUAL.md). Rama de trabajo: Carlos_Borrell, sobre los servicios corregidos de 162064a y el reparto incorporado mediante abfe263.

## Estado y motivo

La Fase 2 del DOCX vigente, CU-10 y RF-08/RF-06, define listado de solicitudes, revisión de prendas, cotizaciones, decisiones y conservación de versiones. Se prepara su navegación mientras Dylan publica CU-05/CU-06 y el líder mantiene aplazados contratos compartidos y migraciones.

Este avance **no completa CU-10**: no conecta los servicios administrativos con controladores ni guarda pedidos/acuerdos. No añade contratos HTTP de pedidos, migra bases, registra pagos, autoriza producción ni descarga originales. Sirve para revisar interfaz y coherencia antes de conectar las dependencias.

## Abrirlo en esta rama

1. Guardar el trabajo propio antes de cambiar de versión y seleccionar Carlos_Borrell. No reemplazar carpetas completas ni descartar cambios locales.
2. Ejecutar el proyecto con F5 como indica [VS Code](../ejecutar-en-vscode.md). Detener todas las depuraciones anteriores si hay DLL o puertos ocupados.
3. Iniciar sesión como Administrador. Desde **Gestionar catálogo**, pulsar **Pedidos · prototipo**.
4. Leer el aviso permanente **Datos ficticios · Sin guardado real**. Abrir DEMO-001 o DEMO-002; las acciones identificadas como simulación solo alteran memoria de la página. Recargar o usar **Reiniciar demostración** descarta esa simulación.

La única consulta de red nueva de la pantalla es GET /api/sesion, ya existente. Visitante/Cliente no muestran el espacio de demostración. Un fallo de consulta informa que no se pudo comprobar la sesión; no lo presenta como un error de permisos. Este control inicial de presentación no sustituye la autorización de los futuros endpoints ni convierte datos ficticios en datos privados reales.

## Navegación disponible

- Búsqueda y filtros por los cuatro estados y condición administrativa Activa/Archivada/Todas.
- Expediente ficticio con modalidad, dirección cuando corresponde, fecha acordada, prendas, empaque e idea original. Talla, color, tela y cantidad originales no se editan.
- Referencia inicial separada de cotización aceptada; valores definitivos pendientes cuando no existe acuerdo.
- Historial de cotizaciones con número de versión, importes, decisión, referencia y metadatos ficticios de fecha/responsable. Las propuestas/rechazos no sustituyen el acuerdo vigente y las decisiones resueltas no se reescriben.
- Nuevas versiones de diseño por descripción, aprobación/rechazo ficticios e historial conservado. Una revisión Pendiente no desplaza la anterior Aprobada.
- Aceptación simulada con referencia y fecha obligatorias. No cambia el estado de producción ni modifica los pagos ficticios existentes.
- Vista del anticipo del 50 % con redondeo al centavo superior y advertencia por insuficiencia. DEMO-002 permite visualizar un aumento de C$1,000 a C$1,400 con C$500 verificados: faltan C$200 de anticipo y sigue En producción. No se registra ningún abono.
- Archivo administrativo simulado de la solicitud Pendiente sin acuerdos aceptados/cobros verificados, conservando estado e historial. Estos ejemplos no incluyen evidencias de pago Pendientes; no acreditan resolver ese flujo.

Los precios se calculan en centavos con BigInt y se formatean como texto, sin convertir dinero a Number. Las entradas libres y referencias se insertan con textContent/value. Los nombres, direcciones, acuerdos, importes, fechas y responsables son ficticios; el prototipo no atribuye aprobaciones a clientes o empleados reales.

## Verificación realizada

- Sintaxis JavaScript comprobada con node --check.
- Compilación Release de la solución correcta, sin errores ni advertencias; frontend incluido por la configuración existente.
- Diez recorridos de interfaz verificados con Chrome sin interfaz gráfica, en servidor estático local e interceptando la consulta de sesión con identidades ficticias: denegación de Visitante/Cliente; fallo 503; referencia/fecha; versiones de diseño; contenido HTML inerte; propuestas/rechazos y centavos; vista del aumento; móvil; archivo/filtros; navegación desde catálogo y ausencia de errores o peticiones de escritura.
- Se revisaron capturas de escritorio 1440 px y móvil 320 px. No hay desplazamiento horizontal.
- Evidencia local de QA: artifacts/cu10-prototipo/resultado.json. Las capturas ficticias están en [escritorio](../evidencias/prototipo-pedidos-escritorio.png) y [móvil](../evidencias/prototipo-pedidos-movil-320.png).
- Revisión de IA independiente sin bloqueantes. La aceptación humana continúa pendiente.

Estas comprobaciones **no utilizan SQL ni los servicios de pagos**, no equivalen a pruebas HTTP/SQL de CU-10 y no acreditan la autorización operativa de pedidos. No se repitió la suite SQL histórica de la base en este avance.

## Siguiente conexión pendiente

Cuando estén disponibles las solicitudes de Dylan y los contratos acordados, reemplazar los ejemplos por consultas reales y conectar alta/decisión de cotizaciones y diseños al backend existente. El servidor debe obtener al Administrador desde la sesión, validar permisos/CSRF/entradas, conservar decisiones y responder de forma consistente ante fallos. Completar originales y descarga con CU-11. Ejecutar después pruebas reales de permisos, persistencia, duplicados y concurrencia según el orden autorizado; no declarar el caso terminado por disponer de esta pantalla.
