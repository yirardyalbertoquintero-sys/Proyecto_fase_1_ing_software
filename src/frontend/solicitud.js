'use strict';

const posicionPestana = window.crearPosicionPestana();
const $ = (id) => document.getElementById(id);
const configuracion = $('configurar-prenda'),
  formulario = $('enviar-solicitud');
let variantes = [],
  carrito = [],
  cliente = null,
  propietario = null,
  ocupado = false;
let envio = null,
  dudoso = false,
  consultaSesion = 0;
let recibo = null;
let revalidarPendiente = false;
const MAX_ARCHIVO = 10000000,
  MAX_TOTAL_ARCHIVOS = 20000000;
function nodo(tipo, texto) {
  const n = document.createElement(tipo);
  if (texto !== undefined) n.textContent = texto;
  return n;
}
function centavos(value) {
  const s = String(value);
  if (!/^\d{1,26}(\.\d{1,2})?$/u.test(s)) throw new Error('Precio de referencia no válido.');
  const [a, b = ''] = s.split('.');
  return BigInt(a) * 100n + BigInt(b.padEnd(2, '0'));
}
function decimal(c) {
  return (c / 100n).toString() + '.' + (c % 100n).toString().padStart(2, '0');
}
function moneda(c) {
  const [a, b] = decimal(c).split('.');
  return 'C$' + a.replace(/\B(?=(\d{3})+(?!\d))/gu, ',') + '.' + b;
}
function referencia() {
  return carrito.reduce((n, d) => n + centavos(d.precioBaseConfirmado) * BigInt(d.cantidad), 0n);
}
function mensaje(texto) {
  const m = $('mensaje-solicitud');
  m.textContent = texto;
  m.hidden = !texto;
  if (texto) m.focus({ preventScroll: posicionPestana.pendiente });
}
function mismaCuenta() {
  return cliente && (propietario === null || propietario === cliente.id);
}
function controles() {
  const editar = ocupado || dudoso || !mismaCuenta();
  for (const el of configuracion.elements) el.disabled = editar;
  for (const el of formulario.elements) el.disabled = editar;
  $('registrar-solicitud').disabled =
    ocupado || !mismaCuenta() || carrito.length === 0 || carrito.some((d) => d.invalida);
  $('registrar-solicitud').textContent = ocupado
    ? 'Enviando…'
    : dudoso
      ? 'Reintentar la misma solicitud'
      : 'Enviar solicitud para cotizar';
  $('actualizar-carrito').disabled = editar || !carrito.length;
  $('acceso-en-pagina').querySelector('button').disabled = ocupado;
  $('nueva-solicitud').disabled = ocupado;
  $('vaciar-otra-cuenta').hidden = !(cliente && propietario !== null && propietario !== cliente.id);
  document.querySelectorAll('.quitar-configuracion').forEach((b) => (b.disabled = editar));
}
async function csrf() {
  const r = await fetch('/api/seguridad/token', { credentials: 'same-origin', cache: 'no-store' });
  if (!r.ok) throw new Error('No pudimos preparar el formulario. Intenta de nuevo.');
  return (await r.json()).token;
}
async function sesion() {
  const identidadAnterior = cliente?.id;
  const numero = ++consultaSesion;
  $('contenido-solicitud').hidden = true;
  $('carrito-solicitud').replaceChildren();
  $('solicitud-confirmada').hidden = true;
  $('codigo-solicitud').textContent = '';
  $('resumen-solicitud').textContent = '';
  try {
    const r = await fetch('/api/sesion', { credentials: 'same-origin', cache: 'no-store' });
    if (numero !== consultaSesion) return;
    cliente = r.ok ? await r.json() : null;
    if (numero !== consultaSesion) return;
    const esCliente = cliente?.rol === 'Cliente';
    if (!esCliente) cliente = null;
    $('acceso-solicitud').hidden = esCliente;
    $('identidad-solicitud').textContent = esCliente
      ? 'Cuenta Cliente: ' + cliente.nombre
      : r.status === 401
        ? 'Inicia sesión aquí para continuar.'
        : 'Necesitas una sesión de Cliente activa. Puedes cambiarla desde Mi sesión.';
    $('contenido-solicitud').hidden = !mismaCuenta();
    if (recibo && cliente?.id === recibo.cliente) {
      mostrarRecibo();
    }
    if (esCliente && !mismaCuenta())
      mensaje(
        'El carrito pertenece a la cuenta anterior. Accede con esa cuenta o vacíalo para empezar con esta.',
      );
    if (mismaCuenta()) dibujarCarrito();
  } catch {
    if (numero === consultaSesion) {
      cliente = null;
      $('acceso-solicitud').hidden = false;
      $('identidad-solicitud').textContent = 'No pudimos comprobar tu sesión. El carrito sigue en memoria.';
    }
  } finally {
    if (numero === consultaSesion) {
      controles();
      posicionPestana.restaurar(!!cliente && cliente.id === identidadAnterior && mismaCuenta());
    }
  }
}
function dibujarCarrito() {
  const lista = $('carrito-solicitud');
  lista.replaceChildren();
  if (!carrito.length) lista.append(nodo('p', 'Todavía no agregaste prendas.'));
  carrito.forEach((d, i) => {
    const card = nodo('article');
    card.className = 'linea-carrito';
    card.append(
      nodo('h3', `${i + 1}. ${d.nombre}`),
      nodo('p', `${d.cantidad} unidades · ${d.talla} · ${d.color} · ${d.tela} · Bolsa estándar`),
      nodo('p', d.sinPersonalizacion ? 'Sin personalización' : d.ideaDiseno || 'Imagen para asesoría'),
      nodo('p', 'Referencia: ' + moneda(centavos(d.precioBaseConfirmado) * BigInt(d.cantidad))),
    );
    if (d.observaciones) card.append(nodo('p', 'Observaciones: ' + d.observaciones));
    if (d.fichero) card.append(nodo('p', 'Archivo seleccionado: ' + d.fichero.name));
    if (d.invalida) {
      const p = nodo('p', 'Esta variante ya no está disponible. Retírala y elige otra.');
      p.className = 'error-variante';
      card.append(p);
    }
    const b = nodo('button', 'Quitar configuración ' + (i + 1));
    b.type = 'button';
    b.className = 'boton-secundario quitar-configuracion';
    b.addEventListener('click', () => {
      if (ocupado || dudoso) return;
      carrito.splice(i, 1);
      envio = null;
      $('confirmar-solicitud').checked = false;
      dibujarCarrito();
      controles();
    });
    card.append(b);
    lista.append(card);
  });
  $('total-solicitud').textContent = 'Referencia: ' + moneda(referencia());
}
function mostrarRecibo() {
  $('contenido-solicitud').hidden = true;
  $('solicitud-confirmada').hidden = false;
  $('codigo-solicitud').textContent = 'Código: ' + recibo.datos.codigo;
  $('resumen-solicitud').textContent =
    'Estado: ' + recibo.datos.estado + ' · Referencia: ' + moneda(centavos(recibo.datos.totalReferencial));
}
async function cargarVariantes() {
  try {
    const r = await fetch('/api/productos', { credentials: 'same-origin', cache: 'no-store' });
    if (!r.ok) throw new Error();
    variantes = await r.json();
    const s = $('variante-solicitud');
    s.replaceChildren();
    const base = nodo('option', 'Selecciona una variante');
    base.value = '';
    s.append(base);
    for (const p of variantes) {
      const o = nodo(
        'option',
        `${p.nombre} · ${p.talla} · ${p.color} · ${p.tela} · ${moneda(centavos(p.precioBase))}`,
      );
      o.value = String(p.id);
      s.append(o);
    }
    const id = new URLSearchParams(location.search).get('producto');
    if (variantes.some((p) => String(p.id) === id)) s.value = id;
    $('estado-variantes').textContent = variantes.length
      ? 'Elige la variante que deseas configurar.'
      : 'No hay variantes disponibles. La administración debe agregar prendas al catálogo.';
    mostrarReferencia();
  } catch {
    $('estado-variantes').textContent =
      'No pudimos cargar las variantes. Puedes volver al catálogo e intentarlo de nuevo.';
  }
}
function mostrarReferencia() {
  const p = variantes.find((p) => p.id === Number(configuracion.elements.producto.value));
  $('referencia-variante').textContent = p ? 'Referencia por unidad: ' + moneda(centavos(p.precioBase)) : '';
}
$('variante-solicitud').addEventListener('change', mostrarReferencia);
$('personalizacion').addEventListener('change', () => {
  const basica = $('personalizacion').value === 'basica';
  $('campos-diseno').hidden = basica;
  if (basica) {
    configuracion.elements.ideaDiseno.value = '';
    configuracion.elements.diseno.value = '';
  }
});
configuracion.addEventListener('submit', (e) => {
  e.preventDefault();
  if (ocupado || dudoso || !mismaCuenta()) return;
  if (!configuracion.reportValidity()) return;
  try {
    if (carrito.length >= 20) throw new Error('Puedes agregar hasta 20 configuraciones en una solicitud.');
    const p = variantes.find((p) => p.id === Number(configuracion.elements.producto.value));
    if (!p) throw new Error('Selecciona una variante disponible.');
    const cantidad = Number(configuracion.elements.cantidad.value);
    if (!Number.isInteger(cantidad) || cantidad < 1 || cantidad > 2147483647)
      throw new Error('Escribe una cantidad entera positiva.');
    const sinPersonalizacion = $('personalizacion').value === 'basica',
      fichero = sinPersonalizacion ? null : configuracion.elements.diseno.files[0] || null;
    const ideaDiseno = sinPersonalizacion ? null : configuracion.elements.ideaDiseno.value.trim() || null,
      observaciones = configuracion.elements.observaciones.value.trim() || null;
    if (!sinPersonalizacion && !fichero && !ideaDiseno)
      throw new Error('Adjunta una imagen, describe tu idea o indica Sin personalización.');
    if ([ideaDiseno, observaciones].some((v) => v && /[\u0000-\u001f\u007f-\u009f]/u.test(v)))
      throw new Error('Escribe idea y observaciones sin caracteres de control.');
    if (
      fichero &&
      (fichero.size < 1 || fichero.size > MAX_ARCHIVO || !/\.(jpg|jpeg|png)$/iu.test(fichero.name))
    )
      throw new Error('Elige una imagen JPG o PNG de hasta 10 MB.');
    if (carrito.reduce((n, d) => n + (d.fichero?.size || 0), 0) + (fichero?.size || 0) > MAX_TOTAL_ARCHIVOS)
      throw new Error('Las imágenes de una solicitud no pueden superar 20 MB en conjunto.');
    const next = referencia() + centavos(p.precioBase) * BigInt(cantidad);
    if (next > 9999999999999999999999999999n) throw new Error('El importe supera el límite de la solicitud.');
    propietario ??= cliente.id;
    carrito.push({
      producto: p.id,
      nombre: p.nombre,
      cantidad,
      talla: p.talla,
      color: p.color,
      tela: p.tela,
      empaque: 'Bolsa estándar',
      sinPersonalizacion,
      ideaDiseno,
      observaciones,
      precioBaseConfirmado: String(p.precioBase),
      archivo: fichero ? 'diseno-' + crypto.randomUUID() : null,
      fichero,
    });
    envio = null;
    $('confirmar-solicitud').checked = false;
    configuracion.reset();
    $('campos-diseno').hidden = false;
    mostrarReferencia();
    dibujarCarrito();
    controles();
    mensaje('Configuración agregada al carrito. Todavía no se ha enviado al servidor.');
  } catch (err) {
    mensaje(err.message);
  }
});
$('modalidad-solicitud').addEventListener('change', () => {
  const domicilio = formulario.elements.modalidadEntrega.value === 'Domicilio';
  $('campo-direccion').hidden = !domicilio;
  formulario.elements.direccionEntrega.required = domicilio;
});
$('actualizar-carrito').addEventListener('click', async () => {
  if (ocupado || dudoso || !mismaCuenta()) return;
  ocupado = true;
  controles();
  try {
    const nuevos = await Promise.all(
      carrito.map(async (d) => {
        const r = await fetch('/api/productos/' + d.producto, {
          cache: 'no-store',
          credentials: 'same-origin',
        });
        if (r.status === 404) return { ...d, invalida: true };
        if (!r.ok) throw new Error('No pudimos actualizar las variantes. Tu carrito se conserva.');
        const p = await r.json();
        return {
          ...d,
          nombre: p.nombre,
          talla: p.talla,
          color: p.color,
          tela: p.tela,
          precioBaseConfirmado: String(p.precioBase),
          invalida: false,
        };
      }),
    );
    carrito = nuevos;
    envio = null;
    $('confirmar-solicitud').checked = false;
    dibujarCarrito();
    mensaje(
      'Variantes y referencias actualizadas. Revisa de nuevo talla, color, tela y precio antes de marcar la confirmación. Las imágenes y observaciones se conservaron.',
    );
  } catch (err) {
    mensaje(err.message);
  } finally {
    ocupado = false;
    controles();
  }
});
formulario.addEventListener('submit', async (e) => {
  e.preventDefault();
  if (ocupado || !mismaCuenta() || !carrito.length || carrito.some((d) => d.invalida)) return;
  if (!dudoso && !formulario.reportValidity()) return;
  if (!envio) {
    const modalidadEntrega = formulario.elements.modalidadEntrega.value,
      direccionEntrega =
        modalidadEntrega === 'Domicilio' ? formulario.elements.direccionEntrega.value.trim() : null;
    if (
      modalidadEntrega === 'Domicilio' &&
      (!direccionEntrega || /[\u0000-\u001f\u007f-\u009f]/u.test(direccionEntrega))
    ) {
      mensaje('Escribe la dirección de entrega sin caracteres de control.');
      return;
    }
    envio = {
      datos: {
        confirmacion: crypto.randomUUID(),
        clienteConfirmado: cliente.id,
        modalidadEntrega,
        direccionEntrega,
        totalReferencialConfirmado: decimal(referencia()),
        detalles: carrito.map(({ nombre, fichero, invalida, ...d }) => d),
      },
      archivos: carrito.filter((d) => d.fichero).map((d) => ({ nombre: d.archivo, fichero: d.fichero })),
    };
  }
  ocupado = true;
  controles();
  mensaje('Enviando tu solicitud…');
  try {
    dudoso = true;
    await sesion();
    if (!mismaCuenta() || cliente.id !== envio.datos.clienteConfirmado) {
      mensaje('Accede con la cuenta que preparó el carrito. No se envió la solicitud con otra identidad.');
      return;
    }
    const titularEnvio = envio.datos.clienteConfirmado;
    const form = new FormData();
    form.append('datos', JSON.stringify(envio.datos));
    for (const f of envio.archivos) form.append(f.nombre, f.fichero, f.fichero.name);
    const r = await fetch('/api/solicitudes', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'X-CSRF-TOKEN': await csrf() },
      body: form,
    });
    const b = await r.json().catch(() => null);
    if (r.ok && b?.codigo) {
      carrito = [];
      envio = null;
      dudoso = false;
      propietario = null;
      dibujarCarrito();
      formulario.reset();
      configuracion.reset();
      $('campo-direccion').hidden = true;
      recibo = { cliente: titularEnvio, datos: b };
      await sesion();
      if (cliente?.id === titularEnvio) {
        mensaje('Solicitud registrada correctamente. El carrito se vació después de confirmar el guardado.');
        $('solicitud-confirmada').focus();
      } else
        mensaje(
          'La solicitud se registró para la cuenta que la confirmó. El resultado se mostrará al acceder con esa cuenta.',
        );
    } else if (r.status === 401) {
      dudoso = true;
      await sesion();
      mensaje('Tu sesión venció. Accede aquí con la misma cuenta y reintenta; el carrito sigue en memoria.');
    } else if (r.status === 400 && b?.codigo === 'formulario_vencido') {
      dudoso = true;
      await sesion();
      mensaje('La sesión cambió o la protección venció. Accede con la misma cuenta y reintenta la solicitud conservada.');
    } else if (r.status === 400 || r.status === 413 || (r.status === 409 && b?.campo === 'catalogo')) {
      envio = null;
      dudoso = false;
      $('confirmar-solicitud').checked = false;
      const errores = b?.errors ? Object.values(b.errors).flat().join(' ') : '';
      mensaje(
        b?.title
          ? [b.title, errores].filter(Boolean).join(' ')
          : 'Revisa la solicitud y el tamaño de sus archivos.',
      );
    } else {
      dudoso = true;
      mensaje(
        'No pudimos confirmar el resultado. Conservamos la misma solicitud: reintenta sin cambiar los datos para evitar duplicados.',
      );
      if (r.status === 403) await sesion();
    }
  } catch {
    dudoso = true;
    mensaje(
      'No recibimos la confirmación. Conservamos tu carrito y la misma solicitud; usa Reintentar para comprobar el guardado sin duplicarlo.',
    );
  } finally {
    if (revalidarPendiente) {
      revalidarPendiente = false;
      await sesion();
    }
    ocupado = false;
    controles();
  }
});
$('acceso-en-pagina').addEventListener('submit', async (e) => {
  e.preventDefault();
  if (ocupado) return;
  const f = e.currentTarget;
  ocupado = true;
  controles();
  try {
    const r = await fetch('/api/sesion', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await csrf() },
      body: JSON.stringify({
        correo: f.elements.correo.value.trim(),
        contrasena: f.elements.contrasena.value,
      }),
    });
    const b = await r.json().catch(() => null);
    f.elements.contrasena.value = '';
    if (r.ok || r.status === 409) {
      await sesion();
      if (mismaCuenta()) mensaje('Sesión comprobada. Tu carrito permanece en esta página.');
    } else
      mensaje(
        [400, 401, 429].includes(r.status) && b?.title
          ? b.title
          : 'No pudimos iniciar sesión. Inténtalo de nuevo.',
      );
  } catch {
    mensaje('No pudimos confirmar el acceso. Tu carrito se conserva.');
  } finally {
    ocupado = false;
    controles();
  }
});
$('vaciar-otra-cuenta').addEventListener('click', () => {
  if (ocupado) return;
  carrito = [];
  envio = null;
  dudoso = false;
  propietario = null;
  formulario.reset();
  configuracion.reset();
  $('campo-direccion').hidden = true;
  $('contenido-solicitud').hidden = !mismaCuenta();
  dibujarCarrito();
  controles();
  mensaje('Carrito anterior descartado. Puedes preparar una solicitud con esta cuenta.');
});
$('nueva-solicitud').addEventListener('click', () => {
  if (ocupado) return;
  recibo = null;
  $('solicitud-confirmada').hidden = true;
  $('codigo-solicitud').textContent = '';
  $('resumen-solicitud').textContent = '';
  $('contenido-solicitud').hidden = !mismaCuenta();
  mensaje('');
  controles();
});
window.addEventListener('pageshow', (e) => {
  if (e.persisted) {
    if (ocupado) revalidarPendiente = true;
    else sesion();
  }
});
document.addEventListener('visibilitychange', () => {
  if (document.hidden) {
    $('contenido-solicitud').hidden = true;
    $('carrito-solicitud').replaceChildren();
    $('solicitud-confirmada').hidden = true;
    $('codigo-solicitud').textContent = '';
    $('resumen-solicitud').textContent = '';
  } else if (ocupado) revalidarPendiente = true;
  else sesion();
});
Promise.all([sesion(), cargarVariantes()]);
