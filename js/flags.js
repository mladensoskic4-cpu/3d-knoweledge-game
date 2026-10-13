// Zastave i baza država.
// Zastave su prave zastave iz otvorenog projekta flag-icons (MIT licenca), unaprijed pretvorene u male slike
// i spakovane u data/flags.json, pa se učitaju jednom na startu i prikazuju odmah.
// Jedino istorijska zastava SFRJ je nacrtana ovdje, jer je nema u tom skupu.
import { RAW_COUNTRIES } from './countries.js';

function star(cx, cy, r, fill) {
  let d = '';
  for (let i = 0; i < 10; i++) {
    const a = -Math.PI / 2 + (i * Math.PI) / 5, rr = i % 2 ? r * 0.42 : r;
    d += `${i ? 'L' : 'M'}${(cx + Math.cos(a) * rr).toFixed(2)} ${(cy + Math.sin(a) * rr).toFixed(2)}`;
  }
  return `<path d="${d}Z" fill="${fill}"/>`;
}
const SFRJ_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 30 20" preserveAspectRatio="none"><rect width="30" height="6.7" fill="#003893"/><rect y="6.6" width="30" height="6.8" fill="#fff"/><rect y="13.3" width="30" height="6.7" fill="#de0000"/>${star(15, 10, 4.6, '#fcd116')}${star(15, 10, 3.9, '#de0000')}</svg>`;

let DATA = { sfrj: 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(SFRJ_SVG) };

export async function loadFlags() {
  try {
    const r = await fetch('data/flags.json');
    if (r.ok) DATA = { ...(await r.json()), sfrj: DATA.sfrj };
  } catch { /* bez zastava igra i dalje radi */ }
  // dekodiraj unaprijed, da se prikazuju bez kašnjenja
  Object.values(DATA).forEach(src => { const i = new Image(); i.src = src; i.decode?.().catch(() => {}); });
}

export const flagUrl = code => DATA[code] || DATA.sfrj;
export function flagHtml(code, cls = '') {
  return `<img class="${cls}" src="${flagUrl(code)}" alt="" draggable="false">`;
}

export const COUNTRIES = RAW_COUNTRIES.map(([code, name, capital, continent, cities]) => ({ code, name, capital, continent, cities }));
export const FLAG_COUNTRIES = COUNTRIES;
export const CAPITAL_COUNTRIES = COUNTRIES.filter(c => c.capital);
