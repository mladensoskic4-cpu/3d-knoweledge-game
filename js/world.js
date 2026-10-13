// Svjetski podaci za mini igre: države (oblik, površina, centar), kopno, veliki gradovi i rijeke.
// Države: Natural Earth (javno vlasništvo), Kosovo je u sastavu Srbije.
import { haversine, pointInPolygon } from './geo.js';

let WORLD = null;
export async function loadWorld() {
  if (WORLD) return WORLD;
  const [drzave, kopno] = await Promise.all([
    fetch('data/maps/drzave.json').then(r => r.json()),
    fetch('data/maps/kopno-svijet.json').then(r => r.json())
  ]);
  WORLD = { drzave, kopno };
  return WORLD;
}

export function inCountry(lon, lat, d) {
  return d.poly.some(rings => pointInPolygon(lon, lat, rings[0]) && !rings.slice(1).some(h => pointInPolygon(lon, lat, h)));
}
// Udaljenost (km) od tačke do države: 0 ako je unutra, inače do najbliže tačke obrisa.
export function distToCountry(lon, lat, d) {
  if (inCountry(lon, lat, d)) return 0;
  let best = Infinity;
  d.poly.forEach(rings => rings[0].forEach(([x, y], i) => { if (i % 2 === 0) best = Math.min(best, haversine(lon, lat, x, y)); }));
  return best;
}
export function distToLine(lon, lat, pts) {
  let best = Infinity;
  for (let i = 0; i < pts.length - 1; i++) {
    const [a, b] = [pts[i], pts[i + 1]];
    for (let k = 0; k <= 10; k++) {
      const t = k / 10;
      best = Math.min(best, haversine(lon, lat, a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t));
    }
  }
  return best;
}

// Veliki gradovi svijeta [naziv, lon, lat, engleski naziv]
export const CITIES = [
  ['Njujork', -74.01, 40.71, 'New York'], ['Los Anđeles', -118.24, 34.05, 'Los Angeles'], ['Čikago', -87.63, 41.88, 'Chicago'],
  ['Meksiko Siti', -99.13, 19.43, 'Mexico City'], ['Toronto', -79.38, 43.65, 'Toronto'], ['Vankuver', -123.12, 49.28, 'Vancouver'],
  ['Havana', -82.37, 23.11, 'Havana'], ['Bogota', -74.07, 4.71, 'Bogotá'], ['Lima', -77.04, -12.05, 'Lima'],
  ['Rio de Žaneiro', -43.17, -22.91, 'Rio de Janeiro'], ['Sao Paolo', -46.63, -23.55, 'São Paulo'], ['Buenos Ajres', -58.38, -34.6, 'Buenos Aires'],
  ['Santijago', -70.67, -33.45, 'Santiago'], ['London', -0.13, 51.51, 'London'], ['Pariz', 2.35, 48.86, 'Paris'],
  ['Madrid', -3.7, 40.42, 'Madrid'], ['Lisabon', -9.14, 38.72, 'Lisbon'], ['Rim', 12.5, 41.9, 'Rome'], ['Berlin', 13.4, 52.52, 'Berlin'],
  ['Beč', 16.37, 48.21, 'Vienna'], ['Beograd', 20.46, 44.79, 'Belgrade'], ['Podgorica', 19.26, 42.44, 'Podgorica'],
  ['Atina', 23.73, 37.98, 'Athens'], ['Istanbul', 28.98, 41.01, 'Istanbul'], ['Moskva', 37.62, 55.76, 'Moscow'],
  ['Stokholm', 18.07, 59.33, 'Stockholm'], ['Rejkjavik', -21.94, 64.15, 'Reykjavík'], ['Kairo', 31.24, 30.04, 'Cairo'],
  ['Lagos', 3.38, 6.52, 'Lagos'], ['Najrobi', 36.82, -1.29, 'Nairobi'], ['Kejptaun', 18.42, -33.92, 'Cape Town'],
  ['Kazablanka', -7.59, 33.57, 'Casablanca'], ['Dubai', 55.27, 25.2, 'Dubai'], ['Teheran', 51.39, 35.69, 'Tehran'],
  ['Mumbaj', 72.88, 19.08, 'Mumbai'], ['Nju Delhi', 77.21, 28.61, 'New Delhi'], ['Peking', 116.41, 39.9, 'Beijing'],
  ['Šangaj', 121.47, 31.23, 'Shanghai'], ['Hong Kong', 114.17, 22.32, 'Hong Kong'], ['Tokio', 139.69, 35.69, 'Tokyo'],
  ['Seul', 126.98, 37.57, 'Seoul'], ['Bangkok', 100.5, 13.76, 'Bangkok'], ['Singapur', 103.82, 1.35, 'Singapore'],
  ['Džakarta', 106.85, -6.21, 'Jakarta'], ['Manila', 120.98, 14.6, 'Manila'], ['Sidnej', 151.21, -33.87, 'Sydney'],
  ['Melburn', 144.96, -37.81, 'Melbourne'], ['Okland', 174.76, -36.85, 'Auckland'], ['Honolulu', -157.86, 21.31, 'Honolulu'],
  ['Anchorage', -149.9, 61.22, 'Anchorage']
].map(([name, lon, lat, en]) => ({ name, lon, lat, en }));

// Velike rijeke: pojednostavljen tok (izvor → ušće), dovoljno tačan za igru.
export const RIVERS = [
  { name: 'Nil', en: 'Nile', pts: [[33.0, -2.3], [32.9, 0.4], [32.5, 3.9], [31.6, 9.5], [32.5, 15.6], [33.0, 19.3], [31.4, 21.9], [32.9, 24.1], [31.2, 30.0], [31.0, 31.4]] },
  { name: 'Amazon', en: 'Amazon', pts: [[-73.5, -4.4], [-69.9, -4.2], [-65.0, -3.3], [-60.0, -3.1], [-55.0, -2.2], [-51.0, -1.0], [-49.5, -0.2]] },
  { name: 'Misisipi', en: 'Mississippi', pts: [[-95.2, 47.2], [-93.1, 44.9], [-91.2, 43.0], [-90.2, 38.6], [-89.2, 37.0], [-90.1, 35.1], [-91.1, 32.3], [-90.1, 29.95], [-89.4, 29.2]] },
  { name: 'Dunav', en: 'Danube', pts: [[8.5, 47.95], [11.0, 48.7], [14.3, 48.3], [16.4, 48.2], [17.1, 48.1], [19.0, 47.8], [18.9, 45.9], [20.5, 44.8], [22.5, 44.5], [25.0, 43.7], [28.0, 44.0], [28.2, 45.4], [29.7, 45.2]] },
  { name: 'Rajna', en: 'Rhine', pts: [[9.6, 46.6], [9.5, 47.5], [7.6, 47.6], [7.8, 48.6], [8.3, 49.9], [7.6, 50.4], [6.96, 50.94], [6.6, 51.8], [4.1, 51.9]] },
  { name: 'Volga', en: 'Volga', pts: [[33.0, 57.3], [35.9, 56.9], [39.9, 57.6], [44.0, 56.3], [49.1, 55.8], [48.4, 54.3], [46.0, 51.5], [44.5, 48.7], [48.0, 46.3]] },
  { name: 'Jangce', en: 'Yangtze', pts: [[91.0, 33.4], [97.0, 32.0], [100.2, 27.0], [104.6, 28.8], [106.5, 29.6], [111.3, 30.7], [114.3, 30.6], [118.8, 32.0], [121.8, 31.4]] },
  { name: 'Žuta rijeka', en: 'Yellow River', pts: [[96.0, 34.8], [103.8, 36.1], [106.5, 39.0], [109.9, 40.6], [111.0, 39.0], [110.3, 34.6], [113.7, 34.9], [117.0, 36.7], [119.2, 37.7]] },
  { name: 'Gang', en: 'Ganges', pts: [[79.0, 30.9], [78.2, 29.9], [80.3, 26.4], [82.9, 25.3], [85.1, 25.6], [87.9, 25.0], [88.6, 23.0], [89.9, 22.0]] },
  { name: 'Kongo', en: 'Congo', pts: [[26.5, -8.5], [25.9, -5.0], [25.2, 0.5], [21.5, 2.1], [18.3, 0.0], [16.0, -2.5], [15.3, -4.3], [12.3, -6.0]] },
  { name: 'Niger', en: 'Niger', pts: [[-10.5, 9.1], [-8.0, 12.6], [-4.2, 14.5], [-3.0, 16.8], [0.0, 16.3], [2.1, 13.5], [4.2, 11.5], [6.7, 7.8], [6.0, 4.5]] },
  { name: 'Mekong', en: 'Mekong', pts: [[94.7, 33.6], [98.7, 29.0], [100.1, 22.0], [101.8, 20.0], [102.6, 17.97], [105.8, 15.1], [105.9, 12.4], [106.6, 9.9]] },
  { name: 'Ob', en: 'Ob', pts: [[85.0, 52.5], [83.0, 55.0], [82.9, 56.4], [76.0, 60.9], [69.0, 61.0], [66.6, 66.5], [72.0, 67.0]] },
  { name: 'Murej', en: 'Murray', pts: [[148.0, -36.6], [146.9, -36.1], [144.0, -35.7], [141.5, -34.2], [139.6, -34.2], [139.3, -35.5]] },
  { name: 'Temza', en: 'Thames', pts: [[-2.0, 51.7], [-1.26, 51.75], [-0.98, 51.46], [-0.13, 51.5], [0.6, 51.5]] },
  { name: 'Sena', en: 'Seine', pts: [[4.7, 47.5], [4.1, 48.3], [2.35, 48.86], [1.1, 49.4], [0.1, 49.45]] },
  { name: 'Morača', en: 'Morača', pts: [[19.15, 42.98], [19.3, 42.76], [19.26, 42.44], [19.25, 42.27]] }
];
