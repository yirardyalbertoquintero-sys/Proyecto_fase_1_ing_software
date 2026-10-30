'use strict';

const formulario = document.getElementById('formulario-producto');
const resultado = document.getElementById('resultado');
const guardar = document.getElementById('guardar-producto');
let productos = [];
let actual = null;
let ocupado = false;
let imagenSubida = null;
let vistaLocal = null;

function aviso(texto, errores = {}) {
  resultado.textContent = texto;
  resultado.hidden = !texto;
  formulario.querySelectorAll('.error-campo').forEach(e => { e.textContent = ''; });
  formulario.querySelectorAll('[aria-invalid]').forEach(e => e.removeAttribute('aria-invalid'));
  for (const [nombre, mensajes] of Object.entries(errores)) {
    const error = document.getElementById(`error-${nombre}`);
    if (error) {
      error.textContent = Array.isArray(mensajes) ? mensajes.join(' ') : String(mensajes);
      formulario.elements.namedItem(nombre === 'imagenURL' ? 'imagen' : nombre)?.setAttribute('aria-invalid', 'true');
    }
  }
  if (texto) resultado.focus();
}

function denegar() {
  document.getElementById('espacio-catalogo').hidden = true;
  document.getElementById('acceso-denegado').hidden = false;
  productos = [];
  document.getElementById('lista-variantes').replaceChildren();
  formulario.reset(); actual = null; imagenSubida = null;
}

async function solicitar(ruta, opciones) {
  try { return await fetch(ruta, opciones); }
  catch { throw new Error('No pudimos confirmar la operación. Comprueba tu conexión y actualiza el listado antes de reintentar.'); }
}

async function enviar(ruta, opciones = {}) {
  if (opciones.method) {
    const tokenRespuesta = await solicitar('/api/seguridad/token', { credentials: 'same-origin', cache: 'no-store' });
    if (!tokenRespuesta.ok) throw new Error('No se pudo preparar el formulario.');
    const { token } = await tokenRespuesta.json();
    opciones.headers = { ...opciones.headers, 'X-CSRF-TOKEN': token };
  }
  const respuesta = await solicitar(ruta, { ...opciones, credentials: 'same-origin', cache: 'no-store' });
  if (respuesta.status === 401 || respuesta.status === 403) { denegar(); throw new Error('La sesión terminó o no tiene permisos de Administrador.'); }
  const cuerpo = await respuesta.json().catch(() => null);
  if (!respuesta.ok) {
    const error = new Error([400, 409, 429].includes(respuesta.status) && cuerpo?.title ? cuerpo.title : 'No pudimos completar la operación. Vuelve a intentarlo.');
    error.errores = cuerpo?.errors; throw error;
  }
  return cuerpo;
}

function moneda(valor) {
  const [entero, decimales = ''] = String(valor).split('.');
  return 'C$' + entero.replace(/\B(?=(\d{3})+(?!\d))/g, ',') + '.' + decimales.padEnd(2, '0');
}

function elemento(etiqueta, texto, clase) {
  const e = document.createElement(etiqueta);
  if (texto !== undefined) e.textContent = texto;
  if (clase) e.className = clase;
  return e;
}

function dibujar() {
  document.getElementById('total-variantes').textContent = productos.length;
  document.getElementById('total-activas').textContent = productos.filter(p => p.activo).length;
  document.getElementById('total-inactivas').textContent = productos.filter(p => !p.activo).length;
  const buscar = document.getElementById('buscar').value.trim().toLocaleLowerCase('es');
  const estado = document.getElementById('filtro-estado').value;
  const visibles = productos.filter(p => (estado === 'todas' || p.activo === (estado === 'activas'))
    && [p.nombre, p.color, p.talla, p.tela].join(' ').toLocaleLowerCase('es').includes(buscar));
  const lista = document.getElementById('lista-variantes'); lista.replaceChildren();
  for (const producto of visibles) {
    const tarjeta = elemento('article', undefined, 'variante');
    const imagen = elemento('img'); imagen.src = producto.imagenURL; imagen.alt = producto.nombre; imagen.loading = 'lazy';
    const datos = elemento('div');
    datos.append(elemento('span', producto.activo ? 'Disponible' : 'Inactiva', 'etiqueta-estado' + (producto.activo ? '' : ' inactiva')),
      elemento('h3', producto.nombre), elemento('p', `${producto.color} · Talla ${producto.talla} · ${producto.tela}`));
    const precio = elemento('p', moneda(producto.precioBase), 'precio'); precio.append(elemento('span', 'Precio de referencia'));
    const editar = elemento('button', 'Editar variante', 'boton-secundario'); editar.type = 'button';
    editar.addEventListener('click', () => { if (!ocupado) editarProducto(producto); });
    datos.append(precio, editar); tarjeta.append(imagen, datos); lista.append(tarjeta);
  }
  document.getElementById('estado-listado').textContent = productos.length === 0 ? 'Aún no hay variantes. Crea la primera con el formulario.'
    : visibles.length === 0 ? 'No hay variantes que coincidan con tu búsqueda.' : `${visibles.length} variante(s) en el listado.`;
}

async function cargar() { productos = await enviar('/api/administracion/productos'); dibujar(); }

function vista(referencia) {
  if (vistaLocal) { URL.revokeObjectURL(vistaLocal); vistaLocal = null; }
  const imagen = document.getElementById('vista-imagen');
  imagen.hidden = !referencia;
  if (referencia) imagen.src = referencia; else imagen.removeAttribute('src');
}

function editarProducto(producto) {
  actual = producto; imagenSubida = null; formulario.reset();
  for (const nombre of ['nombre', 'color', 'talla', 'tela', 'precioBase']) formulario.elements[nombre].value = producto[nombre];
  formulario.elements.activo.checked = producto.activo;
  vista(producto.imagenURL);
  document.getElementById('titulo-editor').textContent = 'Editar variante';
  guardar.textContent = 'Guardar cambios'; aviso('');
  document.getElementById('titulo-editor').focus();
}

function limpiar() {
  if (ocupado) return;
  actual = null; imagenSubida = null; formulario.reset(); vista(null); aviso('');
  document.getElementById('titulo-editor').textContent = 'Nueva variante'; guardar.textContent = 'Crear variante';
  formulario.elements.nombre.focus();
}

document.getElementById('imagen').addEventListener('change', evento => {
  imagenSubida = null;
  const archivo = evento.target.files[0];
  vista(null);
  if (archivo) { vistaLocal = URL.createObjectURL(archivo); document.getElementById('vista-imagen').src = vistaLocal; document.getElementById('vista-imagen').hidden = false; }
});

formulario.addEventListener('submit', async evento => {
  evento.preventDefault(); if (ocupado) return;
  aviso('');
  const datos = {};
  const errores = {};
  for (const [campo, maximo] of [['nombre',100],['color',50],['talla',30],['tela',100]]) {
    datos[campo] = formulario.elements[campo].value.trim();
    if (!datos[campo] || datos[campo].length > maximo || /[\u0000-\u001f\u007f-\u009f]/u.test(datos[campo])) errores[campo] = [`Completa este campo sin superar ${maximo} caracteres.`];
  }
  datos.precioBase = formulario.elements.precioBase.value.trim();
  if (!/^\d{1,16}(\.\d{1,2})?$/u.test(datos.precioBase) || !/[1-9]/u.test(datos.precioBase)) errores.precioBase = ['Indica un precio positivo con hasta dos decimales.'];
  datos.activo = formulario.elements.activo.checked;
  if (actual) datos.version = actual.version;
  const archivo = formulario.elements.imagen.files[0];
  if (!archivo && !actual?.imagenURL && !imagenSubida) errores.imagenURL = ['Selecciona una foto pública de la prenda.'];
  if (archivo && (archivo.size <= 0 || archivo.size > 10000000 || !/\.(jpg|jpeg|png)$/iu.test(archivo.name))) errores.imagenURL = ['Selecciona un JPG o PNG de hasta 10 MB.'];
  if (Object.keys(errores).length) { aviso('Revisa los campos indicados.', errores); return; }
  ocupado = true;
  const controles = Array.from(formulario.elements); controles.forEach(c => { c.disabled = true; });
  guardar.textContent = 'Guardando variante…';
  try {
    if (archivo && !imagenSubida) {
      const contenido = new FormData(); contenido.append('archivo', archivo);
      imagenSubida = (await enviar('/api/imagenes-catalogo', { method: 'POST', body: contenido })).imagenURL;
    }
    datos.imagenURL = imagenSubida || actual?.imagenURL;
    const guardado = await enviar('/api/administracion/productos' + (actual ? '/' + actual.id : ''),
      { method: actual ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(datos) });
    actual = guardado; imagenSubida = null;
    formulario.elements.imagen.value = ''; vista(guardado.imagenURL);
    document.getElementById('titulo-editor').textContent = 'Editar variante';
    aviso('La variante se guardó correctamente.');
    await cargar();
  } catch (error) { aviso(error.message || 'No pudimos confirmar el guardado. Actualiza el listado antes de reintentar.', error.errores); }
  finally { ocupado = false; controles.forEach(c => { c.disabled = false; }); guardar.textContent = actual ? 'Guardar cambios' : 'Crear variante'; }
});

document.getElementById('nueva-variante').addEventListener('click', limpiar);
document.getElementById('cancelar-edicion').addEventListener('click', limpiar);
document.getElementById('buscar').addEventListener('input', dibujar);
document.getElementById('filtro-estado').addEventListener('change', dibujar);
document.getElementById('recargar').addEventListener('click', () => { if (!ocupado) cargar().catch(e => aviso(e.message)); });

async function iniciar() {
  try {
    const sesion = await enviar('/api/sesion');
    if (sesion.rol !== 'Administrador') { denegar(); return; }
    document.getElementById('espacio-catalogo').hidden = false;
    await cargar();
  } catch (error) { aviso(error.message || 'No se pudo cargar el catálogo.'); }
}
iniciar();
document.addEventListener('visibilitychange', () => { if (!document.hidden && !ocupado) iniciar(); });
window.addEventListener('pageshow', evento => {
  if (evento.persisted) { document.getElementById('espacio-catalogo').hidden = true; iniciar(); }
});
