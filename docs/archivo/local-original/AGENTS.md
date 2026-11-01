# Impacto Textil – Proyecto de Ing. de Software I (UNI)

App web de pedidos de camisetas personalizadas por sublimación.

## Fuente de verdad
- Los requisitos están en /docs (SRS, backlog, matriz RF/RNF, casos de uso, diccionario de datos).
- No inventes funcionalidades fuera del SRS. Si algo falta o es ambiguo, pregunta antes de asumir.

## Stack (no cambiar)
- Frontend: HTML, CSS y JavaScript puro, responsive (escritorio y móvil).
- Backend: C# (.NET) con Entity Framework Core, patrón Repository y DTOs.
- Base de datos: SQL Server, normalizada hasta 3FN.
- Hosting: Somee.

## Reglas de trabajo
- Trabaja un caso de uso a la vez, completo: base de datos, API, pantalla y prueba.
- Usa los nombres de entidades y campos exactamente como están en el diccionario de datos.
- Valida todas las entradas. Nunca muestres errores crudos del servidor al usuario.
- Contraseñas siempre con hash, nunca en texto plano. No subas secretos al repositorio
  (cadenas de conexión, claves): usa variables de entorno o archivos ignorados por git.
- Commits pequeños con formato: feat(CU-03): descripción corta.
- Explica brevemente lo que generas: el autor debe poder defender cada parte del código.

## Estructura
- /docs: documentación de las Fases 1 y 2 y plan de construcción.
- /database: scripts SQL (esquema y datos de prueba).
- /src/backend: API en C#.
- /src/frontend: HTML, CSS y JS.
- /.agents/skills: skills del proyecto (SKILL.md).

## Documento vigente
- La ÚNICA fuente de verdad es docs/fase-1-y-2-ing-de-software.md (convertido de "Fase 1 y 2 ing de software.docx").
- IGNORA por completo: "Fase 1 ing software-4.docx", "Fase1_Proyecto software.docx", cualquier archivo docs/anterior_*.md y la carpeta repetida Proyecto_fase_1_ing_software/.
- Si algo no está en el documento vigente, pregunta; no lo tomes de otro archivo. 