# Organización y unificación — 28 de octubre de 2026

## Carpeta única y rama

La aplicación continúa en `C:\proyectos\Proyecto_fase_1_ing_software`, rama `Yirardy_Quintero`. Se trasladó la rama mediante Git desde la carpeta adicional, sin copiar por encima del trabajo anterior. Git registra una sola carpeta de trabajo activa.

`C:\proyectos\Impacto_Textil_Yirardy` era un worktree, no un segundo repositorio independiente. Se liberó su rama, se activó en la carpeta original y se retiró el worktree mediante Git después de respaldarlo y comprobarlo. La referencia local de `main` se conserva en `c184594900a226cb68d506157e530fe1f2c242a5`.

## Preservación

Antes de organizar se copiaron ambas carpetas completas a `C:\Users\USER\Documents\Respaldos\ImpactoTextil\2026-10-28_c4aa35a3`. Se verificaron por SHA-256 los 258 archivos de la carpeta original y los 1,250 de la carpeta adicional. El respaldo contiene además un bundle de todas las referencias de Git, el estado previo y un inventario. Los resultados generados de compilación y otras salidas anteriores se conservan en ese respaldo y se regeneran en la ubicación nueva.

Se archivaron 23 archivos anteriores en `docs/archivo/`, con correspondencia de contenido comprobada contra las copias originales. Los archivos que estaban sin seguimiento en la carpeta original se preservan bajo `local-original/`; los Word históricos registrados se conservan bajo `documentacion-anterior/`. El gitlink anidado vacío, sin `.gitmodules`, se documentó con su modo y commit en `repositorio-anidado/REFERENCIA.md`; el historial y el respaldo conservan su procedencia.

El documento de requisitos aceptado continúa intacto en `docs/Impacto textil fase 1 y 2.docx`, SHA-256 `9178a494dc4ddaf62b7882d9d8203351ceb8260515c34f85e0988107ba424821`. Después de que el usuario cerrara Word se trasladó el ejemplar externo a `fuente-original/` dentro del respaldo y se comprobó su huella. En `C:\proyectos` queda únicamente la carpeta definitiva del proyecto.

Los PDF del docente y el plan del equipo se copiaron sin cambiar su contenido a `docs/guias/` y `docs/planes/`. Sus ejemplares originales de Descargas se conservan. `docs/INDICE.md` permite distinguir la documentación activa del archivo histórico.

## Configuración y comprobaciones

- Se conservaron las exclusiones de `.gitignore`: compilación, configuración privada, `.env.*`, archivos privados, resultados y salidas locales.
- `.gitattributes` conserva los antecedentes y el plan copiado sin normalizar sus finales de línea ni su espaciado original.
- La configuración local necesaria se trasladó y se regeneró `.env.vscode` en la carpeta definitiva; ambos están excluidos de Git.
- Las rutas activas de README, AGENTS, instrucciones de VS Code y CU-01 apuntan a la carpeta única. Las rutas históricas permanecen identificadas como tales.
- La preparación de VS Code y la migración se ejecutaron desde la ubicación nueva; la base `ImpactoTextil` ya estaba actualizada y conserva sus datos.
- Las 20 pruebas pasaron nuevamente, sin fallos ni omisiones. Incluyen las 29 comprobaciones del esquema sobre la migración.
- Se verificó que los enlaces locales activos existan y que el documento aceptado y los 23 antecedentes mantengan sus hashes.

## GitHub

La conexión del complemento GitHub devolvió 404 para el repositorio privado y Git local no disponía de una cuenta autenticada. Se inició el flujo oficial de acceso de Git Credential Manager, que requiere que el usuario complete su autenticación y autorización.

La actualización de referencias remotas y la subida están pendientes de ese acceso. No se declara que esta copia esté sincronizada con el `main` publicado. Cualquier envío se limitará a `Yirardy_Quintero`; no se autorizan cambios en `main` ni en las ramas de los compañeros.
