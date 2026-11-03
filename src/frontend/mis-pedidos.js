'use strict';
(() => {
  const posicionPestana = window.crearPosicionPestana();
  const $ = (id) => document.getElementById(id),
    base = '/api/mis-pedidos',
    borradores = new Map();
  const f = $('form-comprobante');
  let cliente = null,
    titularPagina = null,
    pedido = null,
    generacion = 0,
    lectura = 0,
    ocupado = false,
    revalidar = false;
  const nodo = (tipo, texto) => {
    const n = document.createElement(tipo);
    if (texto !== undefined) n.textContent = texto;
    return n;
  };
  const clave = () => (cliente && pedido ? cliente.id + ':' + pedido : null);
  function borrador() {
    const k = clave();
    if (!k) return null;
    if (!borradores.has(k))
      borradores.set(k, {
        monto: '',
        medio: 'Transferencia',
        referencia: '',
        fecha: '',
        archivo: null,
        envio: null,
        dudoso: false,
      });
    return borradores.get(k);
  }
  function moneda(v) {
    if (v === null || v === undefined) return 'Pendiente de acuerdo';
    const s = String(v);
    if (!/^\d+(\.\d{1,2})?$/.test(s)) throw new Error('Importe recibido no válido.');
    const [a, b = ''] = s.split('.');
    return 'C$' + a.replace(/\B(?=(\d{3})+(?!\d))/g, ',') + '.' + b.padEnd(2, '0');
  }
  const fecha = (v) => (v ? new Date(v).toLocaleString('es-NI') : 'Sin registrar');
  const fechaDia = (v) => (v ? v.split('-').reverse().join('/') : 'Pendiente de acuerdo');
  function mensaje(t) {
    $('mensaje-cliente').textContent = t;
    $('mensaje-cliente').hidden = !t;
    if (t) $('mensaje-cliente').focus({ preventScroll: posicionPestana.pendiente });
  }
  function controles() {
    const d = clave() ? borradores.get(clave()) : null;
    for (const e of f.elements) e.disabled = ocupado || !!d?.dudoso;
    $('enviar-comprobante').disabled = ocupado || !cliente || !pedido;
    $('enviar-comprobante').textContent = ocupado
      ? 'Enviando…'
      : d?.dudoso
        ? 'Reintentar el mismo comprobante'
        : 'Enviar comprobante para revisión';
    $('actualizar-mis-pedidos').disabled = ocupado;
    $('login-cliente').querySelector('button').disabled = ocupado;
    $('lista-cliente')
      .querySelectorAll('button')
      .forEach((b) => (b.disabled = ocupado));
  }
  function ocultar() {
    $('mensaje-cliente').textContent = '';
    $('mensaje-cliente').hidden = true;
    $('identidad-pedidos').textContent = 'Comprobando tu sesión…';
    $('contenido-cliente').hidden = true;
    $('lista-cliente').replaceChildren();
    $('expediente-cliente').replaceChildren();
    $('panel-comprobante').hidden = true;
    f.reset();
    $('archivo-conservado').textContent = 'Hasta 10 MB (10 000 000 bytes).';
  }
  async function token() {
    const r = await fetch('/api/seguridad/token', { credentials: 'same-origin', cache: 'no-store' });
    if (!r.ok) throw new Error('No pudimos preparar el envío.');
    return (await r.json()).token;
  }
  async function consulta(url, g) {
    const r = await fetch(url, {
      credentials: 'same-origin',
      cache: 'no-store',
      headers: { 'X-Cliente-Confirmado': String(cliente?.id || 0) },
    });
    if (g !== generacion) throw new Error('Lectura descartada.');
    if (r.status === 401 || r.status === 403) {
      cliente = null;
      ocultar();
      $('acceso-cliente').hidden = false;
      throw new Error('Tu sesión venció o cambió. Inicia sesión aquí para continuar.');
    }
    if (!r.ok)
      throw new Error(
        r.status === 404
          ? 'No encontramos ese pedido en tu cuenta.'
          : 'No pudimos consultar tus pedidos. Actualiza para volver a intentar.',
      );
    return r.json();
  }
  async function cargar(g = generacion) {
    const filas = await consulta(base, g);
    if (g !== generacion || !cliente) return;
    $('contenido-cliente').hidden = false;
    $('lista-cliente').replaceChildren();
    $('estado-lista-cliente').textContent = filas.length
      ? filas.length + ' pedidos.'
      : 'Todavía no tienes solicitudes. Puedes preparar una desde Solicitar cotización.';
    for (const p of filas) {
      const b = nodo('button', p.codigo + ' · ' + p.estado + ' · ' + p.condicionAdministrativa);
      b.type = 'button';
      b.setAttribute('aria-pressed', String(p.id === pedido));
      b.addEventListener('click', () => {
        if (!ocupado) abrir(p.id);
      });
      $('lista-cliente').append(b);
    }
    if (pedido && !filas.some((p) => p.id === pedido)) pedido = null;
    if (pedido) await abrir(pedido, g);
    else {
      $('expediente-cliente').replaceChildren(
        nodo('p', 'Selecciona un pedido para consultar su expediente.'),
      );
      $('panel-comprobante').hidden = true;
    }
    controles();
  }
  async function sesion() {
    const identidadAnterior = titularPagina;
    const g = ++generacion;
    ++lectura;
    ocultar();
    try {
      const r = await fetch('/api/sesion', { credentials: 'same-origin', cache: 'no-store' });
      const c = r.ok ? await r.json() : null;
      if (g !== generacion) return;
      if (c?.rol !== 'Cliente') {
        cliente = null;
        $('acceso-cliente').hidden = false;
        $('identidad-pedidos').textContent = 'Necesitas una sesión de Cliente activa.';
        return;
      }
      if (titularPagina !== c.id) pedido = null;
      cliente = c;
      titularPagina = c.id;
      $('identidad-pedidos').textContent = 'Cuenta Cliente: ' + c.nombre;
      $('acceso-cliente').hidden = true;
      const id = new URLSearchParams(location.search).get('pedido');
      if (!pedido && /^\d+$/.test(id || '')) pedido = Number(id);
      await cargar(g);
    } catch (err) {
      if (g === generacion) {
        cliente = null;
        ocultar();
        $('acceso-cliente').hidden = false;
        $('identidad-pedidos').textContent =
          'No pudimos comprobar el acceso. El formulario sigue en memoria.';
        mensaje(err.message);
      }
    } finally {
      controles();
      if (g === generacion)
        posicionPestana.restaurar(!!cliente && cliente.id === identidadAnterior);
    }
  }
  async function abrir(id, g = generacion) {
    const l = ++lectura;
    pedido = id;
    $('panel-comprobante').hidden = true;
    f.reset();
    $('expediente-cliente').replaceChildren(nodo('p', 'Consultando el pedido…'));
    try {
      const e = await consulta(base + '/' + id, g);
      if (g !== generacion || l !== lectura || !cliente) return;
      dibujar(e);
      restaurar();
      $('lista-cliente')
        .querySelectorAll('button')
        .forEach((b) =>
          b.setAttribute('aria-pressed', String(b.textContent.startsWith(e.pedido.codigo + ' ·'))),
        );
    } catch (err) {
      if (g === generacion && l === lectura) {
        $('expediente-cliente').replaceChildren(nodo('p', err.message));
        mensaje(err.message);
      }
    }
  }
  function dibujar(e) {
    const panel = $('expediente-cliente'),
      p = e.pedido;
    panel.replaceChildren(
      nodo('h2', p.codigo),
      nodo('p', p.estado + ' · ' + p.condicionAdministrativa + ' · ' + p.modalidadEntrega),
      nodo(
        'p',
        'Entrega acordada: ' +
          fechaDia(p.fechaEntregaAcordada) +
          ' · Entrega real: ' +
          fecha(p.fechaEntregaReal),
      ),
    );
    const dl = nodo('dl');
    dl.className = 'resumen-cliente';
    for (const [titulo, v] of [
      ['Referencia inicial', p.totalReferencial],
      ['Total aceptado', p.total],
      ['Anticipo mínimo', p.anticipoExigible],
      ['Saldo pendiente', p.saldoPendiente],
    ])
      dl.append(nodo('dt', titulo), nodo('dd', moneda(v)));
    panel.append(dl);
    if (e.direccionEntrega) panel.append(nodo('p', 'Dirección: ' + e.direccionEntrega));
    if (e.motivoArchivo)
      panel.append(nodo('p', 'Archivo administrativo: ' + e.motivoArchivo + ' · ' + fecha(e.fechaArchivo)));
    panel.append(nodo('h3', 'Prendas e historial'));
    for (const d of e.detalles) {
      const x = nodo('details');
      x.dataset.vista = 'prenda-' + p.id + '-' + d.id;
      x.append(
        nodo('summary', 'Prenda ' + d.id + ' · ' + d.cantidad + ' unidades'),
        nodo('p', d.talla + ' · ' + d.color + ' · ' + d.tela + ' · ' + d.empaque),
        nodo('p', d.ideaDiseno || 'Diseño aportado'),
        nodo('p', d.observaciones || 'Sin observaciones'),
      );
      for (const v of e.versionesDiseno.filter((v) => v.detalle === d.id))
        x.append(
          nodo(
            'p',
            'Diseño v' +
              v.numeroVersion +
              ' · ' +
              v.decision +
              (v.id === d.versionDisenoVigente ? ' · Vigente' : '') +
              ' · ' +
              (v.descripcion || 'Imagen') +
              ' · ' +
              fecha(v.fechaDecision || v.fechaRegistro),
          ),
        );
      panel.append(x);
    }
    for (const v of e.versionesCotizacion) {
      const x = nodo('details');
      x.dataset.vista = 'cotizacion-' + p.id + '-' + v.id;
      x.append(
        nodo(
          'summary',
          'Cotización v' +
            v.numeroVersion +
            ' · ' +
            v.decision +
            (v.id === e.versionCotizacionVigente ? ' · Vigente' : '') +
            ' · ' +
            moneda(v.total),
        ),
        nodo('p', 'Registro: ' + fecha(v.fechaRegistro) + ' · Decisión: ' + fecha(v.fechaDecision)),
      );
      for (const d of v.detalleImportes)
        x.append(
          nodo(
            'p',
            'Prenda ' +
              d.detalle +
              ': ' +
              d.cantidad +
              ' × ' +
              moneda(d.precioUnitarioAcordado) +
              ' = ' +
              moneda(d.subtotal),
          ),
        );
      panel.append(x);
    }
    panel.append(nodo('h3', 'Pagos y evidencias'));
    if (!e.pagos.length) panel.append(nodo('p', 'No hay pagos registrados.'));
    for (const pago of e.pagos) {
      const x = nodo('article');
      x.append(
        nodo('h4', 'Pago ' + pago.id + ' · ' + pago.estadoVerificacion + ' · ' + moneda(pago.monto)),
        nodo(
          'p',
          pago.medio + ' · ' + (pago.referencia || 'Sin referencia') + ' · Cobro: ' + fecha(pago.fecha),
        ),
        nodo('p', 'Revisión: ' + fecha(pago.fechaVerificacion)),
      );
      if (pago.tieneComprobante) {
        const b = nodo('button', 'Descargar comprobante ' + pago.id);
        b.type = 'button';
        b.className = 'boton-secundario';
        b.addEventListener('click', () => descargar(p.id, pago.id, generacion));
        x.append(b);
      }
      panel.append(x);
    }
    $('panel-comprobante').hidden = !(p.cotizacionAceptada && p.condicionAdministrativa === 'Activa');
    if (!p.cotizacionAceptada)
      panel.append(
        nodo(
          'p',
          'El precio definitivo y el saldo están pendientes de acuerdo. Los comprobantes se aportan después de aceptar la cotización.',
        ),
      );
  }
  function restaurar() {
    const d = borrador();
    if (!d) return;
    for (const campo of ['monto', 'medio', 'referencia', 'fecha']) f.elements[campo].value = d[campo];
    $('archivo-conservado').textContent = d.archivo
      ? 'Archivo conservado en esta página: ' + d.archivo.name
      : 'Hasta 10 MB (10 000 000 bytes).';
    controles();
  }
  f.addEventListener('input', () => {
    if (ocupado) return;
    const d = borrador();
    if (!d || d.dudoso) return;
    for (const campo of ['monto', 'medio', 'referencia', 'fecha']) d[campo] = f.elements[campo].value;
    d.envio = null;
  });
  f.elements.archivo.addEventListener('change', () => {
    const d = borrador();
    if (!d || ocupado || d.dudoso) return;
    d.archivo = f.elements.archivo.files[0] || null;
    d.envio = null;
    restaurar();
  });
  f.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    if (ocupado || !cliente || !pedido) return;
    const titular = cliente.id,
      id = pedido,
      k = clave(),
      d = borrador();
    if (!d.envio) {
      if (!f.reportValidity()) return;
      const monto = d.monto.trim().replace(',', '.');
      const instante = new Date(d.fecha);
      if (
        !/^\d{1,16}(\.\d{1,2})?$/.test(monto) ||
        !/[1-9]/.test(monto) ||
        !d.referencia.trim() ||
        /[\u0000-\u001f\u007f-\u009f]/u.test(d.referencia) ||
        !Number.isFinite(instante.getTime())
      ) {
        mensaje('Revisa monto positivo, referencia y fecha.');
        return;
      }
      if (
        !d.archivo ||
        d.archivo.size < 1 ||
        d.archivo.size > 10000000 ||
        !/\.(jpg|jpeg|png|pdf)$/iu.test(d.archivo.name)
      ) {
        mensaje('Elige un comprobante JPG, PNG o PDF de hasta 10 MB.');
        return;
      }
      d.envio = {
        datos: {
          confirmacion: crypto.randomUUID(),
          clienteConfirmado: titular,
          monto,
          medio: d.medio,
          referencia: d.referencia.trim(),
          fecha: instante.toISOString(),
        },
        archivo: d.archivo,
      };
    }
    ocupado = true;
    d.dudoso = true;
    controles();
    mensaje('Enviando el comprobante…');
    try {
      const rSesion = await fetch('/api/sesion', { credentials: 'same-origin', cache: 'no-store' });
      const cuenta = rSesion.ok ? await rSesion.json() : null;
      if (cuenta?.rol !== 'Cliente' || cuenta.id !== titular) {
        await sesion();
        mensaje('Accede con la cuenta que preparó el comprobante; el envío se conserva en memoria.');
        return;
      }
      const form = new FormData();
      form.append('datos', JSON.stringify(d.envio.datos));
      form.append('archivo', d.envio.archivo, d.envio.archivo.name);
      const r = await fetch(base + '/' + id + '/comprobantes', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'X-CSRF-TOKEN': await token() },
        body: form,
      });
      const b = await r.json().catch(() => null);
      if (r.ok && b?.pagoId) {
        borradores.delete(k);
        await sesion();
        if (cliente?.id === titular)
          mensaje(
            'Comprobante recibido: pago ' +
              b.pagoId +
              ' · ' +
              b.estadoVerificacion +
              '. Solo Verificado reduce el saldo.',
          );
      } else if (r.status === 400 && b?.codigo === 'formulario_vencido') {
        await sesion();
        mensaje('La sesión venció o cambió. Accede aquí con la misma cuenta y reintenta el mismo envío.');
      } else if ([400, 413, 409].includes(r.status)) {
        d.envio = null;
        d.dudoso = false;
        mensaje(b?.title || 'Revisa los datos y el tamaño del archivo.');
      } else if ([401, 403].includes(r.status)) {
        await sesion();
        mensaje('La sesión venció o cambió. Accede aquí con la misma cuenta y reintenta el mismo envío.');
      } else
        mensaje('No pudimos confirmar el resultado. Reintenta el mismo comprobante sin cambiar sus datos.');
    } catch {
      mensaje(
        'No recibimos la confirmación. El archivo y el envío se conservan; reintenta el mismo comprobante para evitar duplicados.',
      );
    } finally {
      ocupado = false;
      if (revalidar) {
        revalidar = false;
        await sesion();
      }
      controles();
    }
  });
  async function descargar(id, pago, g) {
    try {
      const r = await fetch(base + '/' + id + '/pagos/' + pago + '/comprobante', {
        credentials: 'same-origin',
        cache: 'no-store',
        headers: { 'X-Cliente-Confirmado': String(cliente?.id || 0) },
      });
      if (g !== generacion) return;
      if (!r.ok) throw new Error('El comprobante no está disponible para esta cuenta.');
      const b = await r.blob();
      if (g !== generacion) return;
      const u = URL.createObjectURL(b),
        a = nodo('a');
      a.href = u;
      const tipo = r.headers.get('Content-Type') || '';
      a.download =
        'comprobante-' + pago + (tipo.includes('pdf') ? '.pdf' : tipo.includes('png') ? '.png' : '.jpg');
      a.click();
      setTimeout(() => URL.revokeObjectURL(u), 1000);
    } catch (err) {
      if (g === generacion) mensaje(err.message);
    }
  }
  $('actualizar-mis-pedidos').addEventListener('click', () => {
    if (!ocupado) sesion();
  });
  $('login-cliente').addEventListener('submit', async (ev) => {
    ev.preventDefault();
    if (ocupado) return;
    ocupado = true;
    controles();
    const formulario = ev.currentTarget;
    try {
      const r = await fetch('/api/sesion', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await token() },
        body: JSON.stringify({
          correo: formulario.elements.correo.value.trim(),
          contrasena: formulario.elements.contrasena.value,
        }),
      });
      formulario.elements.contrasena.value = '';
      if (r.ok || r.status === 409) await sesion();
      else mensaje('No pudimos iniciar sesión. Revisa tus datos e inténtalo de nuevo.');
    } catch {
      mensaje('No pudimos confirmar el acceso. Intenta de nuevo.');
    } finally {
      ocupado = false;
      controles();
    }
  });
  window.addEventListener('pageshow', (e) => {
    if (e.persisted) {
      if (ocupado) revalidar = true;
      else sesion();
    }
  });
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      ++generacion;
      ++lectura;
      ocultar();
    } else if (ocupado) revalidar = true;
    else sesion();
  });
  sesion();
})();
