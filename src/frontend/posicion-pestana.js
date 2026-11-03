'use strict';

// Mantiene el espacio mientras se ocultan datos privados y se vuelve a validar la sesión.
// Solo conserva estado visual en memoria; no guarda formularios ni credenciales.
window.crearPosicionPestana = function () {
  let anterior = null;

  function guardar() {
    if (anterior) return;
    anterior = {
      x: window.scrollX,
      y: window.scrollY,
      minimo: document.body.style.minHeight,
      prioridad: document.body.style.getPropertyPriority('min-height'),
      interaccion: false,
      detalles: new Map(
        [...document.querySelectorAll('details[data-vista]')].map((d) => [d.dataset.vista, d.open]),
      ),
    };
    // hidden/replaceChildren reducen la altura y el navegador limita scrollY de inmediato.
    document.body.style.setProperty('min-height', document.documentElement.scrollHeight + 'px');
  }

  function restaurar(conservar = true) {
    if (!anterior || document.hidden) return;
    const estado = anterior;
    anterior = null;
    if (conservar && !estado.interaccion) {
      for (const d of document.querySelectorAll('details[data-vista]'))
        if (estado.detalles.has(d.dataset.vista)) d.open = estado.detalles.get(d.dataset.vista);
    }
    if (estado.minimo)
      document.body.style.setProperty('min-height', estado.minimo, estado.prioridad);
    else document.body.style.removeProperty('min-height');
    // Una acción del usuario durante una consulta lenta tiene prioridad sobre la posición antigua.
    if (conservar && !estado.interaccion)
      window.scrollTo({ left: estado.x, top: estado.y, behavior: 'instant' });
  }

  function intervino() {
    if (anterior && !document.hidden) anterior.interaccion = true;
  }
  for (const evento of ['wheel', 'touchstart', 'pointerdown', 'keydown'])
    window.addEventListener(evento, intervino, { passive: true });
  window.addEventListener('resize', intervino);
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) guardar();
  });
  window.addEventListener('pagehide', guardar);

  return { guardar, restaurar, get pendiente() { return anterior !== null; } };
};
