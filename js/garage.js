// Garaža: izgled lika i vozila, kupovina opreme za novčiće, uz 3D pregled.
import { h, openCard } from './ui.js';
import { buildToken } from './models.js';
import { SHOP, POWERUPS, buyPowerup, profile, owns, buy, equip, isEquipped, applyItem, rankAllows, RANKS, save } from './profile.js';
import { COUNTRIES } from './flags.js';

const THREE = window.THREE;
const TABS = [
  ['vozilo', 'Vozilo', ['vozilo']], ['boja', 'Boja', ['boja']], ['lik', 'Lik', ['koza', 'majica', 'naocare']],
  ['kapa', 'Kapa', ['kapa']], ['dodaci', 'Dodaci', ['dodatak']], ['power', 'Power-up', []]
];

function preview(canvas) {
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  const scene = new THREE.Scene();
  const cam = new THREE.PerspectiveCamera(32, 1, 0.1, 50);
  cam.position.set(1.4, 1.0, 1.6); cam.lookAt(0, 0.3, 0);
  scene.add(new THREE.HemisphereLight(0xffffff, 0x88aa77, 1.0));
  const sun = new THREE.DirectionalLight(0xffffff, 0.7); sun.position.set(2, 3, 2); scene.add(sun);
  const disc = new THREE.Mesh(new THREE.CylinderGeometry(0.62, 0.66, 0.06, 24), new THREE.MeshStandardMaterial({ color: 0xd7ecf6, flatShading: true }));
  disc.position.y = -0.03; scene.add(disc);
  let model = null, alive = true, rot = 0;
  function set(look) { if (model) scene.remove(model); model = buildToken(look); scene.add(model); }
  function size() { const w = canvas.clientWidth || 300, hh = canvas.clientHeight || 200; renderer.setSize(w, hh, false); cam.aspect = w / hh; cam.updateProjectionMatrix(); }
  size();
  const loop = () => { if (!alive) return; rot += 0.012; if (model) model.rotation.y = rot; renderer.render(scene, cam); requestAnimationFrame(loop); };
  loop();
  return { set, size, dispose() { alive = false; renderer.dispose(); renderer.forceContextLoss?.(); } };
}

export function openGarage(onClose) {
  const P = profile();
  let tab = 'vozilo', pending = null; // predmet koji se isprobava, a nije kupljen
  const card = openCard('garage');
  const coins = h('span', { class: 'coin-pill big' }, h('i', { class: 'coin' }), h('b', { translate: 'no' }, String(P.novcici)));
  const canvas = h('canvas', { class: 'garage-cv', 'aria-label': 'Pregled lika i vozila' });
  const tabs = h('div', { class: 'seg tabs', role: 'tablist' });
  const grid = h('div', { class: 'shop-grid' });
  const buyBar = h('div', { class: 'buy-bar' });
  const flagPick = h('select', { id: 'zastava-drzava', class: 'flag-select', 'aria-label': 'Država na zastavici' },
    COUNTRIES.map(c => h('option', { value: c.code, selected: P.izgled.zastava === c.code }, c.name)));
  flagPick.addEventListener('change', () => { P.izgled.zastava = flagPick.value; save(); pv.set(currentLook()); });
  const back = h('button', { class: 'btn btn-primary', type: 'button', id: 'garage-back' }, 'Nazad');

  card.append(
    h('header', { class: 'garage-head' }, h('h2', { class: 'q-text' }, 'Garaža'), coins),
    h('div', { class: 'garage-body' }, h('div', { class: 'garage-stage' }, canvas), h('div', { class: 'garage-shop' }, tabs, grid, buyBar)),
    h('div', { class: 'row-bar end' }, back)
  );
  const pv = preview(canvas);
  const currentLook = () => (pending ? applyItem(P.izgled, pending) : P.izgled);
  pv.set(currentLook());
  window.addEventListener('resize', pv.size);

  function renderTabs() {
    tabs.replaceChildren(...TABS.map(([id, label]) => {
      const b = h('button', { type: 'button', role: 'tab', id: `tab-${id}`, 'aria-selected': String(tab === id) }, label);
      b.addEventListener('click', () => { tab = id; pending = null; render(); });
      return b;
    }));
  }
  function render() {
    renderTabs();
    coins.querySelector('b').textContent = String(P.novcici);
    if (tab === 'power') {
      buyBar.replaceChildren();
      grid.replaceChildren(...POWERUPS.map(pu => {
        const b = h('div', { class: 'shop-item pu-item' },
          h('b', {}, pu.label), h('small', { class: 'pu-desc' }, pu.opis),
          h('small', {}, `Imaš: ${P.powerupi[pu.id] || 0}`),
          h('button', { type: 'button', class: 'btn btn-primary btn-sm', id: `pu-buy-${pu.id}`, disabled: P.novcici < pu.price, onclick: () => { if (buyPowerup(pu.id)) render(); } },
            h('i', { class: 'coin' }), ` ${pu.price}`));
        return b;
      }));
      return;
    }
    const slots = TABS.find(t => t[0] === tab)[2];
    const items = SHOP.filter(i => slots.includes(i.slot) && (!i.za || i.za === P.izgled.vozilo));
    grid.replaceChildren(...items.map(item => {
      const own = owns(item.id), eq = isEquipped(item), allowed = rankAllows(item);
      const state = eq ? 'Opremljeno' : own ? 'Izaberi' : !allowed ? `Rang: ${RANKS.find(r => r.id === item.rank).label}` : `${item.price}`;
      const b = h('button', { type: 'button', class: `shop-item${eq ? ' eq' : ''}${pending === item ? ' try' : ''}${!allowed && !own ? ' locked' : ''}`, id: `shop-${item.id}` },
        item.swatch ? h('i', { class: 'swatch', style: { background: item.swatch } }) : null,
        h('b', {}, item.label),
        h('small', {}, own || !allowed ? state : [h('i', { class: 'coin' }), ' ', state]));
      b.addEventListener('click', () => {
        if (own) { equip(item); pending = null; }
        else pending = item;
        pv.set(currentLook()); render();
      });
      return b;
    }));
    buyBar.replaceChildren();
    if (tab === 'dodaci' && P.izgled.dodaci?.includes('zastavica')) buyBar.append(h('label', { for: 'zastava-drzava', class: 'map-hint' }, 'Zastavica:'), flagPick);
    if (pending) {
      const can = rankAllows(pending) && P.novcici >= pending.price;
      const btn = h('button', { type: 'button', class: 'btn btn-primary', id: 'shop-buy', disabled: !can }, `Kupi (${pending.price})`);
      btn.addEventListener('click', () => { if (buy(pending)) { equip(pending); pending = null; pv.set(currentLook()); render(); } });
      buyBar.append(h('span', { class: 'map-hint' }, !rankAllows(pending) ? `Otključava se na rangu ${RANKS.find(r => r.id === pending.rank).label}` : can ? `Isprobavaš: ${pending.label}` : 'Nemaš dovoljno novčića.'), btn);
    }
  }
  render();
  back.addEventListener('click', () => { window.removeEventListener('resize', pv.size); pv.dispose(); onClose?.(); });
}
