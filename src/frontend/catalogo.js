'use strict';

const lista = document.getElementById('lista-prendas');
const estado = document.getElementById('estado-catalogo');
const errorCatalogo = document.getElementById('error-catalogo');
const filtros = document.getElementById('filtros-catalogo');
const actualizar = document.getElementById('actualizar-catalogo');
const dialogo = document.getElementById('detalle-prenda');
let productos = [];
let consultaListado = null;
let consultaDetalle = null;
let operacionListado = 0;
let operacionDetalle = 0;
let detalleId = null;

function elemento(tipo, texto, clase) {
  const nodo = document.createElement(tipo);
  if (texto !== undefined) nodo.textContent = texto;
  if (clase) nodo.className = clase;
  return nodo;
}

function moneda(valor) {
  const texto = String(valor);
  if (!/^\d{1,16}(\.\d{1,2})?$/u.test(texto)) return 'Precio no disponible';
  const [entero, decimal = ''] = texto.split('.');
  return 'C$' + entero.replace(/\B(?=(\d{3})+(?!\d))/g, ',') + '.' + decimal.padEnd(2, '0');
}

function foto(contenedor, referencia, nombre, diferida = false) {
  contenedor.replaceChildren();
  const sinImagen = () => elemento('span', 'Imagen no disponible', 'sin-imagen');
  if (typeof referencia !== 'string' || !/^\/api\/imagenes-catalogo\/[a-f0-9]{32}\.(jpg|png)$/u.test(referencia)) { contenedor.append(sinImagen()); return; }
  const imagen = elemento('img');
  imagen.alt = nombre;
  imagen.loading = diferida ? 'lazy' : 'eager';
  imagen.addEventListener('error', () => {
    // Una foto retirada de un detalle anterior no puede reemplazar la foto actual.
    if (imagen.parentNode === contenedor) contenedor.replaceChildren(sinImagen());
  }, { once: true });
  imagen.src = referencia;
  contenedor.append(imagen);
}

async function consultar(ruta, signal) {
  let respuesta;
  try { respuesta = await fetch(ruta, { credentials: 'same-origin', cache: 'no-store', signal }); }
  catch (error) {
    if (error.name === 'AbortError') throw error;
    throw new Error('No pudimos consultar las prendas. Comprueba tu conexión y vuelve a intentarlo.');
  }
  if (!respuesta.ok) {
    const error = new Error(respuesta.status === 404
      ? 'Esta variante ya no está disponible. Actualiza el catálogo para ver las prendas disponibles.'
      : 'No pudimos consultar las prendas en este momento. Vuelve a intentarlo.');
    error.estado = respuesta.status;
    throw error;
  }
  try { return await respuesta.json(); }
  catch { throw new Error('No pudimos leer el catálogo. Vuelve a intentarlo.'); }
}

function opciones() {
  for (const campo of ['talla', 'color', 'tela']) {
    const selector = filtros.elements[campo];
    const anterior = selector.value;
    const valores = [...new Set(productos.map(p => p[campo]))].sort((a,b) => a.localeCompare(b,'es',{numeric:true}));
    const todas = elemento('option', campo === 'color' ? 'Todos' : 'Todas'); todas.value = '';
    selector.replaceChildren(todas);
    for (const valor of valores) { const opcion = elemento('option', valor); opcion.value = valor; selector.append(opcion); }
    selector.value = valores.includes(anterior) ? anterior : '';
  }
}

function dibujar() {
  const buscar = filtros.elements.buscar.value.trim().toLocaleLowerCase('es');
  const visibles = productos.filter(p => ['talla','color','tela'].every(c => !filtros.elements[c].value || filtros.elements[c].value === p[c])
    && [p.nombre,p.color,p.talla,p.tela].join(' ').toLocaleLowerCase('es').includes(buscar));
  lista.replaceChildren();
  for (const producto of visibles) {
    const tarjeta = elemento('article', undefined, 'prenda');
    const imagen = elemento('div', undefined, 'foto-prenda'); foto(imagen, producto.imagenURL, producto.nombre, true);
    const datos = elemento('div', undefined, 'datos-prenda');
    datos.append(elemento('p','DISPONIBLE EN CATÁLOGO','ceja ceja-oscura'),elemento('h3',producto.nombre),
      elemento('p',`${producto.color} · Talla ${producto.talla} · ${producto.tela}`,'caracteristicas-prenda'),
      elemento('p',moneda(producto.precioBase),'precio-prenda'),elemento('p','Precio de referencia','etiqueta-referencia'));
    const ver = elemento('button','Ver prenda','boton-secundario'); ver.type = 'button';
    ver.setAttribute('aria-label', `Ver ${producto.nombre}, talla ${producto.talla}, color ${producto.color}`);
    ver.addEventListener('click', () => abrirDetalle(producto.id));
    datos.append(ver); tarjeta.append(imagen,datos); lista.append(tarjeta);
  }
  estado.textContent = productos.length === 0 ? 'Por el momento no hay prendas disponibles. Puedes volver a consultar más adelante.'
    : visibles.length === 0 ? 'No encontramos prendas con esos filtros. Prueba otra combinación o limpia los filtros.'
      : `${visibles.length} ${visibles.length === 1 ? 'variante disponible' : 'variantes disponibles'}. Cada tarjeta corresponde a una talla, color y tela.`;
}

async function cargarCatalogo() {
  const seleccionAnterior = Object.fromEntries(['talla','color','tela'].map(c => [c, filtros.elements[c].value]));
  const operacion = ++operacionListado;
  consultaListado?.abort(); consultaListado = new AbortController();
  actualizar.disabled = true; lista.setAttribute('aria-busy','true');
  errorCatalogo.hidden = true; productos = []; lista.replaceChildren(); opciones();
  estado.textContent = 'Cargando prendas…';
  try {
    const datos = await consultar('/api/productos',consultaListado.signal);
    if (operacion !== operacionListado) return;
    productos = datos; opciones();
    for (const campo of ['talla','color','tela'])
      if ([...filtros.elements[campo].options].some(o => o.value === seleccionAnterior[campo])) filtros.elements[campo].value = seleccionAnterior[campo];
    dibujar();
  } catch (error) {
    if (operacion !== operacionListado || error.name === 'AbortError') return;
    estado.textContent = '';
    errorCatalogo.textContent = error.message; errorCatalogo.hidden = false;
  } finally {
    if (operacion === operacionListado) { actualizar.disabled = false; lista.removeAttribute('aria-busy'); }
  }
}

function enlaceDetalle(id) {
  const url = new URL(location.href);
  if (id) url.searchParams.set('producto', String(id)); else url.searchParams.delete('producto');
  history.replaceState(null,'',url.pathname+url.search+url.hash);
}

async function abrirDetalle(id) {
  const operacion = ++operacionDetalle;
  consultaDetalle?.abort(); consultaDetalle = new AbortController(); detalleId = id;
  const titulo = document.getElementById('titulo-detalle');
  const mensaje = document.getElementById('estado-detalle');
  const errorDetalle = document.getElementById('error-detalle');
  const contenido = document.getElementById('contenido-detalle');
  contenido.hidden = true; errorDetalle.hidden = true;
  document.getElementById('foto-detalle').replaceChildren();
  titulo.textContent = 'Detalle de la prenda'; mensaje.textContent = 'Cargando características…';
  document.getElementById('actualizar-detalle').disabled = true;
  if (!dialogo.open) dialogo.showModal();
  enlaceDetalle(id);
  try {
    const producto = await consultar('/api/productos/'+id, consultaDetalle.signal);
    if (operacion !== operacionDetalle || !dialogo.open) return;
    titulo.textContent = producto.nombre;
    foto(document.getElementById('foto-detalle'),producto.imagenURL,producto.nombre);
    const caracteristicas = document.getElementById('caracteristicas-detalle'); caracteristicas.replaceChildren();
    for (const [etiqueta,campo] of [['Talla','talla'],['Color','color'],['Tela','tela']])
      caracteristicas.append(elemento('dt',etiqueta),elemento('dd',producto[campo]));
    document.getElementById('precio-detalle').textContent = moneda(producto.precioBase);
    mensaje.textContent = ''; contenido.hidden = false; titulo.focus();
  } catch (error) {
    if (operacion !== operacionDetalle || error.name === 'AbortError' || !dialogo.open) return;
    mensaje.textContent = ''; errorDetalle.textContent = error.message; errorDetalle.hidden = false;
    if (error.estado === 404) await cargarCatalogo();
  } finally {
    if (operacion === operacionDetalle) document.getElementById('actualizar-detalle').disabled = false;
  }
}

filtros.addEventListener('submit', evento => evento.preventDefault());
filtros.addEventListener('input', () => { if (!actualizar.disabled) dibujar(); });
document.getElementById('limpiar-filtros').addEventListener('click', () => { filtros.reset(); if (!actualizar.disabled) dibujar(); });
actualizar.addEventListener('click', cargarCatalogo);
document.getElementById('cerrar-detalle').addEventListener('click', () => dialogo.close());
document.getElementById('actualizar-detalle').addEventListener('click', () => { if (detalleId) abrirDetalle(detalleId); });
dialogo.addEventListener('close', () => { ++operacionDetalle; consultaDetalle?.abort(); detalleId = null; enlaceDetalle(null); });

async function revalidar() { await cargarCatalogo(); if (dialogo.open && detalleId) await abrirDetalle(detalleId); }
document.addEventListener('visibilitychange', () => { if (!document.hidden) revalidar(); });
window.addEventListener('pageshow', evento => { if (evento.persisted) revalidar(); });

cargarCatalogo();
const enlace = new URLSearchParams(location.search).get('producto');
if (enlace !== null) {
  if (/^[1-9]\d{0,9}$/u.test(enlace) && Number(enlace) <= 2147483647) abrirDetalle(Number(enlace));
  else { errorCatalogo.textContent = 'El enlace de la prenda no es válido. Puedes consultarla desde el catálogo.'; errorCatalogo.hidden = false; enlaceDetalle(null); }
}
