'use strict';

(() => {
  // Solo datos ficticios y memoria de esta página. No existe guardado de pedidos.
  function datosIniciales() {
    const ejemplos = [
      { id: 901, codigo: 'DEMO-001', cliente: 'Cliente de demostración 01', modalidadEntrega: 'Retiro', direccionEntrega: '', estado: 'Pendiente', condicionAdministrativa: 'Activa', fechaEntregaAcordada: '', motivoArchivo: '', verificados: '0.00', vigente: null,
        detalles: [{ id: 101, cantidad: 2, talla: 'M', color: 'Blanco', tela: 'Poliéster', empaque: 'Empaque indicado en la solicitud ficticia', precioBaseReferencia: '120.00', idea: 'Logotipo de demostración en el frente.', vigente: null, versiones: [{ id: 3001, numeroVersion: 1, descripcion: 'Primera propuesta del logotipo ficticio.', decision: 'Pendiente', referencia: '' }] }],
        cotizaciones: [{ id: 1001, numeroVersion: 1, decision: 'Propuesta', referencia: '', lineas: [{ detalle: 101, cantidad: 2, precioUnitarioAcordado: '150.00' }] }] },
      { id: 902, codigo: 'DEMO-002', cliente: 'Cliente de demostración 02', modalidadEntrega: 'Domicilio', direccionEntrega: 'Dirección ficticia de demostración', estado: 'En producción', condicionAdministrativa: 'Activa', fechaEntregaAcordada: '2026-11-10', motivoArchivo: '', verificados: '500.00', vigente: 2001,
        detalles: [{ id: 102, cantidad: 5, talla: 'L', color: 'Blanco', tela: 'Poliéster', empaque: 'Empaque acordado en este ejemplo', precioBaseReferencia: '180.00', idea: 'Texto ficticio para un evento.', vigente: 4001, versiones: [{ id: 4001, numeroVersion: 1, descripcion: 'Texto de demostración aprobado.', decision: 'Aprobada', referencia: 'Acuerdo ficticio inicial' }] }],
        cotizaciones: [{ id: 2001, numeroVersion: 1, decision: 'Aceptada', referencia: 'Acuerdo ficticio inicial', lineas: [{ detalle: 102, cantidad: 5, precioUnitarioAcordado: '200.00' }] }] }
    ];
    for (const p of ejemplos) {
      for (const v of [...p.cotizaciones, ...p.detalles.flatMap(d => d.versiones)]) {
        v.fechaRegistro = '2026-10-29T14:00:00Z'; v.responsableRegistro = 1;
        const resuelta = ['Aceptada', 'Aprobada', 'Rechazada'].includes(v.decision);
        v.fechaDecision = resuelta ? '2026-10-29T15:00:00Z' : null; v.responsableDecision = resuelta ? 1 : null;
      }
    }
    return ejemplos;
  }
  let pedidos = datosIniciales();
  let seleccionado = null;
  const $ = id => document.getElementById(id);
  const nodo = (tag, texto, clase) => { const e = document.createElement(tag); if (texto !== undefined) e.textContent = texto; if (clase) e.className = clase; return e; };
  const cents = valor => {
    const texto = String(valor).trim();
    if (!/^\d{1,16}(?:\.\d{1,2})?$/.test(texto)) throw new Error('Usa importes positivos con punto decimal y hasta dos decimales, sin separadores de miles.');
    const [entero, decimal = ''] = texto.split('.');
    return BigInt(entero) * 100n + BigInt(decimal.padEnd(2, '0'));
  };
  const decimal = valor => `${valor / 100n}.${String(valor % 100n).padStart(2, '0')}`;
  const moneda = valor => { const [entero, centavos] = decimal(valor).split('.'); return `C$${entero.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}.${centavos}`; };
  const total = version => version.lineas.reduce((suma, l) => suma + cents(l.precioUnitarioAcordado) * BigInt(l.cantidad), 0n);
  const actual = () => pedidos.find(p => p.id === seleccionado);
  const registroFicticio = () => ({ fechaRegistro: new Date().toISOString(), responsableRegistro: 1, fechaDecision: null, responsableDecision: null });
  function mostrarRegistro(destino, version) {
    const fecha = valor => new Date(valor).toLocaleString('es-NI', { timeZone: 'America/Managua' });
    destino.append(nodo('p', `Registro ficticio: ${fecha(version.fechaRegistro)} · Administrador ficticio ${version.responsableRegistro}.`, 'ayuda-pedido'));
    if (version.fechaDecision) destino.append(nodo('p', `Decisión ficticia: ${fecha(version.fechaDecision)} · Administrador ficticio ${version.responsableDecision}.`, 'ayuda-pedido'));
  }
  const bloqueado = p => p.condicionAdministrativa === 'Archivada' || ['Listo para entrega', 'Entregado'].includes(p.estado);
  const boton = (texto, accion, deshabilitado = false) => { const e = nodo('button', texto, 'boton-secundario'); e.type = 'button'; e.disabled = deshabilitado; e.addEventListener('click', accion); return e; };
  function campo(formulario, id, titulo, tipo = 'text', valor = '', maximo = 250) {
    const div = nodo('div', undefined, 'campo'); const label = nodo('label', titulo); label.htmlFor = id;
    const input = document.createElement(tipo === 'textarea' ? 'textarea' : 'input'); input.id = id; input.name = id; input.value = valor; input.required = true;
    if (tipo !== 'textarea') input.type = tipo; input.maxLength = maximo;
    div.append(label, input); formulario.append(div); return input;
  }
  function mensaje(texto, error = false) { const e = $('resultado-prototipo'); e.textContent = texto; e.hidden = false; e.classList.toggle('error', error); e.focus(); }
  function intentar(accion) { try { accion(); } catch (e) { mensaje(e.message, true); } }
  function referencia(input) { const t = input.value.trim(); if (!t || t.length > 250) throw new Error('Indica una referencia ficticia del acuerdo, de 1 a 250 caracteres.'); return t; }
  function listo(texto) { dibujarLista(); dibujarExpediente(); mensaje(`Simulación: ${texto} Estos cambios solo existen en esta página.`); }
  function dibujarLista() {
    const busqueda = $('buscar-pedido').value.trim().toLocaleLowerCase('es');
    const visibles = pedidos.filter(p => (!busqueda || `${p.codigo} ${p.cliente}`.toLocaleLowerCase('es').includes(busqueda)) && (!$('estado-pedido').value || p.estado === $('estado-pedido').value) && ($('condicion-pedido').value === 'Todas' || p.condicionAdministrativa === $('condicion-pedido').value));
    $('conteo-pedidos').textContent = `${visibles.length} solicitudes ficticias`;
    $('pedidos-demo').replaceChildren(...visibles.map(p => {
      const b = boton('', () => { seleccionado = p.id; dibujarLista(); dibujarExpediente(); $('titulo-expediente').focus(); }); b.className = 'tarjeta-pedido'; b.setAttribute('aria-pressed', String(seleccionado === p.id));
      b.append(nodo('strong', p.codigo), nodo('span', p.cliente), nodo('span', `${p.estado} · ${p.condicionAdministrativa}`)); return b;
    }));
    if (!visibles.length) $('pedidos-demo').append(nodo('p', 'No hay solicitudes ficticias con esos filtros.', 'ayuda-pedido'));
  }
  function bloque(titulo) { const s = nodo('section', undefined, 'bloque-pedido'); s.append(nodo('h3', titulo)); $('expediente-demo').append(s); return s; }
  function dibujarExpediente() {
    const p = actual(); const destino = $('expediente-demo'); destino.replaceChildren();
    if (!p) { destino.append(nodo('p', 'Selecciona una solicitud para revisar sus prendas e historial.')); return; }
    const cabecera = nodo('div', undefined, 'cabecera-expediente'); const titulo = nodo('h2', p.codigo); titulo.id = 'titulo-expediente'; titulo.tabIndex = -1;
    const identidad = nodo('div'); identidad.append(titulo, nodo('p', p.cliente), nodo('p', `Entrega: ${p.modalidadEntrega}${p.direccionEntrega ? ' · ' + p.direccionEntrega : ''}`), nodo('p', `Fecha acordada: ${p.fechaEntregaAcordada || 'Pendiente'}`)); cabecera.append(identidad, nodo('span', `${p.estado} · ${p.condicionAdministrativa}`, 'etiqueta')); destino.append(cabecera);
    const vigente = p.cotizaciones.find(v => v.id === p.vigente); const resumen = nodo('div', undefined, 'resumen-importes');
    const referenciaTotal = p.detalles.reduce((suma, d) => suma + cents(d.precioBaseReferencia) * BigInt(d.cantidad), 0n);
    const valores = [['Referencia inicial', moneda(referenciaTotal)], ['Cotización aceptada', vigente ? moneda(total(vigente)) : 'Pendiente'], ['Saldo pendiente', vigente ? moneda(total(vigente) - cents(p.verificados)) : 'Sin acuerdo vigente']];
    for (const [label, valor] of valores) { const item = nodo('p'); item.append(nodo('span', label), nodo('strong', valor)); resumen.append(item); } destino.append(resumen);
    if (vigente) {
      const anticipo = (total(vigente) + 1n) / 2n; const faltante = anticipo > cents(p.verificados) ? anticipo - cents(p.verificados) : 0n;
      destino.append(nodo('p', `Pagos verificados ficticios: ${moneda(cents(p.verificados))}. Anticipo mínimo del 50 %: ${moneda(anticipo)}.`, 'ayuda-pedido'));
      destino.append(nodo('p', faltante ? `Faltan ${moneda(faltante)} de anticipo. No corresponde autorizar la sublimación mientras sea insuficiente; también se deben comprobar los diseños vigentes.` : 'El importe cubre el mínimo. La autorización de producción requiere comprobar también las versiones y las demás condiciones.', 'aviso-pedido'));
    }
    const prendas = bloque('Prendas y versiones de diseño');
    for (const d of p.detalles) dibujarPrenda(prendas, p, d);
    const cotizaciones = bloque('Historial de cotizaciones');
    if (!p.cotizaciones.length) cotizaciones.append(nodo('p', 'Aún no hay propuestas.'));
    for (const v of p.cotizaciones) {
      const card = nodo('article', undefined, 'version-pedido'); card.append(nodo('h3', `Cotización v${v.numeroVersion} · ${v.decision}${p.vigente === v.id ? ' · Vigente' : ''}`));
      mostrarRegistro(card, v);
      const lineas = nodo('ul'); for (const l of v.lineas) lineas.append(nodo('li', `Prenda ${l.detalle}: ${l.cantidad} × ${moneda(cents(l.precioUnitarioAcordado))}`)); card.append(lineas, nodo('p', `Total: ${moneda(total(v))}`));
      if (v.referencia) card.append(nodo('p', `Referencia ficticia: ${v.referencia}`));
      if (v.decision === 'Propuesta' && !bloqueado(p)) {
        const form = nodo('form'); form.noValidate = true;
        const ref = campo(form, `acuerdo-${v.id}`, 'Referencia ficticia de la decisión');
        const fecha = campo(form, `fecha-${v.id}`, 'Fecha acordada (obligatoria al aceptar)', 'date', p.fechaEntregaAcordada); fecha.required = false;
        const acciones = nodo('div', undefined, 'acciones-version');
        for (const decision of ['Aceptada', 'Rechazada']) acciones.append(boton(`${decision === 'Aceptada' ? 'Aceptar' : 'Rechazar'} propuesta · simulación`, () => intentar(() => {
          if (v.decision !== 'Propuesta') throw new Error('Esta decisión ya fue resuelta.');
          const acuerdo = referencia(ref);
          if (decision === 'Aceptada') {
            if (!fecha.value || !fecha.checkValidity()) throw new Error('Indica una fecha de entrega acordada válida para aceptar.');
            if (total(v) < cents(p.verificados)) throw new Error('El total aceptado no puede quedar por debajo de los pagos verificados.');
            p.vigente = v.id; p.fechaEntregaAcordada = fecha.value;
          }
          v.decision = decision; v.referencia = acuerdo; v.fechaDecision = new Date().toISOString(); v.responsableDecision = 1; listo(`cotización v${v.numeroVersion} ${decision.toLowerCase()}; el estado sigue ${p.estado}.`);
        })));
        form.addEventListener('submit', e => e.preventDefault()); form.append(acciones); card.append(form);
      } cotizaciones.append(card);
    }
    dibujarPropuesta(p); dibujarArchivo(p);
  }
  function dibujarPrenda(seccion, p, d) {
    const card = nodo('article', undefined, 'detalle-prenda'); card.append(nodo('h3', `Prenda ${d.id} · ${d.cantidad} camisetas`), nodo('p', `${d.talla} · ${d.color} · ${d.tela}`), nodo('p', `Empaque: ${d.empaque}`), nodo('p', `Idea original: ${d.idea}`));
    for (const v of d.versiones) {
      const version = nodo('div', undefined, 'version-pedido'); version.append(nodo('strong', `Diseño v${v.numeroVersion} · ${v.decision}${d.vigente === v.id ? ' · Vigente' : ''}`), nodo('p', v.descripcion));
      mostrarRegistro(version, v);
      if (v.referencia) version.append(nodo('p', `Referencia ficticia: ${v.referencia}`));
      if (v.decision === 'Pendiente' && !bloqueado(p)) {
        const ref = campo(version, `diseno-${d.id}-${v.id}`, 'Referencia ficticia de aprobación o rechazo'); const acciones = nodo('div', undefined, 'acciones-version');
        for (const decision of ['Aprobada', 'Rechazada']) acciones.append(boton(`${decision === 'Aprobada' ? 'Aprobar' : 'Rechazar'} diseño · simulación`, () => intentar(() => {
          if (v.decision !== 'Pendiente') throw new Error('La decisión de esta versión ya está resuelta.');
          const acuerdo = referencia(ref); v.decision = decision; v.referencia = acuerdo; v.fechaDecision = new Date().toISOString(); v.responsableDecision = 1;
          if (decision === 'Aprobada') d.vigente = v.id;
          listo(`diseño v${v.numeroVersion} ${decision.toLowerCase()}; se conserva el historial.`);
        }))); version.append(acciones);
      } card.append(version);
    }
    if (!bloqueado(p)) {
      const form = nodo('form'); form.noValidate = true; const descripcion = campo(form, `revision-${d.id}`, 'Descripción ficticia de una nueva versión', 'textarea', '', 1000);
      const submit = nodo('button', 'Simular nueva versión', 'boton-secundario'); submit.type = 'submit'; form.append(submit);
      form.addEventListener('submit', e => { e.preventDefault(); intentar(() => {
        const texto = descripcion.value.trim(); if (!texto || texto.length > 1000) throw new Error('Describe la nueva versión con 1 a 1000 caracteres.');
        const numero = Math.max(0, ...d.versiones.map(v => v.numeroVersion)) + 1; const id = Math.max(0, ...pedidos.flatMap(p => p.detalles.flatMap(d => d.versiones.map(v => v.id)))) + 1; d.versiones.push({ id, numeroVersion: numero, descripcion: texto, decision: 'Pendiente', referencia: '', ...registroFicticio() });
        listo('nueva revisión pendiente; el diseño aprobado vigente se conserva.');
      }); }); card.append(form);
    } seccion.append(card);
  }
  function dibujarPropuesta(p) {
    const seccion = bloque('Nueva propuesta de precio');
    if (bloqueado(p)) { seccion.append(nodo('p', 'Esta solicitud no admite nuevas propuestas en su condición actual.')); return; }
    seccion.append(nodo('p', 'Cada propuesta conserva todos los detalles y no sustituye el acuerdo vigente hasta su aceptación.', 'ayuda-pedido'));
    const form = nodo('form'); form.noValidate = true; const entradas = p.detalles.map(d => {
      const input = campo(form, `precio-${d.id}`, `Precio unitario acordado para prenda ${d.id} · ${d.cantidad} unidades`, 'text', '', 19); input.inputMode = 'decimal'; return { d, input };
    });
    form.append(nodo('p', 'Usa punto decimal y hasta dos decimales, sin separadores de miles.', 'ayuda-pedido'));
    const submit = nodo('button', 'Simular nueva propuesta', 'boton-principal'); submit.type = 'submit'; form.append(submit);
    form.addEventListener('submit', e => { e.preventDefault(); intentar(() => {
      const lineas = entradas.map(({ d, input }) => { const valor = cents(input.value); if (valor <= 0n) throw new Error('Todos los precios deben ser mayores que cero.'); return { detalle: d.id, cantidad: d.cantidad, precioUnitarioAcordado: decimal(valor) }; });
      const numero = Math.max(0, ...p.cotizaciones.map(v => v.numeroVersion)) + 1; const id = Math.max(0, ...pedidos.flatMap(p => p.cotizaciones.map(v => v.id))) + 1; p.cotizaciones.push({ id, numeroVersion: numero, decision: 'Propuesta', referencia: '', lineas, ...registroFicticio() });
      listo(`propuesta v${numero} creada; no modifica el precio aceptado ni registra pagos.`);
    }); }); seccion.append(form);
  }
  function dibujarArchivo(p) {
    const seccion = bloque('Archivo administrativo');
    if (p.condicionAdministrativa === 'Archivada') { seccion.append(nodo('p', `Motivo ficticio: ${p.motivoArchivo}. Se conserva el estado ${p.estado} y todo el historial.`)); return; }
    const puede = p.estado === 'Pendiente' && !p.cotizaciones.some(v => v.decision === 'Aceptada') && cents(p.verificados) === 0n;
    seccion.append(nodo('p', 'Solo una solicitud Pendiente sin cotización aceptada ni cobros verificados puede archivarse. Las evidencias pendientes deben resolverse antes; este ejemplo no contiene ninguna.', 'ayuda-pedido'));
    if (!puede) return;
    const form = nodo('form'); form.noValidate = true; const motivo = campo(form, 'motivo-archivo', 'Motivo ficticio del archivo'); const b = nodo('button', 'Simular archivo de solicitud', 'boton-secundario'); b.type = 'submit'; form.append(b);
    form.addEventListener('submit', e => { e.preventDefault(); intentar(() => { p.motivoArchivo = referencia(motivo); p.condicionAdministrativa = 'Archivada'; listo('solicitud archivada; su estado e historial se conservan.'); }); }); seccion.append(form);
  }
  for (const id of ['buscar-pedido', 'estado-pedido', 'condicion-pedido']) $(id).addEventListener(id === 'buscar-pedido' ? 'input' : 'change', dibujarLista);
  $('reiniciar-demo').addEventListener('click', () => { pedidos = datosIniciales(); seleccionado = null; $('buscar-pedido').value = ''; $('estado-pedido').value = ''; $('condicion-pedido').value = 'Activa'; listo('demostración reiniciada.'); });
  async function iniciar() {
    function denegar() { $('estado-acceso').hidden = true; $('acceso-denegado').hidden = false; }
    try {
      const respuesta = await fetch('/api/sesion', { credentials: 'same-origin', cache: 'no-store' });
      if (respuesta.status === 401 || respuesta.status === 403) { denegar(); return; }
      if (!respuesta.ok) throw new Error('No se pudo comprobar la sesión.');
      if ((await respuesta.json()).rol !== 'Administrador') { denegar(); return; }
      $('estado-acceso').hidden = true; $('espacio-pedidos').hidden = false; dibujarLista();
    } catch { $('estado-acceso').textContent = 'No pudimos comprobar la sesión. Revisa la conexión y actualiza la página para volver a intentarlo.'; }
  }
  iniciar();
})();
