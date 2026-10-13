// Geografski podaci: pojednostavljeni obrisi (lon, lat), projekcija i udaljenost.
// Obrisi su namjerno stilizovani (low-poly), ne kartografski precizni.

// focus = država koja je istaknuta na mapi, labels = natpisi okolo.
export const MAPS = {
  cg:     { minLon: 18.35, maxLon: 20.45, minLat: 41.78, maxLat: 43.62, full: 4,  zero: 55,  grid: 0.5, label: 'Crna Gora', focus: 'cg',  labels: 'cg' },
  srb:    { minLon: 18.7,  maxLon: 23.1,  minLat: 41.8,  maxLat: 46.3,  full: 8,  zero: 110, grid: 1,   label: 'Srbija',    focus: 'srb', labels: 'srb' },
  balkan: { minLon: 13.0,  maxLon: 29.7,  minLat: 38.5,  maxLat: 46.8,  full: 20, zero: 280, grid: 2,   label: 'Balkan',    focus: null,  labels: 'balkan' }
};

export const SRB_SHAPE = [
  [18.85,45.92],[19.3,46.1],[19.7,46.18],[20.25,46.13],[20.75,45.75],[21.0,45.45],[21.5,45.15],[21.4,44.78],
  [22.0,44.6],[22.45,44.72],[22.7,44.55],[22.68,44.22],[22.5,43.9],[22.55,43.5],[22.95,43.1],[22.75,42.85],
  [22.45,42.5],[22.36,42.31],[21.9,42.3],[21.6,42.25],[21.1,42.15],[20.6,41.88],[20.55,42.2],[20.25,42.35],
  [19.98,42.53],[20.1,42.7],[20.35,42.83],[20.35,42.9],[20.08,43.0],[19.95,43.1],[19.7,43.18],
  [19.53,43.32],[19.42,43.38],[19.22,43.53],[19.5,43.72],[19.45,44.05],[19.12,44.4],[19.2,44.85],[19.0,45.15],
  [19.0,45.35],[18.95,45.55]
];

export const SRB_LABELS = [
  ['Mađarska',20.6,46.24],['Rumunija',22.3,45.35],['Bugarska',22.85,43.65],['BiH',18.95,44.2],['Hrvatska',18.85,45.25],
  ['Crna Gora',19.55,42.95],['S. Makedonija',21.9,42.0],['Albanija',20.15,42.05]
];

// Balkansko poluostrvo, isječeno na granice mape (gornja i donja ivica su ravne).
export const BALKAN_LAND = [
  [13,46.8],[13,45.75],[13.53,45.8],[13.75,45.65],[13.6,45.45],[13.55,45.1],[13.85,44.83],[14.1,45.2],
  [14.45,45.32],[14.9,45.0],[15.2,44.45],[15.23,44.11],[15.9,43.73],[16.44,43.5],[17.0,43.3],[17.6,42.93],
  [18.1,42.64],[18.52,42.43],[18.58,42.39],[18.7,42.37],[18.84,42.28],[19.1,42.06],[19.21,41.92],[19.37,41.86],
  [19.55,41.6],[19.45,41.32],[19.45,40.9],[19.49,40.47],[19.4,40.3],[19.98,39.9],[20.0,39.7],[20.3,39.4],
  [20.75,38.95],[21.1,38.5],[23.0,38.5],[23.4,38.9],[22.95,39.36],[22.6,40.0],[22.95,40.62],[23.4,40.25],
  [23.9,40.35],[23.85,40.75],[24.41,40.94],[25.0,40.95],[25.87,40.85],[26.08,40.72],[26.2,40.6],[26.67,40.41],
  [27.5,40.97],[28.98,41.0],[29.1,41.2],[28.1,41.63],[27.98,41.97],[27.47,42.5],[27.73,42.66],[27.92,43.2],
  [28.47,43.36],[28.58,43.82],[28.65,44.17],[29.6,45.0],[29.7,45.2],[29.7,46.8]
];

export const ITALY = [
  [13,43.85],[13.5,43.62],[14.2,42.46],[15.0,42.0],[15.9,41.95],[16.2,41.75],[16.87,41.12],[17.94,40.64],
  [18.49,40.15],[18.36,39.8],[17.2,40.45],[16.6,40.0],[16.6,38.5],[13,38.5]
];

export const CG_SHAPE = [
  [18.46,42.48],[18.56,42.57],[18.55,42.68],[18.53,42.88],[18.58,43.02],[18.68,43.2],[18.84,43.33],[19.02,43.45],
  [19.22,43.53],[19.42,43.38],[19.53,43.32],[19.7,43.18],[19.95,43.1],[20.08,43.0],[20.35,42.9],[20.35,42.83],
  [20.1,42.7],[19.98,42.53],[19.75,42.48],[19.58,42.52],[19.4,42.38],[19.38,42.22],[19.32,42.1],[19.4,41.98],
  [19.37,41.86],[19.21,41.92],[19.1,42.06],[18.95,42.2],[18.84,42.28],[18.7,42.37],[18.58,42.39],[18.52,42.43]
];

export const KOTOR_BAY = [
  [18.53,42.435],[18.6,42.45],[18.66,42.47],[18.72,42.49],[18.775,42.47],[18.77,42.425],[18.72,42.41],[18.68,42.43],[18.6,42.425]
];

export const LAKES = {
  skadar: [[19.05,42.3],[19.15,42.36],[19.27,42.3],[19.38,42.22],[19.45,42.12],[19.4,42.06],[19.3,42.12],[19.2,42.18],[19.1,42.24]],
  ohrid:  [[20.68,41.15],[20.78,41.2],[20.84,41.1],[20.8,40.92],[20.7,40.95]],
  prespa: [[20.95,41.05],[21.05,41.0],[21.05,40.8],[20.95,40.82]],
  plav:   [[19.91,42.615],[19.94,42.62],[19.955,42.6],[19.93,42.585],[19.91,42.595]]
};

export const COUNTRY_LABELS = [
  ['Slovenija',14.8,46.15],['Hrvatska',15.9,45.45],['BiH',17.9,44.25],['Srbija',20.9,44.0],['Crna Gora',19.3,42.75],
  ['S. Makedonija',21.7,41.6],['Albanija',20.1,40.8],['Grčka',22.0,39.7],['Bugarska',25.2,42.7],
  ['Rumunija',24.6,45.8],['Italija',15.2,40.6],['Jadransko more',16.8,42.4]
];

export const CG_LABELS = [
  ['Hrvatska',18.42,42.58],['BiH',18.5,43.35],['Srbija',20.0,43.45],['Albanija',19.8,42.1],['Jadransko more',18.75,41.98]
];

const R = 6371;
export function haversine(lon1, lat1, lon2, lat2) {
  const toR = d => d * Math.PI / 180;
  const dLat = toR(lat2 - lat1), dLon = toR(lon2 - lon1);
  const a = Math.sin(dLat / 2) ** 2 + Math.cos(toR(lat1)) * Math.cos(toR(lat2)) * Math.sin(dLon / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(a));
}

// Ekvirektangularna projekcija sa korekcijom cos(lat), centrirana u pravougaonik w×h.
export function projector(b, w, h, pad = 12) {
  const cos = Math.cos(((b.minLat + b.maxLat) / 2) * Math.PI / 180);
  const spanX = (b.maxLon - b.minLon) * cos, spanY = b.maxLat - b.minLat;
  const s = Math.min((w - 2 * pad) / spanX, (h - 2 * pad) / spanY);
  const ox = (w - spanX * s) / 2, oy = (h - spanY * s) / 2;
  return {
    toXY: (lon, lat) => [ox + (lon - b.minLon) * cos * s, oy + (b.maxLat - lat) * s],
    toLonLat: (x, y) => [b.minLon + (x - ox) / (cos * s), b.maxLat - (y - oy) / s],
    aspect: spanY / spanX
  };
}

export function pointInPolygon(lon, lat, poly) {
  let inside = false;
  for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) {
    const [xi, yi] = poly[i], [xj, yj] = poly[j];
    if ((yi > lat) !== (yj > lat) && lon < ((xj - xi) * (lat - yi)) / (yj - yi) + xi) inside = !inside;
  }
  return inside;
}
