'use strict';

const posicionPestana = window.crearPosicionPestana();
const formulario = document.getElementById('formulario-acceso');
const resultado = document.getElementById('resultado');
const cuenta = document.getElementById('cuenta');
const entrar = document.getElementById('iniciar-sesion');
const cerrar = document.getElementById('cerrar-sesion');
const contrasena = document.getElementById('contrasena');
let ocupado = true;
let revalidarPendiente = false;
let consultaActual = 0;

function mensaje(texto, errores = {}) {
  resultado.textContent = texto;
  resultado.hidden = !texto;
  formulario.querySelectorAll('[aria-invalid]').forEach(c => c.removeAttribute('aria-invalid'));
  formulario.querySelectorAll('.error-campo').forEach(c => { c.textContent = ''; });
  let primero;
  for (const [nombre, mensajes] of Object.entries(errores)) {
    const campo = formulario.elements.namedItem(nombre);
    const error = document.getElementById(`error-${nombre}`);
    if (campo && error) {
      campo.setAttribute('aria-invalid', 'true');
      error.textContent = Array.isArray(mensajes) ? mensajes.join(' ') : String(mensajes);
      primero ||= campo;
    }
  }
  if (texto) (primero || resultado).focus();
}

function mostrarCuenta(datos) {
  document.getElementById('titulo-acceso').textContent = 'Sesión iniciada';
  document.querySelector('.introduccion').textContent = 'Puedes cerrar tu sesión desde aquí.';
  formulario.reset();
  contrasena.type = 'password';
  document.getElementById('mostrar-contrasena').textContent = 'Mostrar';
  document.getElementById('mostrar-contrasena').setAttribute('aria-pressed', 'false');
  formulario.hidden = true;
  cuenta.hidden = false;
  document.getElementById('nombre-cuenta').textContent = `Bienvenido, ${datos.nombre}.`;
  document.getElementById('rol-cuenta').textContent = `Acceso: ${datos.rol}.`;
  document.getElementById('opcion-administracion').hidden = datos.rol !== 'Administrador';
  document.getElementById('opcion-solicitud').hidden = datos.rol !== 'Cliente';
  cuenta.focus({ preventScroll: posicionPestana.pendiente });
}

function mostrarFormulario() {
  document.getElementById('titulo-acceso').textContent = 'Inicia sesión';
  document.querySelector('.introduccion').textContent = 'Ingresa con el correo y la contraseña de tu cuenta.';
  cuenta.hidden = true;
  formulario.hidden = false;
  document.getElementById('nombre-cuenta').textContent = '';
  document.getElementById('rol-cuenta').textContent = '';
  document.getElementById('opcion-administracion').hidden = true;
  document.getElementById('opcion-solicitud').hidden = true;
}

async function token() {
  // Obtener un token nuevo para la identidad actual, también después de entrar o salir.
  const respuesta = await fetch('/api/seguridad/token', { credentials: 'same-origin', cache: 'no-store' });
  if (!respuesta.ok) throw new Error('Formulario no disponible');
  return (await respuesta.json()).token;
}

document.getElementById('mostrar-contrasena').addEventListener('click', evento => {
  const mostrar = contrasena.type === 'password';
  contrasena.type = mostrar ? 'text' : 'password';
  evento.currentTarget.textContent = mostrar ? 'Ocultar' : 'Mostrar';
  evento.currentTarget.setAttribute('aria-pressed', String(mostrar));
});

formulario.addEventListener('submit', async evento => {
  evento.preventDefault();
  if (ocupado) return;
  mensaje('');
  const datos = { correo: formulario.elements.correo.value.trim(), contrasena: contrasena.value };
  const errores = {};
  if (!datos.correo || datos.correo.length > 150 || !/^[^\s@]+@[^\s@]+$/u.test(datos.correo))
    errores.correo = ['Escribe tu correo electrónico.'];
  if (!datos.contrasena.trim() || new TextEncoder().encode(datos.contrasena).length > 72
      || /[\u0000-\u001f\u007f-\u009f]/u.test(datos.contrasena))
    errores.contrasena = ['Escribe tu contraseña completa, sin superar 72 bytes.'];
  if (Object.keys(errores).length) { mensaje('Revisa los campos indicados.', errores); return; }
  ++consultaActual;
  revalidarPendiente = revalidarPendiente || posicionPestana.pendiente;
  ocupado = true;
  entrar.disabled = true;
  entrar.textContent = 'Iniciando sesión…';
  try {
    const respuesta = await fetch('/api/sesion', {
      method: 'POST', credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': await token() },
      body: JSON.stringify(datos)
    });
    const cuerpo = await respuesta.json().catch(() => null);
    if (respuesta.ok) mostrarCuenta(cuerpo);
    else if (respuesta.status === 409) await consultar();
    else mensaje([400, 401, 429].includes(respuesta.status) && cuerpo?.title
      ? cuerpo.title : 'No pudimos iniciar sesión. Inténtalo en unos momentos.', cuerpo?.errors);
  } catch { mensaje('No pudimos confirmar el acceso. Comprueba tu conexión y vuelve a intentarlo.'); }
  finally {
    ocupado = false; entrar.disabled = false; entrar.textContent = 'Entrar a mi cuenta ↗';
    if (revalidarPendiente) await revalidarAlVolver();
  }
});

cerrar.addEventListener('click', async () => {
  if (ocupado) return;
  ++consultaActual;
  revalidarPendiente = revalidarPendiente || posicionPestana.pendiente;
  ocupado = true;
  cerrar.disabled = true;
  mensaje('');
  try {
    const respuesta = await fetch('/api/sesion', {
      method: 'DELETE', credentials: 'same-origin', headers: { 'X-CSRF-TOKEN': await token() }
    });
    if (respuesta.ok || respuesta.status === 401) {
      mostrarFormulario(); mensaje('Tu sesión se ha cerrado.');
    } else mensaje('No pudimos cerrar la sesión. Vuelve a intentarlo.');
  } catch { mensaje('No pudimos confirmar el cierre. Comprueba tu conexión y vuelve a intentarlo.'); }
  finally {
    ocupado = false; cerrar.disabled = false;
    if (revalidarPendiente) await revalidarAlVolver();
  }
});

async function consultar() {
  const turno = ++consultaActual;
  try {
    const respuesta = await fetch('/api/sesion', { credentials: 'same-origin', cache: 'no-store' });
    const datos = respuesta.ok ? await respuesta.json() : null;
    if (turno !== consultaActual) return;
    if (respuesta.ok) mostrarCuenta(datos);
    else if (respuesta.status === 401) { mostrarFormulario(); }
    else throw new Error('No se pudo consultar la sesión');
  } catch (error) {
    if (turno === consultaActual) throw error;
  } finally { if (turno === consultaActual) posicionPestana.restaurar(); }
}

function revalidarAlVolver() {
  if (ocupado) { revalidarPendiente = true; return; }
  revalidarPendiente = false;
  return consultar().catch(() => { mostrarFormulario(); mensaje('No pudimos comprobar tu sesión.'); });
}

entrar.disabled = true;
consultar().catch(() => mensaje('No pudimos consultar tu sesión. Comprueba tu conexión e inténtalo de nuevo.'))
  .finally(async () => {
    ocupado = false; entrar.disabled = false;
    if (revalidarPendiente) await revalidarAlVolver();
  });
// Revalidar al volver a la pestaña evita conservar datos visibles de una sesión cerrada en otra.
window.addEventListener('pageshow', evento => { if (evento.persisted) revalidarAlVolver(); });
document.addEventListener('visibilitychange', () => {
  if (document.hidden) ++consultaActual;
  else revalidarAlVolver();
});
