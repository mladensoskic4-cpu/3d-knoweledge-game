// Registar mini igara i nagrade. Nova mini igra = novi modul + registerMinigame().
//
// Ugovor za mini igru:
//   id, name, intro, duration (s)
//   init?() -> Promise   (opciono; npr. učitavanje podataka, postavlja ready)
//   ready?  (false = igra se preskače dok nema podataka)
//   play(host, { rng }) -> Promise<{ score 0..1, label }>
//   simulateBot(skill, rng) -> { score 0..1, label }
import zastave from './flags.js';
import gradovi from './capitals.js';
import mjesta from './photos.js';
import { instrumenti, zivotinje } from './recognize.js';

const REGISTRY = [];
export function registerMinigame(m) { REGISTRY.push(m); }
[zastave, gradovi, instrumenti, zivotinje, mjesta].forEach(registerMinigame);

export async function initMinigames() {
  await Promise.all(REGISTRY.map(m => m.init?.().catch(() => {})));
}
const available = () => REGISTRY.filter(m => m.ready !== false);

let lastId = null;
// Naizmjenično, da se ista mini igra ne ponavlja dvaput zaredom.
export function pickMinigame(rng = Math.random) {
  const all = available();
  const pool = all.length > 1 ? all.filter(m => m.id !== lastId) : all;
  const m = pool[Math.floor(rng() * pool.length)];
  lastId = m.id;
  return m;
}

export function rewardFor(score) {
  const coins = Math.round(score * 30);
  const jump = score >= 0.9 ? 3 : score >= 0.6 ? 2 : score >= 0.3 ? 1 : 0;
  return { coins, jump };
}
