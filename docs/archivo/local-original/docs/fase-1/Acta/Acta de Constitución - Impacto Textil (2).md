<!-- Convertido automáticamente desde "Acta de Constitución - Impacto Textil (2).docx". Revisar contra el original. -->

UNIVERSIDAD NACIONAL DE INGENIERÍA

RECINTO UNIVERSITARIO SIMÓN BOLÍVAR

Ingeniería de Software I

ACTA DE CONSTITUCIÓN DEL PROYECTO

Sistema Web para Impacto Textil

## 1. Información general del proyecto

| Nombre del proyecto | Sistema Web para Impacto Textil |
|---|---|
| Curso | Ingeniería de Software I |
| Universidad | Universidad Nacional de Ingeniería (UNI) — RUSB |
| Integrantes | Yirardy · Dylan · Carlos |
| Responsable de este documento | Yirardy — Dirección del proyecto / Problemas y requerimientos |
| Negocio | Impacto Textil — confección de camisetas personalizadas y básicas por sublimación (Masatepe, Masaya) |
| Fecha | 27/08/2026 |

## 2. Antecedentes

Impacto Textil es un negocio familiar ubicado en Masatepe, Masaya, dedicado a la confección de camisetas personalizadas y básicas mediante la técnica de sublimación. Es atendido por dos propietarios: una a cargo de la selección de materia prima, corte, costura e inspección, y otro encargado del diseño, impresión, sublimación, acabados y empaque. El negocio recibe aproximadamente por mes entre 25 y 50 pedidos unitarios , además de pedidos grupales, y opera principalmente a través de WhatsApp Business y redes sociales (Instagram, Facebook).

El equipo de curso fue asignado a levantar una posible solución web para este negocio, dado que actualmente su proceso de recepción, registro y seguimiento de pedidos es completamente manual.

## 3. Situación problemática

- El registro de pedidos se lleva en un cuaderno físico, sin un formato estandarizado ni forma sencilla de consultar pedidos anteriores de un cliente.

- El registro de clientes es mínimo: solo se anota el nombre, sin datos de contacto ni historial accesible.

- No existe una forma automatizada de saber qué pedidos están pendientes, en producción o entregados; solo se identifica por la fecha de entrega anotada.

- El cálculo del precio es completamente manual, combinando costo de producción, comparación con la competencia y tipo de empaque.

- El inventario de materia prima no se controla de forma sistemática; se compra de manera reactiva cuando llega un pedido, lo que genera presión de tiempo en pedidos urgentes.

- No existe un catálogo digital fijo: los diseños se muestran de forma dispersa en publicaciones de redes sociales, lo que obliga al cliente a preguntar todo por WhatsApp.

## 4. Justificación

Una aplicación web resuelve directamente las limitaciones anteriores: centraliza el registro de pedidos y clientes en un solo lugar consultable (reemplazando el cuaderno), permite mostrar un catálogo digital permanente de diseños, tallas, colores y precios accesible desde el celular, y habilita el seguimiento del estado de cada pedido sin depender de la memoria o de revisar todo el cuaderno. Esto responde directamente a lo que el propio negocio expresó que le gustaría tener: un catálogo digital, una página con galería y contacto directo a WhatsApp, y a mediano plazo un sistema que reduzca la carga administrativa manual.

## 5. Objetivo general

Desarrollar una aplicación web para Impacto Textil que permita gestionar el catálogo de productos, el registro de pedidos y clientes, y el seguimiento del estado de cada pedido, reduciendo la dependencia del registro manual en cuaderno.

## 6. Objetivos específicos

- O.E.1: Implementar un catálogo digital de camisetas (diseños, tallas, colores y precios) accesible desde dispositivos móviles, disponible para la entrega del MVP.

- O.E.2: Desarrollar un módulo de registro de pedidos que capture cliente, características de la prenda, precio y fecha de entrega, sustituyendo el registro en cuaderno.

- O.E.3: Habilitar el seguimiento del estado de cada pedido (recibido, en producción, terminado, entregado) para reducir el tiempo de consulta manual.

- O.E.4: Registrar un historial de clientes (nombre, contacto y pedidos anteriores) consultable desde el sistema, inexistente en el proceso actual.

## 7. Alcance del MVP

Prioridad según lo expresado por el negocio: primero lo esencial para reemplazar el cuaderno y mostrar el catálogo; luego lo que ellos mismos definieron como "mediano plazo".

- Catálogo digital de productos (diseños, tallas, colores, precios)

- Formulario de contacto con enlace directo a WhatsApp Business

- Registro de pedidos (cliente, características, precio, fecha de entrega)

- Registro básico de clientes con historial de pedidos

- Seguimiento del estado del pedido (recibido, en producción, terminado, entregado)

## 8. Fuera del alcance

- Control automatizado de inventario de materia prima (mencionado por el negocio como necesidad a mediano plazo, no inmediata)

- Pasarela de pago en línea (actualmente los pagos son en efectivo, transferencia, depósito o tarjeta, gestionados fuera del sistema)

- Facturación electrónica formal (el negocio no emite factura salvo en pedidos de gran volumen)

## 9. Stakeholders

| Nombre / Rol | Interés en el proyecto | Influencia | Involucramiento |
|---|---|---|---|
| Propietarios (2): uno a cargo de corte/costura/inspección, otro de diseño/sublimación/empaque | Reducir la carga administrativa manual (cuaderno) y modernizar la atención al cliente | Alta | Directo (patrocinadoras) |
| Clientes de Impacto Textil | Cotizar y pedir camisetas de forma más rápida; ver catálogo sin depender solo de redes sociales | Media-alto | Usuario primario |
| Equipo de desarrollo (los 3) | Cumplir el proyecto de curso y entregar un sistema funcional | Alta | Directo |
| Docente / evaluador | Evaluar el cumplimiento de los artefactos de la Fase 1 | Media | Indirecto |

## 10. User Personas

### Persona 1

| Nombre | Aracelly Salamanca |
|---|---|
| Rol frente al sistema | Cliente que descubre Impacto Textil por recomendación y pide camisetas personalizadas |
| Edad / contexto | 43 años, pide camisetas de vez en cuando de 2-3 veces al año |
| Objetivo al usar el sistema | Ver diseños y precios rápido desde el celular, y cotizar sin tener que escribir primero por WhatsApp para preguntar precios básicos |
| Frustración actual | No hay un catálogo fijo donde ver todos los diseños y precios; tiene que buscar publicaciones viejas en redes o preguntar todo por WhatsApp |
| Cita representativa | “Quiero ver de una vez cuánto cuesta y cómo se ve, sin tener que escribir para preguntar todo” |

### Persona 2

| Nombre | Doña Rosa (propietaria) |
|---|---|
| Rol frente al sistema | Propietaria encargada de recibir, registrar y dar seguimiento a los pedidos |
| Edad / contexto | 42 años, administra el negocio junto a su esposo; hoy anota todo en un cuaderno |
| Objetivo al usar el sistema | Registrar pedidos y clientes sin perder información, y saber de un vistazo qué pedidos están pendientes y cuáles ya se entregaron |
| Frustración actual | El cuaderno no permite buscar fácilmente el historial de un cliente ni saber rápido qué pedidos faltan por entregar |
| Cita representativa | “A veces tengo que revisar en reiteradas ocasiones el cuaderno para encontrar un pedido que no haya sido confirmado” |

## 11. Restricciones

- Restricción de plataforma del curso: la solución debe ser 100% aplicación web (frontend + backend + base de datos).

- El negocio prefiere, al menos inicialmente, una opción de hosting gratuita antes de invertir en dominio propio.

- El negocio cuenta con un costo fijo de internet de C$1,350 mensuales; el sistema debe funcionar razonablemente bien sin asumir conexión de alta velocidad.

## 12. Supuestos

- Los propietarios están disponibles para validar avances y para una eventual capacitación breve en el uso del sistema.

- El negocio seguirá usando WhatsApp Business como canal de atención en paralelo al sistema, al menos durante la transición.

- El volumen de pedidos (25-50 camisetas/mes) se mantendrá relativamente estable durante el desarrollo del proyecto.

## 13. Riesgos iniciales

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Los propietarios sigan usando el cuaderno en paralelo al sistema por costumbre | Media | Alta | Capacitación breve y sistema tan simple como el cuaderno actual, o más |
| Dependencia de internet del local (costo fijo ya identificado, C$1,350/mes) | Baja | Media | Diseñar el sistema para funcionar bien incluso con conexión limitada |
| El negocio prioriza inicialmente una opción gratuita de hosting, lo que puede limitar funciones | Media | Media | Definir MVP compatible con hosting gratuito y dejar el resto para una fase posterior |
| Cambio de alcance a mitad de curso | Media | Alta | Congelar el alcance del MVP una vez aprobada esta Acta |

## 14. Criterios de éxito

- El negocio puede consultar el historial de un cliente sin revisar todo el cuaderno.

- El negocio puede identificar de un vistazo qué pedidos están pendientes, en producción o listos para entrega.

- Los clientes pueden ver el catálogo completo (diseños, tallas, colores, precios) sin necesidad de preguntar todo por WhatsApp.

- Las propietarias logran registrar un pedido nuevo en menos tiempo del que toma hoy con el cuaderno.
