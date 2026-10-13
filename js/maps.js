// Mape za trku. Svaka sljedeća se otključava višim rangom: Crna Gora → Balkan → Evropa → svijet.
// Kopno za Evropu i svijet je iz Natural Earth (javno vlasništvo), pojednostavljeno u data/maps/.
import { BALKAN_LAND, ITALY, LAKES } from './geo.js';

export const MAPS_RACE = [
  {
    id: 'cg', name: 'Crna Gora', rank: 'bronza', finish: 'Žabljak',
    land: 'balkan',
    route: [
      ['Herceg Novi', 18.537, 42.453], ['Kotor', 18.771, 42.424], ['Cetinje', 18.914, 42.39], ['Budva', 18.84, 42.289],
      ['Bar', 19.1, 42.093], ['Ulcinj', 19.216, 41.929], ['Podgorica', 19.262, 42.441], ['Nikšić', 18.944, 42.773],
      ['Plužine', 18.84, 43.153], ['Žabljak', 19.123, 43.154]
    ]
  },
  {
    id: 'balkan', name: 'Balkan', rank: 'srebro', finish: 'Žabljak',
    land: 'balkan',
    route: [
      ['Ljubljana', 14.506, 46.056], ['Zagreb', 15.982, 45.815], ['Novi Sad', 19.845, 45.267], ['Beograd', 20.457, 44.787],
      ['Kragujevac', 20.911, 44.012], ['Zlatibor', 19.7, 43.73], ['Sarajevo', 18.413, 43.856], ['Mostar', 17.807, 43.343],
      ['Dubrovnik', 18.094, 42.65], ['Herceg Novi', 18.537, 42.453], ['Kotor', 18.771, 42.424], ['Budva', 18.84, 42.289],
      ['Bar', 19.1, 42.093], ['Ulcinj', 19.216, 41.929], ['Podgorica', 19.262, 42.441], ['Kolašin', 19.522, 42.823],
      ['Žabljak', 19.123, 43.154]
    ]
  },
  {
    id: 'evropa', name: 'Evropa', rank: 'zlato', finish: 'Podgorica',
    land: 'data/maps/evropa.json',
    route: [
      ['Lisabon', -9.139, 38.722], ['Madrid', -3.704, 40.417], ['Pariz', 2.352, 48.857], ['London', -0.128, 51.507],
      ['Amsterdam', 4.904, 52.368], ['Berlin', 13.405, 52.52], ['Prag', 14.438, 50.075], ['Beč', 16.373, 48.208],
      ['Budimpešta', 19.04, 47.498], ['Beograd', 20.457, 44.787], ['Podgorica', 19.262, 42.441]
    ]
  },
  {
    id: 'svijet', name: 'Svijet', rank: 'platina', finish: 'Sidnej',
    land: 'data/maps/svijet.json',
    route: [
      ['Njujork', -74.006, 40.713], ['Rio de Žaneiro', -43.173, -22.907], ['Kejptaun', 18.424, -33.925], ['Najrobi', 36.822, -1.292],
      ['Kairo', 31.236, 30.044], ['Podgorica', 19.262, 42.441], ['Moskva', 37.618, 55.756], ['Nju Delhi', 77.209, 28.614],
      ['Peking', 116.407, 39.904], ['Tokio', 139.692, 35.69], ['Sidnej', 151.209, -33.869]
    ]
  }
];

export const TRACK_LENGTHS = {
  kratka: { label: 'Kratka', tiles: 30, note: 'oko 3–5 min' },
  srednja: { label: 'Srednja', tiles: 44, note: 'oko 6–8 min' },
  duga: { label: 'Duga', tiles: 58, note: 'oko 10 min' }
};

const cache = {};
// Vraća poligone kopna kao [[spoljni prsten, ...rupe], ...] i jezera.
export async function loadLand(map) {
  if (map.land === 'balkan') {
    return { polygons: [[BALKAN_LAND], [ITALY]], lakes: Object.values(LAKES) };
  }
  if (!cache[map.land]) {
    const r = await fetch(map.land);
    if (!r.ok) throw new Error('Mapa se nije učitala');
    cache[map.land] = await r.json();
  }
  return { polygons: cache[map.land].polygons, lakes: [] };
}
