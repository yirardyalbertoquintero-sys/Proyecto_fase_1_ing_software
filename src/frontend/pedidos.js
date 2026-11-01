"use strict";
(() => {
    const posicionPestana = window.crearPosicionPestana();
    const base = "/api/administracion/pedidos";
    const $ = (id) => document.getElementById(id);
    let pedidos = [],
        seleccionado = null,
        expediente = null,
        ocupado = false,
        secuencia = 0,
        secuenciaLista = 0,
        entradaId = 0;
    let autorizado = false,
        identidadId = null,
        validandoAcceso = false;
    const n = (tipo, texto, clase) => {
        const e = document.createElement(tipo);
        if (texto !== undefined) e.textContent = texto;
        if (clase) e.className = clase;
        return e;
    };
    const centavos = (valor) => {
        const t = String(valor ?? "0");
        if (!/^\d+(\.\d{1,2})?$/.test(t))
            throw new Error("Importe recibido no válido.");
        const [a, b = ""] = t.split(".");
        return BigInt(a) * 100n + BigInt(b.padEnd(2, "0"));
    };
    const moneda = (valor) => {
        if (valor === null || valor === undefined) return "Sin acuerdo";
        const c = centavos(valor);
        return (
            "C$" +
            (c / 100n).toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",") +
            "." +
            (c % 100n).toString().padStart(2, "0")
        );
    };
    const fecha = (valor) =>
        valor ? new Date(valor).toLocaleString("es-NI") : "Sin registrar";
    function mensaje(texto, error = false) {
        const e = $("resultado-pedidos");
        e.textContent = texto;
        e.className = error ? "error" : "";
        e.hidden = false;
        e.focus({ preventScroll: posicionPestana.pendiente });
    }
    function limpiarExpedientes() {
        ++secuencia;
        ++secuenciaLista;
        pedidos = [];
        seleccionado = null;
        expediente = null;
        $("expediente").replaceChildren();
        $("lista-pedidos").replaceChildren();
        $("estado-lista").textContent = "";
    }
    function denegar() {
        autorizado = false;
        identidadId = null;
        limpiarExpedientes();
        $("gestion").hidden = true;
        $("acceso-denegado").hidden = false;
    }
    async function api(ruta, opciones = {}) {
        let respuesta;
        try {
            respuesta = await fetch(ruta, {
                credentials: "same-origin",
                cache: "no-store",
                ...opciones,
            });
        } catch (error) {
            if (error.name === "AbortError") throw error;
            throw new Error(
                "No pudimos comunicarnos con la aplicación. Comprueba la conexión y actualiza el expediente antes de reintentar.",
            );
        }
        if (respuesta.status === 401 || respuesta.status === 403) {
            denegar();
            throw new Error(
                "La sesión expiró o ya no tiene permisos. Vuelve a iniciar sesión.",
            );
        }
        if (!respuesta.ok) {
            let detalle;
            try {
                detalle = await respuesta.json();
            } catch {
                detalle = {};
            }
            const errores = detalle.errors
                ? Object.values(detalle.errors).flat().join(" ")
                : "";
            throw new Error(
                errores ||
                    detalle.title ||
                    "No pudimos completar la operación. Actualiza el expediente antes de reintentar.",
            );
        }
        return respuesta.json();
    }
    async function enviarEscritura(pedidoId, ruta, datos, archivo = false) {
        const token = await api("/api/seguridad/token");
        const control = new AbortController();
        const limite = setTimeout(() => control.abort(), 30000);
        try {
            return await api(base + "/" + pedidoId + "/" + ruta, {
                method: "POST",
                signal: control.signal,
                headers: archivo
                    ? { "X-CSRF-TOKEN": token.token }
                    : {
                          "X-CSRF-TOKEN": token.token,
                          "Content-Type": "application/json",
                      },
                body: archivo ? datos : JSON.stringify(datos),
            });
        } catch (error) {
            if (error.name === "AbortError" || error instanceof TypeError)
                throw new Error(
                    "No pudimos confirmar la respuesta. Actualiza el expediente para comprobar si se guardó. Conserva los datos del cobro al reintentar.",
                );
            throw error;
        } finally {
            clearTimeout(limite);
        }
    }
    function boton(texto, accion) {
        const b = n("button", texto, "boton-secundario");
        b.type = "button";
        b.addEventListener("click", accion);
        return b;
    }
    function campo(
        formulario,
        etiqueta,
        nombre,
        tipo = "text",
        valor = "",
        obligatorio = true,
        maximo = 250,
    ) {
        const contenedor = n("div", undefined, "campo");
        const id = "entrada-" + ++entradaId;
        const l = n("label", etiqueta);
        l.htmlFor = id;
        const i = n(
            tipo === "textarea"
                ? "textarea"
                : tipo === "select"
                  ? "select"
                  : "input",
        );
        i.id = id;
        i.name = nombre;
        if (i.tagName === "INPUT") i.type = tipo;
        i.required = obligatorio;
        if (tipo === "text" || tipo === "textarea") i.maxLength = maximo;
        if (tipo === "select") {
            for (const v of valor) {
                const o = n("option", v);
                o.value = v;
                i.append(o);
            }
        } else i.value = valor;
        contenedor.append(l, i);
        formulario.append(contenedor);
        return i;
    }
    function formulario(contenedor, texto, preparar, accion) {
        const pedidoFormulario = expediente.pedido.id;
        const f = n("form");
        preparar(f);
        const b = n("button", texto, "boton-secundario");
        b.type = "submit";
        f.append(b);
        f.addEventListener("submit", async (e) => {
            e.preventDefault();
            if (ocupado || !f.reportValidity()) return;
            if (pedidoFormulario !== seleccionado) {
                mensaje(
                    "Selecciona nuevamente el pedido antes de guardar.",
                    true,
                );
                return;
            }
            ocupado = true;
            ++secuencia;
            const controles = [
                ...$("gestion").querySelectorAll(
                    "button,input,select,textarea",
                ),
            ];
            const estados = controles.map((c) => c.disabled);
            controles.forEach((c) => (c.disabled = true));
            try {
                await accion(f);
                mensaje("Operación confirmada.");
                try {
                    await cargarExpediente(pedidoFormulario);
                    await cargarLista();
                } catch {
                    mensaje(
                        "La operación se confirmó, pero no pudimos actualizar la vista. Pulsa Actualizar pedidos.",
                        true,
                    );
                }
            } catch (error) {
                mensaje(
                    error.message || "No pudimos completar la operación.",
                    true,
                );
            } finally {
                ocupado = false;
                controles.forEach((c, i) => {
                    if (c.isConnected) c.disabled = estados[i];
                });
                for (const c of $("gestion").querySelectorAll(
                    "button,input,select,textarea",
                ))
                    if (!controles.includes(c)) c.disabled = false;
            }
        });
        contenedor.append(f);
        return f;
    }
    function seccion(titulo) {
        const s = n("section");
        s.append(n("h3", titulo));
        $("expediente").append(s);
        return s;
    }
    function descargar(contenedor, texto, ruta) {
        const a = n("a", texto);
        a.href = base + "/" + seleccionado + "/" + ruta;
        a.setAttribute("download", "");
        contenedor.append(a);
    }
    function lista() {
        const buscar = $("buscar").value.trim().toLocaleLowerCase("es");
        const visibles = pedidos.filter((p) =>
            (p.codigo + " " + p.clienteNombre)
                .toLocaleLowerCase("es")
                .includes(buscar),
        );
        $("lista-pedidos").replaceChildren();
        $("estado-lista").textContent = visibles.length
            ? `${visibles.length} pedidos.`
            : "No hay pedidos que coincidan. Las solicitudes se mostrarán cuando estén registradas.";
        for (const p of visibles) {
            const b = boton(p.codigo, () => abrir(p.id));
            b.className = "pedido-lista";
            b.disabled = ocupado;
            b.setAttribute("aria-current", String(p.id === seleccionado));
            b.append(
                n("span", p.clienteNombre),
                n("span", p.estado + " · " + p.condicionAdministrativa),
            );
            $("lista-pedidos").append(b);
        }
    }
    async function cargarLista() {
        const turno = ++secuenciaLista;
        const q = new URLSearchParams({
            estado: $("estado").value,
            condicionAdministrativa: $("condicion").value,
        });
        const datos = await api(base + "?" + q);
        if (turno !== secuenciaLista || !autorizado) return;
        pedidos = datos;
        lista();
    }
    async function abrir(id) {
        if (ocupado || !autorizado) return;
        const turno = ++secuencia;
        seleccionado = id;
        $("expediente").replaceChildren(n("p", "Cargando expediente…"));
        try {
            const dato = await api(base + "/" + id);
            if (turno !== secuencia) return;
            expediente = dato;
            dibujar();
            lista();
            history.replaceState(null, "", "?pedido=" + id);
        } catch (error) {
            if (turno === secuencia) {
                $("expediente").replaceChildren(
                    n(
                        "p",
                        "No pudimos consultar el expediente. Selecciona el pedido para reintentar.",
                    ),
                );
                mensaje(error.message, true);
            }
        }
    }
    async function cargarExpediente(id) {
        const turno = ++secuencia;
        const datos = await api(base + "/" + id);
        if (turno !== secuencia || id !== seleccionado || !autorizado) return;
        expediente = datos;
        dibujar();
    }
    function dibujar() {
        const x = expediente,
            p = x.pedido,
            editable = p.condicionAdministrativa === "Activa",
            revisable =
                editable && ["Pendiente", "En producción"].includes(p.estado);
        const escribir = (ruta, datos, archivo = false) =>
            enviarEscritura(p.id, ruta, datos, archivo);
        $("expediente").replaceChildren(n("h2", p.codigo));
        $("expediente").append(
            n("p", `${p.clienteNombre} · ${p.clienteCorreo}`),
            n(
                "p",
                `${p.estado} · ${p.condicionAdministrativa} · ${p.modalidadEntrega}`,
            ),
        );
        if (x.direccionEntrega)
            $("expediente").append(n("p", "Dirección: " + x.direccionEntrega));
        $("expediente").append(
            n(
                "p",
                "Entrega acordada: " +
                    (p.fechaEntregaAcordada || "Sin registrar") +
                    " · Entrega real: " +
                    fecha(p.fechaEntregaReal),
            ),
        );
        const importes = n("dl", undefined, "resumen-importes");
        for (const [etiqueta, valor] of [
            ["Referencia", p.totalReferencial],
            ["Total aceptado", p.total],
            ["Verificado", x.pagosVerificadosAcumulados],
            ["Anticipo mínimo", p.anticipoExigible],
            ["Saldo", p.saldoPendiente],
        ]) {
            const d = n("div");
            d.append(n("dt", etiqueta), n("dd", moneda(valor)));
            importes.append(d);
        }
        $("expediente").append(importes);
        if (
            p.anticipoExigible !== null &&
            centavos(x.pagosVerificadosAcumulados) <
                centavos(p.anticipoExigible)
        ) {
            const c =
                centavos(p.anticipoExigible) -
                centavos(x.pagosVerificadosAcumulados);
            $("expediente").append(
                n(
                    "p",
                    "Faltan " +
                        moneda(
                            c / 100n +
                                "." +
                                (c % 100n).toString().padStart(2, "0"),
                        ) +
                        " verificados para cubrir el anticipo. La sublimación también exige aprobación vigente.",
                    "alerta-pedido",
                ),
            );
        }
        if (x.motivoArchivo)
            $("expediente").append(
                n(
                    "p",
                    `Archivo: ${x.motivoArchivo} · ${fecha(x.fechaArchivo)} · Responsable ${x.responsableArchivo}`,
                ),
            );
        const prendas = seccion("Prendas y versiones de diseño");
        for (const d of x.detalles) {
            const a = n("article");
            a.append(
                n("h4", `Prenda ${d.id} · ${d.cantidad} unidades`),
                n("p", `${d.talla} · ${d.color} · ${d.tela} · ${d.empaque}`),
                n("p", d.observaciones || ""),
                n("p", d.ideaDiseno || ""),
            );
            if (d.tieneArchivoOriginal)
                descargar(
                    a,
                    "Descargar diseño original",
                    `detalles/${d.id}/diseno-original`,
                );
            for (const v of x.versionesDiseno.filter(
                (v) => v.detalle === d.id,
            )) {
                const h = n("details");
                h.open =
                    v.id === d.versionDisenoVigente ||
                    v.decision === "Pendiente";
                h.append(
                    n(
                        "summary",
                        `Diseño v${v.numeroVersion} · ${v.decision}${v.id === d.versionDisenoVigente ? " · Vigente" : ""}`,
                    ),
                    n("p", v.descripcion || "Diseño con archivo"),
                    n(
                        "p",
                        `Registro: ${fecha(v.fechaRegistro)} · Responsable ${v.responsableRegistro}`,
                    ),
                );
                if (v.tieneArchivo)
                    descargar(
                        h,
                        "Descargar esta versión",
                        `detalles/${d.id}/disenos/${v.id}/archivo`,
                    );
                if (v.referenciaDecision)
                    h.append(
                        n(
                            "p",
                            `Decisión: ${v.referenciaDecision} · ${fecha(v.fechaDecision)} · Responsable ${v.responsableDecision}`,
                        ),
                    );
                if (revisable && v.decision === "Pendiente")
                    formulario(
                        h,
                        "Registrar decisión de diseño",
                        (f) => {
                            campo(f, "Decisión", "decision", "select", [
                                "Aprobada",
                                "Rechazada",
                            ]);
                            campo(
                                f,
                                "Referencia del acuerdo con el cliente",
                                "referencia",
                            );
                        },
                        (f) =>
                            escribir(
                                `detalles/${d.id}/disenos/${v.id}/decision`,
                                {
                                    decision: f.elements.decision.value,
                                    referenciaDecision:
                                        f.elements.referencia.value,
                                },
                            ),
                    );
                a.append(h);
            }
            if (revisable) {
                formulario(
                    a,
                    "Registrar versión descrita",
                    (f) =>
                        campo(
                            f,
                            "Descripción o constancia Sin personalización",
                            "descripcion",
                            "textarea",
                            "",
                            true,
                            1000,
                        ),
                    (f) =>
                        escribir(`detalles/${d.id}/disenos/texto`, {
                            descripcion: f.elements.descripcion.value,
                        }),
                );
                formulario(
                    a,
                    "Subir nueva versión de diseño",
                    (f) => {
                        const i = campo(
                            f,
                            "Imagen JPG/PNG (máximo 10 MB)",
                            "archivo",
                            "file",
                        );
                        i.accept = "image/jpeg,image/png";
                        campo(
                            f,
                            "Descripción opcional",
                            "descripcion",
                            "textarea",
                            "",
                            false,
                            1000,
                        );
                    },
                    (f) => {
                        const archivo = f.elements.archivo.files[0];
                        if (archivo.size > 10000000)
                            throw new Error(
                                "El archivo supera 10,000,000 bytes.",
                            );
                        const datos = new FormData();
                        datos.append("archivo", archivo);
                        datos.append(
                            "descripcion",
                            f.elements.descripcion.value,
                        );
                        return escribir(
                            `detalles/${d.id}/disenos/archivo`,
                            datos,
                            true,
                        );
                    },
                );
            }
            prendas.append(a);
        }
        const cot = seccion("Cotizaciones e historial");
        for (const v of x.versionesCotizacion) {
            const a = n("article");
            a.append(
                n(
                    "h4",
                    `Cotización v${v.numeroVersion} · ${v.decision}${v.id === x.versionCotizacionVigente ? " · Vigente" : ""}`,
                ),
                n("p", "Total: " + moneda(v.total)),
                n(
                    "p",
                    `Registro: ${fecha(v.fechaRegistro)} · Responsable ${v.responsableRegistro}`,
                ),
            );
            for (const l of v.detalleImportes)
                a.append(
                    n(
                        "p",
                        `Prenda ${l.detalle}: ${l.cantidad} × ${moneda(l.precioUnitarioAcordado)} = ${moneda(l.subtotal)} · Diseño ${l.versionDiseno ?? "sin asociación"}`,
                    ),
                );
            if (v.referenciaDecision)
                a.append(
                    n(
                        "p",
                        `Decisión: ${v.referenciaDecision} · ${fecha(v.fechaDecision)} · Responsable ${v.responsableDecision}`,
                    ),
                );
            if (revisable && v.decision === "Propuesta")
                formulario(
                    a,
                    "Registrar decisión de cotización",
                    (f) => {
                        campo(f, "Decisión", "decision", "select", [
                            "Aceptada",
                            "Rechazada",
                        ]);
                        campo(
                            f,
                            "Referencia de la decisión del cliente",
                            "referencia",
                        );
                        campo(
                            f,
                            "Fecha de entrega acordada (al aceptar)",
                            "entrega",
                            "date",
                            p.fechaEntregaAcordada || "",
                            false,
                        );
                    },
                    (f) =>
                        escribir(`cotizaciones/${v.id}/decision`, {
                            decision: f.elements.decision.value,
                            referenciaDecision: f.elements.referencia.value,
                            fechaEntregaAcordada:
                                f.elements.entrega.value || null,
                        }),
                );
            cot.append(a);
        }
        if (revisable)
            formulario(
                cot,
                "Proponer nueva cotización",
                (f) => {
                    for (const d of x.detalles) {
                        const i = campo(
                            f,
                            `Precio unitario acordado de prenda ${d.id} (${d.cantidad} unidades)`,
                            "precio-" + d.id,
                            "text",
                            d.precioUnitarioAcordado ?? d.precioBaseReferencia,
                        );
                        i.inputMode = "decimal";
                        i.pattern = "[0-9]+([.][0-9]{1,2})?";
                    }
                },
                (f) =>
                    escribir("cotizaciones", {
                        lineas: x.detalles.map((d) => ({
                            detalle: d.id,
                            precioUnitarioAcordado:
                                f.elements["precio-" + d.id].value,
                            versionDiseno: d.versionDisenoVigente,
                        })),
                    }),
            );
        const pagos = seccion("Pagos y comprobantes");
        for (const g of x.pagos) {
            const a = n("article");
            a.append(
                n(
                    "h4",
                    `Pago ${g.id} · ${g.estadoVerificacion} · ${moneda(g.monto)}`,
                ),
                n(
                    "p",
                    `${g.medio} · ${g.referencia || "Sin referencia"} · Recepción ${fecha(g.fecha)}`,
                ),
            );
            if (g.tieneComprobante)
                descargar(
                    a,
                    "Descargar comprobante",
                    `pagos/${g.id}/comprobante`,
                );
            if (g.fechaVerificacion)
                a.append(
                    n(
                        "p",
                        `Revisión ${fecha(g.fechaVerificacion)} · Responsable ${g.responsableVerificacion}`,
                    ),
                );
            if (editable && g.estadoVerificacion === "Pendiente")
                formulario(
                    a,
                    "Registrar revisión del pago",
                    (f) => {
                        campo(f, "Decisión", "decision", "select", [
                            "Verificado",
                            "Rechazado",
                        ]);
                        campo(
                            f,
                            "Referencia confirmada (obligatoria al verificar)",
                            "referencia",
                            "text",
                            g.referencia || "",
                            false,
                            100,
                        );
                    },
                    (f) =>
                        escribir(`pagos/${g.id}/decision`, {
                            decision: f.elements.decision.value,
                            referencia: f.elements.referencia.value || null,
                        }),
                );
            pagos.append(a);
        }
        if (!x.pagos.length) pagos.append(n("p", "No hay pagos registrados."));
        if (editable && p.cotizacionAceptada) {
            let confirmacion = crypto.randomUUID();
            formulario(
                pagos,
                "Registrar cobro directo recibido",
                (f) => {
                    const m = campo(f, "Monto recibido", "monto");
                    m.inputMode = "decimal";
                    m.pattern = "[0-9]+([.][0-9]{1,2})?";
                    campo(f, "Medio", "medio", "select", [
                        "Efectivo",
                        "Transferencia",
                        "Depósito",
                        "Tarjeta",
                    ]);
                    campo(
                        f,
                        "Referencia o constancia de recepción",
                        "referencia",
                        "text",
                        "",
                        true,
                        100,
                    );
                    campo(
                        f,
                        "Fecha y hora de recepción",
                        "fecha",
                        "datetime-local",
                    );
                },
                (f) =>
                    escribir("cobros", {
                        monto: f.elements.monto.value,
                        medio: f.elements.medio.value,
                        referencia: f.elements.referencia.value,
                        fecha: new Date(f.elements.fecha.value).toISOString(),
                        confirmacion,
                    }),
            );
        }
        const prod = seccion("Producción y entrega");
        prod.append(
            n(
                "p",
                "Secuencia: Pendiente → En producción → Listo para entrega → Entregado. Registrar un pago no cambia automáticamente el estado de producción.",
            ),
        );
        if (editable && p.estado !== "Entregado") {
            const siguiente = {
                Pendiente: "En producción",
                "En producción": "Listo para entrega",
                "Listo para entrega": "Entregado",
            }[p.estado];
            if (p.estado === "En producción")
                formulario(
                    prod,
                    "Comprobar y autorizar sublimación",
                    () => {},
                    () => escribir("sublimacion", {}),
                );
            if (siguiente)
                formulario(
                    prod,
                    "Pasar a " + siguiente,
                    (f) => {
                        if (siguiente === "Entregado")
                            campo(
                                f,
                                "Referencia de confirmación de recepción del cliente",
                                "referencia",
                            );
                    },
                    (f) =>
                        escribir("produccion", {
                            nuevoEstado: siguiente,
                            referenciaConfirmacionEntrega:
                                siguiente === "Entregado"
                                    ? f.elements.referencia.value
                                    : null,
                        }),
                );
        }
        prod.append(
            n(
                "p",
                "Para la demostración académica, la entrega exige saldo cero y confirmación del cliente. La política comercial definitiva está pendiente de validación.",
            ),
        );
        for (const c of x.comprobacionesSublimacion)
            prod.append(
                n(
                    "p",
                    `Comprobación ${c.id} · Cotización ${c.versionCotizacion} · Diseños ${c.versionesDiseno.join(", ")} · Verificado ${moneda(c.montoVerificado)} · ${fecha(c.fecha)} · Responsable ${c.responsable}`,
                ),
            );
        const constancias = seccion("Constancias registradas");
        for (const c of x.constancias)
            constancias.append(
                n(
                    "p",
                    `${c.tipo} · ${c.decision ? "Aceptada" : "Rechazada"} · ${c.referencia} · ${fecha(c.fecha)} · Responsable ${c.responsable}`,
                ),
            );
        if (!x.constancias.length)
            constancias.append(n("p", "Sin constancias registradas."));
        if (editable && p.estado === "Pendiente" && !p.cotizacionAceptada)
            formulario(
                seccion("Archivo administrativo"),
                "Archivar solicitud conservando su historial",
                (f) =>
                    campo(
                        f,
                        "Motivo documentado",
                        "motivo",
                        "textarea",
                        "",
                        true,
                        250,
                    ),
                (f) =>
                    escribir("archivo", {
                        motivoArchivo: f.elements.motivo.value,
                    }),
            );
        if (ocupado)
            for (const c of $("gestion").querySelectorAll(
                "button,input,select,textarea",
            ))
                c.disabled = true;
    }
    $("buscar").addEventListener("input", lista);
    for (const id of ["estado", "condicion"])
        $(id).addEventListener("change", () => {
            if (!ocupado) cargarLista().catch((e) => mensaje(e.message, true));
        });
    $("actualizar").addEventListener("click", async () => {
        if (ocupado) return;
        try {
            await cargarLista();
            if (seleccionado) await cargarExpediente(seleccionado);
        } catch (e) {
            mensaje(e.message, true);
        }
    });
    async function iniciar() {
        try {
            const sesion = await api("/api/sesion");
            if (sesion.rol !== "Administrador") {
                denegar();
                return;
            }
            autorizado = true;
            identidadId = sesion.id;
            $("gestion").hidden = false;
            $("acceso-denegado").hidden = true;
            await cargarLista();
            const id = new URLSearchParams(location.search).get("pedido");
            if (id && /^[1-9]\d{0,9}$/.test(id) && Number(id) <= 2147483647)
                await abrir(Number(id));
        } catch (error) {
            mensaje(error.message, true);
        } finally {
            $("estado-acceso").hidden = true;
        }
    }
    async function revalidarAcceso() {
        if (validandoAcceso) return;
        const identidadAnterior = identidadId;
        posicionPestana.guardar();
        validandoAcceso = true;
        $("gestion").hidden = true;
        try {
            const sesion = await api("/api/sesion");
            if (sesion.rol !== "Administrador") {
                denegar();
                return;
            }
            if (!autorizado || identidadId !== sesion.id) {
                limpiarExpedientes();
                autorizado = true;
                identidadId = sesion.id;
                await cargarLista();
            }
            $("acceso-denegado").hidden = true;
            $("gestion").hidden = false;
        } catch (error) {
            mensaje(error.message, true);
        } finally {
            validandoAcceso = false;
            posicionPestana.restaurar(autorizado && identidadId === identidadAnterior && !$("gestion").hidden);
        }
    }
    window.addEventListener("pagehide", () => {
        $("gestion").hidden = true;
    });
    window.addEventListener("pageshow", (evento) => {
        if (evento.persisted) revalidarAcceso();
    });
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) revalidarAcceso();
    });
    iniciar();
})();
