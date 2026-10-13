// UI pomoćnici: kreiranje elemenata, paneli, HUD, toast poruke, formatiranje brojeva.

export const AREAS = {
  geografija: { label: 'Geografija', color: '#1f86c2' },
  istorija:   { label: 'Istorija', color: '#a8452f' },
  sport:      { label: 'Sport', color: '#2f9e5b' },
  muzika:     { label: 'Muzika i pop kultura', color: '#9b4bd6' },
  priroda:    { label: 'Priroda i životinje', color: '#5c8d2c' },
  hrana:      { label: 'Hrana i običaji', color: '#d9822b' },
  logika:     { label: 'Logika i matematika', color: '#3d5a80' },
  film:       { label: 'Filmovi i serije', color: '#c2410c' },
  nauka:      { label: 'Nauka i svemir', color: '#2a4d9b' },
  jezici:     { label: 'Jezici svijeta', color: '#0f8a8a' },
  tehnologija: { label: 'Tehnologija i gejming', color: '#6b3fa0' }
};

export function h(tag, attrs = {}, ...kids) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v == null || v === false) continue;
    if (k === 'class') el.className = v;
    else if (k === 'text') el.textContent = v;
    else if (k === 'html') el.innerHTML = v;
    else if (k === 'style' && typeof v === 'object') Object.entries(v).forEach(([sk, sv]) => (sk.startsWith('--') ? el.style.setProperty(sk, sv) : (el.style[sk] = sv)));
    else if (k.startsWith('on')) el.addEventListener(k.slice(2), v);
    else el.setAttribute(k, v === true ? '' : v);
  }
  kids.flat().forEach(c => { if (c != null && c !== false) el.append(c.nodeType ? c : document.createTextNode(String(c))); });
  return el;
}

const layer = () => document.getElementById('layer');
export function clearLayer() { layer().replaceChildren(); }
export function openCard(cls = '') {
  const card = h('section', { class: `card ${cls}`, role: 'dialog' });
  layer().replaceChildren(card);
  return card;
}

export function toast(html, color = '#14232e', ms = 2800) {
  const t = h('div', { class: 'toast', html });
  t.style.setProperty('--tc', color);
  document.getElementById('toasts').append(t);
  setTimeout(() => t.classList.add('out'), ms);
  setTimeout(() => t.remove(), ms + 400);
}

export function fmtNum(v, { decimals, group = true } = {}) {
  const d = decimals ?? (Number.isInteger(v) ? 0 : (String(v).split('.')[1] || '').length);
  const [i, f] = Math.abs(v).toFixed(d).split('.');
  const ig = group ? i.replace(/\B(?=(\d{3})+(?!\d))/g, '.') : i;
  return (v < 0 ? '−' : '') + ig + (f ? ',' + f : '');
}
export const fmtSec = s => fmtNum(Math.round(s * 10) / 10, { decimals: 1 }) + ' s';

export function waitButton(btn, key = 'Enter') {
  return new Promise(res => {
    const done = () => { window.removeEventListener('keydown', onKey); res(); };
    const onKey = e => { if (e.key === key) { e.preventDefault(); btn.click(); } };
    btn.addEventListener('click', done, { once: true });
    window.addEventListener('keydown', onKey);
    setTimeout(() => btn.focus({ preventScroll: true }), 50);
  });
}

export function renderHud(players, last, round) {
  const hud = document.getElementById('hud');
  hud.hidden = false;
  const human = players.find(p => p.human);
  const sorted = [...players].sort((a, b) => b.pos - a.pos);
  hud.replaceChildren(
    h('div', { class: 'hud-box' },
      h('div', { class: 'hud-top' },
        h('span', { class: 'hud-round' }, `Runda ${round}`),
        h('span', { class: 'coin-pill', title: 'Novčići' }, h('i', { class: 'coin' }), String(human.coins))
      ),
      h('ol', { class: 'standings' }, sorted.map(p =>
        h('li', { class: p.human ? 'me' : '' },
          h('i', { class: 'dot', style: { background: p.color } }),
          h('span', { class: 'st-name' }, p.name),
          h('span', { class: 'st-bar' }, h('b', { style: { width: `${(p.pos / last) * 100}%`, background: p.color } })),
          h('span', { class: 'st-left' }, p.pos >= last ? 'cilj' : `${last - p.pos}`)
        )
      ))
    )
  );
}
export function hideHud() { document.getElementById('hud').hidden = true; }
