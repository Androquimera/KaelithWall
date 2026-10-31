(() => {
  const root = document.documentElement;
  const toggle = document.querySelector('.language-toggle');
  const savedLanguage = localStorage.getItem('kwall-language') || localStorage.getItem('kaelithwall-language');
  localStorage.removeItem('kaelithwall-language');
  const preferredEnglish = navigator.language && !navigator.language.toLowerCase().startsWith('pt');

  function setLanguage(language) {
    const english = language === 'en';
    root.lang = english ? 'en' : 'pt-BR';
    document.title = english
      ? 'Kwall — your whole collection in motion'
      : 'Kwall — sua coleção inteira em movimento';
    toggle.setAttribute('aria-pressed', String(english));
    toggle.setAttribute('aria-label', english ? 'Mudar para português' : 'Switch to English');
    document.querySelector('nav').setAttribute('aria-label', english ? 'Main navigation' : 'Navegação principal');
    document.querySelector('.brand').setAttribute('aria-label', english ? 'Kwall — home' : 'Kwall — início');
    document.querySelector('.trust-line').setAttribute('aria-label', english ? 'Main features' : 'Características principais');
    document.querySelector('.visual-stage').setAttribute('aria-label', english
      ? 'Shuffle the wallpaper stack'
      : 'Embaralhar a pilha de wallpapers');
    document.querySelector('.story-visual').setAttribute('aria-label', english
      ? 'Shuffle this image round'
      : 'Embaralhar esta rodada de imagens');
    document.querySelector('.crop-demo').setAttribute('aria-label', english
      ? 'Interactive demo: drag the image or use the arrow keys'
      : 'Demonstração interativa: arraste a imagem ou use as setas');
    document.querySelector('.interactive-logo').setAttribute('aria-label', english
      ? 'Animate the Kwall logo'
      : 'Animar o logo do Kwall');
    document.querySelector('.logo-mark').setAttribute('alt', english ? 'Kwall logo' : 'Logo do Kwall');
    document.querySelectorAll('.release-label:not([data-remote="1"])').forEach((label) => {
      label.textContent = english ? 'v1.2.2 · portable EXE' : 'v1.2.2 · EXE portátil';
    });
    localStorage.setItem('kwall-language', english ? 'en' : 'pt');
  }

  setLanguage(savedLanguage || (preferredEnglish ? 'en' : 'pt'));
  toggle.addEventListener('click', () => setLanguage(root.lang === 'en' ? 'pt' : 'en'));

  const visual = document.querySelector('.visual-stage');
  const stageMap = { hero: '0', round: '1', frame: '2', trust: '3', download: '3' };
  const counterMap = { hero: '1 / 4', round: '4 / 4', frame: '✓', trust: 'LOCAL', download: 'v1.2.2' };

  function activateWithKeyboard(element, callback) {
    element.addEventListener('keydown', (event) => {
      if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        callback();
      }
    });
  }

  if (visual) {
    const counter = visual.querySelector('.counter-number');
    const cycleStage = () => {
      const next = (Number(visual.dataset.stage || 0) + 1) % 4;
      visual.dataset.stage = String(next);
      visual.setAttribute('aria-pressed', String(next !== 0));
      if (counter) counter.textContent = next === 3 ? 'LOCAL' : `${next + 1} / 4`;
    };
    visual.addEventListener('click', cycleStage);
    activateWithKeyboard(visual, cycleStage);
    visual.addEventListener('pointermove', (event) => {
      const box = visual.getBoundingClientRect();
      const x = ((event.clientX - box.left) / box.width - .5) * 7;
      const y = ((event.clientY - box.top) / box.height - .5) * -7;
      visual.style.setProperty('--tilt-x', `${x.toFixed(2)}deg`);
      visual.style.setProperty('--tilt-y', `${y.toFixed(2)}deg`);
    });
    visual.addEventListener('pointerleave', () => {
      visual.style.removeProperty('--tilt-x');
      visual.style.removeProperty('--tilt-y');
    });
  }

  const roundVisual = document.querySelector('.story-visual');
  if (roundVisual) {
    const shuffle = () => {
      roundVisual.classList.toggle('is-shuffled');
      roundVisual.setAttribute('aria-pressed', String(roundVisual.classList.contains('is-shuffled')));
      roundVisual.querySelectorAll('.mini-stack i').forEach((card, index) => {
        card.style.zIndex = String((index + (roundVisual.classList.contains('is-shuffled') ? 2 : 0)) % 4);
      });
    };
    roundVisual.addEventListener('click', shuffle);
    activateWithKeyboard(roundVisual, shuffle);
  }

  const cropDemo = document.querySelector('.crop-demo');
  if (cropDemo) {
    const image = cropDemo.querySelector('.crop-image');
    const value = cropDemo.querySelector(':scope > p');
    let x = -13;
    let y = -11;
    let drag = null;
    const renderCrop = () => {
      image.style.setProperty('--crop-x', `${x}%`);
      image.style.setProperty('--crop-y', `${y}%`);
      if (value) value.textContent = `${((x + 25) / 25).toFixed(2)} × ${((y + 25) / 25).toFixed(2)}`;
    };
    const move = (dx, dy) => {
      x = Math.max(-25, Math.min(0, x + dx));
      y = Math.max(-25, Math.min(0, y + dy));
      renderCrop();
    };
    cropDemo.addEventListener('pointerdown', (event) => {
      drag = { x: event.clientX, y: event.clientY, originX: x, originY: y };
      cropDemo.setPointerCapture(event.pointerId);
      cropDemo.classList.add('is-dragging');
    });
    cropDemo.addEventListener('pointermove', (event) => {
      if (!drag) return;
      const box = cropDemo.getBoundingClientRect();
      x = Math.max(-25, Math.min(0, drag.originX + (event.clientX - drag.x) / box.width * 100));
      y = Math.max(-25, Math.min(0, drag.originY + (event.clientY - drag.y) / box.height * 100));
      renderCrop();
    });
    const stopDrag = () => { drag = null; cropDemo.classList.remove('is-dragging'); };
    cropDemo.addEventListener('pointerup', stopDrag);
    cropDemo.addEventListener('pointercancel', stopDrag);
    cropDemo.addEventListener('keydown', (event) => {
      const steps = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
      if (!steps[event.key]) return;
      event.preventDefault();
      move(steps[event.key][0], steps[event.key][1]);
    });
    renderCrop();
  }

  const interactiveLogo = document.querySelector('.interactive-logo');
  if (interactiveLogo) {
    const animateLogo = () => {
      interactiveLogo.classList.remove('is-active');
      void interactiveLogo.offsetWidth;
      interactiveLogo.classList.add('is-active');
    };
    interactiveLogo.addEventListener('click', animateLogo);
    interactiveLogo.addEventListener('animationend', () => interactiveLogo.classList.remove('is-active'));
    activateWithKeyboard(interactiveLogo, animateLogo);
  }

  if ('IntersectionObserver' in window && visual) {
    const observer = new IntersectionObserver((entries) => {
      const visible = entries.filter((entry) => entry.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
      if (!visible) return;
      const name = visible.target.dataset.observe;
      visual.dataset.stage = stageMap[name] || '0';
      const counter = visual.querySelector('.counter-number');
      if (counter) counter.textContent = counterMap[name] || '1 / 4';
    }, { rootMargin: '-25% 0px -50%', threshold: [0, .2, .5] });
    document.querySelectorAll('[data-observe]').forEach((section) => observer.observe(section));
  }

  fetch('https://api.github.com/repos/Androquimera/Kwall/releases/latest', {
    headers: { Accept: 'application/vnd.github+json' }
  })
    .then((response) => response.ok ? response.json() : Promise.reject(new Error('release unavailable')))
    .then((release) => {
      const asset = release.assets.find((item) => item.name.toLowerCase().endsWith('.exe'));
      document.querySelectorAll('.release-label').forEach((label) => {
        const format = asset ? Math.round(asset.size / 1024) + ' KB' : (root.lang === 'en' ? 'portable EXE' : 'EXE portátil');
        label.textContent = `${release.tag_name} · ${format}`;
        label.dataset.remote = '1';
      });
      if (asset) document.querySelectorAll('.download-link').forEach((link) => { link.href = asset.browser_download_url; });
    })
    .catch(() => { /* O link de fallback continua apontando para releases/latest. */ });
})();
