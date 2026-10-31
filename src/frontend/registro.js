'use strict';

const formulario = document.getElementById('formulario-registro');
const resultado = document.getElementById('resultado');
const enviar = document.getElementById('crear-cuenta');
const contrasena = document.getElementById('contrasena');
let enviando = false;

function limpiarErrores() {
  resultado.hidden = true;
  resultado.textContent = '';
  formulario.querySelectorAll('[aria-invalid]').forEach(campo => campo.removeAttribute('aria-invalid'));
  formulario.querySelectorAll('.error-campo').forEach(error => { error.textContent = ''; });
}

function mostrarErrores(errores, mensaje) {
  let primero;
  for (const [nombre, mensajes] of Object.entries(errores || {})) {
    const campo = formulario.elements.namedItem(nombre);
    const error = document.getElementById(`error-${nombre}`);
    if (campo && error) {
      campo.setAttribute('aria-invalid', 'true');
      error.textContent = Array.isArray(mensajes) ? mensajes.join(' ') : String(mensajes);
      primero ||= campo;
    }
  }
  resultado.textContent = mensaje;
  resultado.hidden = false;
  (primero || resultado).focus();
}

function validar(datos) {
  const errores = {};
  if (!datos.nombre || datos.nombre.length > 100 || /[\u0000-\u001f\u007f-\u009f]/u.test(datos.nombre))
    errores.nombre = ['Escribe un nombre de entre 1 y 100 caracteres.'];
  if (!datos.correo || datos.correo.length > 150 || !/^[^\s@]+@[^\s@]+$/u.test(datos.correo))
    errores.correo = ['Escribe un correo electrónico válido.'];
  if (!datos.contrasena.trim() || [...datos.contrasena].length < 8)
    errores.contrasena = ['Usa al menos 8 caracteres.'];
  else if (new TextEncoder().encode(datos.contrasena).length > 72)
    errores.contrasena = ['La contraseña supera el límite de 72 bytes. Las tildes y los emojis pueden ocupar más de uno.'];
  else if (/[\u0000-\u001f\u007f-\u009f]/u.test(datos.contrasena))
    errores.contrasena = ['La contraseña no puede contener saltos de línea ni caracteres de control.'];
  else if (!/\p{Lu}/u.test(datos.contrasena)
    || !/\p{Ll}/u.test(datos.contrasena)
    || !/\p{Nd}/u.test(datos.contrasena)
    || !/[\p{P}\p{S}]/u.test(datos.contrasena))
    errores.contrasena = ['Incluye al menos una mayúscula, una minúscula, un número y un símbolo.'];
  if (datos.contrasena !== formulario.elements.confirmacion.value)
    errores.confirmacion = ['Las contraseñas no coinciden.'];
  if (!datos.aceptacionTerminos)
    errores.aceptacionTerminos = ['Lee y acepta los términos y el aviso de privacidad para continuar.'];
  return errores;
}

document.getElementById('mostrar-contrasena').addEventListener('click', evento => {
  const mostrar = contrasena.type === 'password';
  contrasena.type = mostrar ? 'text' : 'password';
  evento.currentTarget.textContent = mostrar ? 'Ocultar' : 'Mostrar';
  evento.currentTarget.setAttribute('aria-pressed', String(mostrar));
});

formulario.addEventListener('submit', async evento => {
  evento.preventDefault();
  if (enviando) return;
  limpiarErrores();
  const datos = {
    nombre: formulario.elements.nombre.value.trim(),
    correo: formulario.elements.correo.value.trim(),
    contrasena: contrasena.value,
    aceptacionTerminos: formulario.elements.aceptacionTerminos.checked
  };
  const errores = validar(datos);
  if (Object.keys(errores).length) {
    mostrarErrores(errores, 'Revisa los campos indicados para crear tu cuenta.');
    return;
  }
  enviando = true;
  enviar.disabled = true;
  enviar.textContent = 'Creando tu cuenta…';
  try {
    const seguridad = await fetch('/api/seguridad/token', { credentials: 'same-origin', cache: 'no-store' });
    if (!seguridad.ok) throw new Error('No se pudo preparar el formulario');
    const { token } = await seguridad.json();
    const respuesta = await fetch('/api/clientes', {
      method: 'POST', credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
      body: JSON.stringify(datos)
    });
    const cuerpo = await respuesta.json().catch(() => null);
    if (respuesta.status === 201 && cuerpo?.rol === 'Cliente') {
      formulario.reset();
      contrasena.type = 'password';
      formulario.hidden = true;
      document.getElementById('mensaje-exito').textContent = `${datos.nombre}, tu cuenta de cliente se registró correctamente.`;
      const exito = document.getElementById('exito');
      exito.hidden = false;
      exito.focus();
    } else {
      const mensaje = [400, 409, 429].includes(respuesta.status) && cuerpo?.title
        ? cuerpo.title
        : 'No pudimos completar el registro. Inténtalo nuevamente en unos momentos.';
      mostrarErrores(cuerpo?.errors, mensaje);
    }
  } catch {
    mostrarErrores({}, 'No pudimos confirmar el registro. Comprueba tu conexión. Si vuelves a intentarlo y el correo ya existe, la cuenta pudo haberse creado.');
  } finally {
    enviando = false;
    enviar.disabled = false;
    enviar.textContent = 'Crear mi cuenta ↗';
  }
});

document.getElementById('otro-registro').addEventListener('click', () => {
  limpiarErrores();
  document.getElementById('exito').hidden = true;
  formulario.hidden = false;
  const mostrar = document.getElementById('mostrar-contrasena');
  mostrar.textContent = 'Mostrar';
  mostrar.setAttribute('aria-pressed', 'false');
  formulario.elements.nombre.focus();
});
