// Low-poly modeli: lik (avatar) i vozila (auto, konj, kamion) sa dodacima. Svaki model gleda u pravcu +x.
import { flagUrl } from './flags.js';

const THREE = window.THREE;
const mat = (color, extra = {}) => new THREE.MeshStandardMaterial({ color, flatShading: true, roughness: 0.6, ...extra });
const box = (w, h, d, m, x = 0, y = 0, z = 0) => { const o = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), m); o.position.set(x, y, z); return o; };

export const DEFAULT_LOOK = {
  vozilo: 'auto', boja: '#e0312f',
  lik: { koza: '#e8b48f', majica: '#2b7de9', kapa: 'nema', naocare: false },
  dodaci: [], zastava: 'me'
};

function hat(type) {
  const g = new THREE.Group();
  if (type === 'kacket') {
    const top = new THREE.Mesh(new THREE.SphereGeometry(0.075, 8, 5, 0, Math.PI * 2, 0, Math.PI / 2), mat('#c8102e'));
    const brim = box(0.07, 0.012, 0.09, mat('#c8102e'), 0.06, 0, 0);
    g.add(top, brim);
  } else if (type === 'sesir') {
    g.add(new THREE.Mesh(new THREE.CylinderGeometry(0.12, 0.12, 0.012, 10), mat('#3b2a1a')));
    const c = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.068, 0.08, 10), mat('#3b2a1a')); c.position.y = 0.045; g.add(c);
  } else if (type === 'kapa') {
    // tradicionalna kapa: crni obod, crveni tepeluk sa zlatnim vezom
    const rim = new THREE.Mesh(new THREE.CylinderGeometry(0.072, 0.072, 0.045, 10), mat('#111111')); rim.position.y = 0.012;
    const top = new THREE.Mesh(new THREE.CylinderGeometry(0.066, 0.066, 0.008, 10), mat('#c8102e')); top.position.y = 0.038;
    const zlato = new THREE.Mesh(new THREE.CylinderGeometry(0.03, 0.03, 0.01, 8), mat('#d3ae3b')); zlato.position.y = 0.04;
    g.add(rim, top, zlato);
  } else if (type === 'kaciga') {
    const k = new THREE.Mesh(new THREE.SphereGeometry(0.085, 10, 6, 0, Math.PI * 2, 0, Math.PI / 1.8), mat('#f2b630', { roughness: 0.3 }));
    k.position.y = -0.01; g.add(k);
  } else if (type === 'kruna') {
    const ring = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 0.04, 8, 1, true), mat('#f2c230', { metalness: 0.6, roughness: 0.3, side: THREE.DoubleSide }));
    ring.position.y = 0.015; g.add(ring);
    for (let i = 0; i < 5; i++) {
      const a = (i / 5) * Math.PI * 2;
      const s = new THREE.Mesh(new THREE.ConeGeometry(0.014, 0.04, 4), mat('#f2c230', { metalness: 0.6, roughness: 0.3 }));
      s.position.set(Math.cos(a) * 0.065, 0.05, Math.sin(a) * 0.065); g.add(s);
    }
  }
  return g;
}

export function buildRider(lik = DEFAULT_LOOK.lik) {
  const g = new THREE.Group();
  const torso = box(0.12, 0.15, 0.13, mat(lik.majica), 0, 0.075, 0);
  const head = new THREE.Mesh(new THREE.IcosahedronGeometry(0.068, 1), mat(lik.koza));
  head.position.y = 0.21;
  const arms = box(0.05, 0.05, 0.2, mat(lik.majica), 0.04, 0.1, 0);
  g.add(torso, head, arms);
  if (lik.naocare) g.add(box(0.02, 0.022, 0.11, mat('#111111'), 0.06, 0.22, 0));
  if (lik.kapa && lik.kapa !== 'nema') { const ht = hat(lik.kapa); ht.position.y = 0.265; g.add(ht); }
  return g;
}

function wheels(g, xs, z, r = 0.085) {
  const geo = new THREE.CylinderGeometry(r, r, 0.07, 8); geo.rotateX(Math.PI / 2);
  const m = mat('#1b242b');
  xs.forEach(x => [z, -z].forEach(zz => { const w = new THREE.Mesh(geo, m); w.position.set(x, r, zz); g.add(w); }));
}

function flagPole(g, code, x, y) {
  const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.008, 0.008, 0.5, 5), mat('#dddddd'));
  pole.position.set(x, y + 0.25, 0);
  const c = document.createElement('canvas'); c.width = 128; c.height = 96;
  const tex = new THREE.CanvasTexture(c);
  const img = new Image();
  img.onload = () => { c.getContext('2d').drawImage(img, 0, 0, 128, 96); tex.needsUpdate = true; };
  img.src = flagUrl(code || 'me');
  const flag = new THREE.Mesh(new THREE.PlaneGeometry(0.2, 0.15), new THREE.MeshBasicMaterial({ map: tex, side: THREE.DoubleSide }));
  flag.position.set(x - 0.1, y + 0.42, 0);
  flag.rotation.y = Math.PI / 2 * 0; // zastava se vijori unazad
  g.add(pole, flag);
  return flag;
}

export function buildToken(look = DEFAULT_LOOK) {
  const l = { ...DEFAULT_LOOK, ...look, lik: { ...DEFAULT_LOOK.lik, ...(look.lik || {}) } };
  const g = new THREE.Group();
  const paint = mat(l.boja, { roughness: 0.45 });
  const has = a => (l.dodaci || []).includes(a);
  const rider = buildRider(l.lik);
  let backX = -0.3, topY = 0.3;

  if (l.vozilo === 'konj') {
    g.add(box(0.42, 0.17, 0.15, paint, 0, 0.38, 0));
    [[0.16, 0.06], [0.16, -0.06], [-0.16, 0.06], [-0.16, -0.06]].forEach(([x, z]) => g.add(box(0.05, 0.3, 0.05, paint, x, 0.15, z)));
    const neck = box(0.09, 0.2, 0.08, paint, 0.22, 0.5, 0); neck.rotation.z = -0.55; g.add(neck);
    const head = box(0.17, 0.075, 0.075, paint, 0.31, 0.6, 0); head.rotation.z = -0.35; g.add(head);
    g.add(box(0.03, 0.12, 0.05, mat('#2a1a10'), 0.2, 0.6, 0)); // griva
    const tail = box(0.05, 0.18, 0.04, mat('#2a1a10'), -0.23, 0.34, 0); tail.rotation.z = 0.5; g.add(tail);
    if (has('sedlo')) g.add(box(0.16, 0.035, 0.18, mat('#7a3b16'), -0.02, 0.48, 0));
    rider.position.set(-0.02, 0.48, 0);
    backX = -0.24; topY = 0.47;
  } else if (l.vozilo === 'kamion') {
    g.add(box(0.26, 0.18, 0.36, paint, 0.2, 0.19, 0));
    g.add(box(0.24, 0.13, 0.34, mat('#bfe3f2', { transparent: true, opacity: 0.35 }), 0.2, 0.345, 0));
    g.add(box(0.27, 0.03, 0.37, paint, 0.2, 0.425, 0));
    g.add(box(0.44, 0.3, 0.38, mat('#f4f8fa'), -0.17, 0.27, 0));
    g.add(box(0.44, 0.04, 0.385, paint, -0.17, 0.27, 0));
    wheels(g, [0.2, -0.1, -0.3], 0.19);
    if (has('sanduci')) [-0.28, -0.14, -0.04].forEach((x, i) => g.add(box(0.1, 0.1, 0.1, mat('#b5833f'), x, 0.47, (i - 1) * 0.08)));
    rider.position.set(0.19, 0.18, 0); rider.scale.setScalar(0.9);
    backX = -0.4; topY = 0.42;
  } else {
    g.add(box(0.62, 0.15, 0.36, paint, 0, 0.16, 0));
    g.add(box(0.2, 0.06, 0.34, paint, 0.2, 0.25, 0));
    const ws = box(0.02, 0.12, 0.32, mat('#bfe3f2', { transparent: true, opacity: 0.5 }), 0.08, 0.3, 0); ws.rotation.z = -0.3; g.add(ws);
    wheels(g, [0.19, -0.19], 0.19);
    if (has('spojler')) { g.add(box(0.06, 0.02, 0.38, mat('#14232e'), -0.3, 0.33, 0)); g.add(box(0.02, 0.08, 0.02, mat('#14232e'), -0.3, 0.28, 0.12)); g.add(box(0.02, 0.08, 0.02, mat('#14232e'), -0.3, 0.28, -0.12)); }
    rider.position.set(-0.07, 0.22, 0);
    backX = -0.31; topY = 0.3;
  }
  g.add(rider);
  if (has('zastavica')) flagPole(g, l.zastava, backX + 0.04, topY - 0.1);
  if (has('turbo')) {
    const fl = new THREE.Mesh(new THREE.ConeGeometry(0.05, 0.16, 6), mat('#ff8a1f', { emissive: '#ff5a00', emissiveIntensity: 0.8 }));
    fl.rotation.z = Math.PI / 2; fl.position.set(backX - 0.06, 0.14, 0);
    fl.userData.flame = true;
    g.add(fl);
  }
  return g;
}

export function randomBotLook(rng = Math.random, color) {
  const vozila = ['auto', 'auto', 'konj', 'kamion'];
  const kape = ['nema', 'kacket', 'sesir', 'kaciga'];
  const majice = ['#f2b630', '#16a36a', '#9b4bd6', '#14232e', '#ffffff'];
  const koze = ['#f1c7a5', '#e8b48f', '#c98e66', '#8d5a3b'];
  const pick = a => a[Math.floor(rng() * a.length)];
  return { vozilo: pick(vozila), boja: color, lik: { koza: pick(koze), majica: pick(majice), kapa: pick(kape), naocare: rng() < 0.3 }, dodaci: [] };
}
