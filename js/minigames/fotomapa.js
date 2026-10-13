// Mini igra: Gdje je ova fotografija? Prava fotografija poznatog mjesta, a ti tapneš na mapi svijeta gdje je.
// Bodovi zavise od udaljenosti: do 100 km = pun bod, pa sve manje do 2.100 km.
// Podaci: mjesta/podaci.json (naziv, drzava, lat, lon, slika, autor, licenca…), skinuto skriptom.
import { h } from '../ui.js';
import { clamp, gaussian, shuffle, sleep } from '../util.js';
import { loadWorld } from '../world.js';
import { haversine } from '../geo.js';
import { timerBar } from './kit.js';
import { createWorldMap } from './mapwidget.js';
import { loadSlike, photoFrame, preload, imeStavke } from '../slike.js';
import { correct as sfxOk, wrong as sfxBad } from '../sfx.js';

const GOAL = 5;
export const scoreForKm = km => (km <= 100 ? 1 : clamp(1 - (km - 100) / 2000, 0, 1));

const game = {
  id: 'foto-mapa', name: 'Gdje je ova fotografija?', duration: 40, ready: false, w: null, list: [],
  intro: 'Vidiš fotografiju poznatog mjesta. Tapni na mapi svijeta gdje se nalazi. Što bliže, to više bodova (do 100 km je pun bod). Imaš 40 sekundi.',
  async init() {
    const [w, s] = await Promise.all([loadWorld(), loadSlike()]);
    this.w = w;
    this.list = s.mjesta.filter(x => typeof x.lat === 'number' && typeof x.lon === 'number');
    this.ready = this.list.length >= 6;
  },
  play(host, { rng = Math.random } = {}) {
    const dur = this.duration * 1000, list = this.list;
    const card = host.closest('.card');
    card?.classList.add('wide-map');
    return new Promise(resolve => {
      const { fill, status } = timerBar(host);
      const photo = h('div', { class: 'fm-photo' });
      const note = h('p', { class: 'map-hint wm-note' }, 'Tapni na mapu gdje je ovo mjesto.');
      let bag = shuffle([...list], rng), cur = null, score = 0, done = false, busy = false;
      const map = createWorldMap(this.w, { onTap: answer, heightFactor: 0.36 });
      host.append(h('div', { class: 'fm-layout' }, photo, map.el), note);

      async function next() {
        for (let k = 0; k < 5; k++) {
          if (!bag.length) bag = shuffle([...list], rng);
          const it = bag.pop();
          if (await preload(it.slika)) { cur = it; break; }
        }
        if (done || !cur) return;
        photo.replaceChildren(photoFrame(cur, { alt: 'Fotografija mjesta koje treba pronaći na mapi', small: true }));
        map.setOverlay(null);
        note.textContent = 'Tapni na mapu gdje je ovo mjesto.';
      }
      async function answer(lon, lat) {
        if (done || busy || !cur) return;
        busy = true;
        const it = cur, km = haversine(lon, lat, it.lon, it.lat), acc = scoreForKm(km);
        score += acc;
        (acc >= 0.8 ? sfxOk : sfxBad)();
        map.setOverlay(({ ctx, X, Y, k }) => {
          ctx.strokeStyle = 'rgba(20,35,46,.8)'; ctx.setLineDash([6 / k, 5 / k]); ctx.lineWidth = 2 / k;
          ctx.beginPath(); ctx.moveTo(X(lon), Y(lat)); ctx.lineTo(X(it.lon), Y(it.lat)); ctx.stroke(); ctx.setLineDash([]);
          ctx.fillStyle = '#1d8a4e'; ctx.strokeStyle = '#fff'; ctx.lineWidth = 2 / k;
          ctx.beginPath(); ctx.arc(X(it.lon), Y(it.lat), 7 / k, 0, 7); ctx.fill(); ctx.stroke();
          ctx.fillStyle = '#c8102e';
          ctx.beginPath(); ctx.arc(X(lon), Y(lat), 5 / k, 0, 7); ctx.fill(); ctx.stroke();
        });
        map.focus([[lon, lat], [it.lon, it.lat]]);
        note.replaceChildren(h('b', {}, `${imeStavke(it)}${it.drzava ? ', ' + it.drzava : ''}`), ` · ${Math.round(km).toLocaleString('sr-Latn')} km · +${acc.toFixed(1)}`);
        await sleep(1700);
        busy = false;
        if (!done) next();
      }

      map.start(); next();
      const t0 = performance.now();
      const tick = now => {
        const left = Math.max(0, dur - (now - t0));
        fill.style.transform = `scaleX(${left / dur})`;
        status.textContent = `${score.toFixed(1)} bodova · ${Math.ceil(left / 1000)} s`;
        if (left <= 0) {
          done = true; map.destroy(); card?.classList.remove('wide-map');
          return resolve({ score: clamp(score / GOAL, 0, 1), label: `${score.toFixed(1)} bodova` });
        }
        requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  },
  simulateBot(skill, rng = Math.random) {
    const score = clamp(skill * 0.9 + gaussian(rng) * 0.15, 0, 1);
    return { score, label: `${(score * GOAL).toFixed(1)} bodova` };
  }
};
export default game;
