// Staza: polja duž rute izabrane mape, oznake gradova, stanice za mini igre i figure igrača (lik + vozilo).
import { toWorld, LAND_TOP, tween, ease } from './scene.js';
import { buildToken } from './models.js';
import { L } from './i18n.js';

const THREE = window.THREE;

export const CHECKPOINT_EVERY = 7;
const Y = LAND_TOP + 0.08;

function label(text, opts = {}) {
  text = L(text);
  const pad = 18, fs = 44;
  const c = document.createElement('canvas');
  const ctx = c.getContext('2d');
  ctx.font = `700 ${fs}px Figtree, system-ui, sans-serif`;
  const w = Math.ceil(ctx.measureText(text).width) + pad * 2, h = fs + pad;
  c.width = w; c.height = h;
  ctx.font = `700 ${fs}px Figtree, system-ui, sans-serif`;
  ctx.fillStyle = opts.bg || 'rgba(255,255,255,0.92)';
  const r = h / 2;
  ctx.beginPath(); ctx.moveTo(r, 0); ctx.arcTo(w, 0, w, h, r); ctx.arcTo(w, h, 0, h, r); ctx.arcTo(0, h, 0, 0, r); ctx.arcTo(0, 0, w, 0, r); ctx.fill();
  ctx.fillStyle = opts.fg || '#14232e';
  ctx.textBaseline = 'middle';
  ctx.fillText(text, pad, h / 2 + 2);
  const tex = new THREE.CanvasTexture(c);
  tex.minFilter = THREE.LinearFilter;
  const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthWrite: false }));
  const k = opts.scale || 0.0095;
  s.scale.set(w * k, h * k, 1);
  return s;
}

function checker() {
  const c = document.createElement('canvas'); c.width = 128; c.height = 32;
  const ctx = c.getContext('2d');
  for (let x = 0; x < 16; x++) for (let y = 0; y < 4; y++) { ctx.fillStyle = (x + y) % 2 ? '#14232e' : '#ffffff'; ctx.fillRect(x * 8, y * 8, 8, 8); }
  const t = new THREE.CanvasTexture(c); t.magFilter = THREE.NearestFilter; return t;
}

export function createTrack(world, map, TILE_COUNT) {
  const ROUTE = map.route;
  const pts = ROUTE.map(([, lon, lat]) => toWorld(lon, lat, Y));
  const curve = new THREE.CatmullRomCurve3(pts, false, 'centripetal');
  const sp = curve.getSpacedPoints(TILE_COUNT - 1);
  const last = TILE_COUNT - 1;

  const tiles = sp.map((p, i) => {
    const a = sp[Math.max(0, i - 1)], b = sp[Math.min(last, i + 1)];
    const dir = b.clone().sub(a).setY(0).normalize();
    const type = i === 0 ? 'start' : i === last ? 'cilj' : i % CHECKPOINT_EVERY === 0 ? 'stanica' : 'polje';
    return { i, pos: p, dir, type, city: null };
  });
  // Najbliže polje svakom gradu dobija ime grada.
  ROUTE.forEach(([name, lon, lat]) => {
    const c = toWorld(lon, lat, Y);
    let best = 0, bd = Infinity;
    tiles.forEach((t, i) => { const d = t.pos.distanceToSquared(c); if (d < bd) { bd = d; best = i; } });
    if (!tiles[best].city) tiles[best].city = name;
  });
  tiles.forEach(t => {
    if (t.type !== 'stanica') return;
    let bd = Infinity;
    ROUTE.forEach(([name, lon, lat]) => { const d = t.pos.distanceToSquared(toWorld(lon, lat, Y)); if (d < bd) { bd = d; t.near = name; } });
  });

  const group = new THREE.Group();
  world.mapGroup.add(group);
  const unsub = [];

  // Traka staze.
  const tube = new THREE.Mesh(new THREE.TubeGeometry(curve, 300, 0.07, 5, false), new THREE.MeshStandardMaterial({ color: 0xf6f1df, flatShading: true }));
  tube.position.y = -0.02;
  group.add(tube);

  const tileGeo = new THREE.CylinderGeometry(0.42, 0.47, 0.14, 6);
  const mats = {
    polje: [new THREE.MeshStandardMaterial({ color: 0xffffff, flatShading: true }), new THREE.MeshStandardMaterial({ color: 0xd7ecf6, flatShading: true })],
    stanica: new THREE.MeshStandardMaterial({ color: 0xf2b630, flatShading: true, emissive: 0x4a3000, emissiveIntensity: 0.25 }),
    start: new THREE.MeshStandardMaterial({ color: 0x2f9e5b, flatShading: true }),
    cilj: new THREE.MeshStandardMaterial({ color: 0xc8102e, flatShading: true })
  };
  const spinners = [];
  tiles.forEach(t => {
    const mat = t.type === 'polje' ? mats.polje[t.i % 2] : mats[t.type];
    const m = new THREE.Mesh(tileGeo, mat);
    m.position.copy(t.pos);
    m.rotation.y = Math.atan2(t.dir.x, t.dir.z);
    group.add(m);
    if (t.type === 'stanica') {
      const gem = new THREE.Mesh(new THREE.OctahedronGeometry(0.2), new THREE.MeshStandardMaterial({ color: 0xffd34d, flatShading: true, emissive: 0x6b4a00, emissiveIntensity: 0.4 }));
      gem.position.copy(t.pos).setY(t.pos.y + 0.95);
      spinners.push(gem);
      group.add(gem);
      const l = label('Stanica', { bg: 'rgba(242,182,48,0.95)', scale: 0.0065 });
      l.position.copy(t.pos).setY(t.pos.y + 1.45);
      group.add(l);
    }
    if (t.city) {
      const l = label(t.city);
      const side = new THREE.Vector3(-t.dir.z, 0, t.dir.x).multiplyScalar(0.9);
      l.position.copy(t.pos).add(side).setY(t.pos.y + 0.75);
      group.add(l);
      // kućice
      for (let k = 0; k < 5; k++) {
        const ang = Math.random() * 6.28, r = 0.8 + Math.random() * 0.9;
        const hp = t.pos.clone().add(new THREE.Vector3(Math.cos(ang) * r, 0, Math.sin(ang) * r));
        const house = new THREE.Group();
        const wall = new THREE.Mesh(new THREE.BoxGeometry(0.22, 0.18, 0.2), new THREE.MeshStandardMaterial({ color: 0xf7f3ea, flatShading: true }));
        wall.position.y = 0.09;
        const roof = new THREE.Mesh(new THREE.ConeGeometry(0.19, 0.14, 4), new THREE.MeshStandardMaterial({ color: 0xc8553d, flatShading: true }));
        roof.position.y = 0.25; roof.rotation.y = Math.PI / 4;
        house.add(wall, roof);
        house.position.copy(hp).setY(LAND_TOP);
        house.rotation.y = Math.random() * 3;
        group.add(house);
      }
    }
  });

  // Ciljna kapija.
  const fin = tiles[last];
  const gate = new THREE.Group();
  const poleMat = new THREE.MeshStandardMaterial({ color: 0xf4f8fa, flatShading: true });
  [-0.7, 0.7].forEach(x => { const p = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.05, 1.3, 6), poleMat); p.position.set(x, 0.65, 0); gate.add(p); });
  const banner = new THREE.Mesh(new THREE.PlaneGeometry(1.5, 0.36), new THREE.MeshBasicMaterial({ map: checker(), side: THREE.DoubleSide }));
  banner.position.y = 1.2;
  gate.add(banner);
  gate.position.copy(fin.pos);
  gate.rotation.y = Math.atan2(fin.dir.x, fin.dir.z) + Math.PI / 2;
  group.add(gate);
  const finLabel = label(`CILJ · ${map.finish}`, { bg: 'rgba(200,16,46,0.95)', fg: '#fff' });
  finLabel.position.copy(fin.pos).setY(fin.pos.y + 1.85);
  group.add(finLabel);

  unsub.push(world.onFrame((dt, now) => spinners.forEach((s, i) => { s.rotation.y += dt * 1.6; s.position.y = Y + 0.95 + Math.sin(now / 400 + i) * 0.08; })));

  // --- žetoni ---
  const tokens = new Map();
  const markers = [];
  const flames = [];
  unsub.push(world.onFrame((dt, now) => {
    markers.forEach(m => { m.position.y = 1.0 + Math.sin(now / 250) * 0.08; m.rotation.y += dt * 2; });
    flames.forEach((f, i) => { f.scale.setScalar(0.8 + Math.abs(Math.sin(now / 70 + i)) * 0.5); });
  }));

  function slotOffset(player, players) {
    const same = players.filter(p => p.pos === player.pos);
    const idx = same.indexOf(player), n = same.length;
    const t = tiles[player.pos];
    const perp = new THREE.Vector3(-t.dir.z, 0, t.dir.x);
    return perp.multiplyScalar((idx - (n - 1) / 2) * 0.3);
  }
  const yaw = dir => Math.atan2(-dir.z, dir.x);

  return {
    tiles, last,
    isCheckpoint: i => tiles[i]?.type === 'stanica',
    addToken(player) {
      const car = buildToken(player.look);
      car.traverse(o => { if (o.userData.flame) flames.push(o); });
      if (player.human) {
        const mk = new THREE.Mesh(new THREE.ConeGeometry(0.12, 0.26, 4), new THREE.MeshStandardMaterial({ color: player.color, flatShading: true, emissive: player.color, emissiveIntensity: 0.3 }));
        mk.rotation.x = Math.PI; markers.push(mk); car.add(mk);
      }
      car.position.copy(tiles[0].pos);
      car.rotation.y = yaw(tiles[0].dir);
      group.add(car);
      tokens.set(player.id, car);
    },
    clearTokens() { tokens.forEach(c => group.remove(c)); tokens.clear(); markers.length = 0; flames.length = 0; },
    dispose() { unsub.forEach(f => f()); },
    tokenPosition(player) { return tokens.get(player.id).position; },
    async move(player, from, to) {
      const car = tokens.get(player.id);
      for (let i = from; i < to; i++) {
        const a = car.position.clone(), b = tiles[i + 1].pos;
        const dir = b.clone().sub(tiles[i].pos).setY(0).normalize();
        car.rotation.y = yaw(dir);
        await tween(240, k => {
          car.position.lerpVectors(a, b, ease(k));
          car.position.y = Y + Math.sin(Math.PI * k) * 0.4;
        });
      }
    },
    async relayout(players) {
      await Promise.all(players.map(p => {
        const car = tokens.get(p.id), a = car.position.clone();
        const b = tiles[p.pos].pos.clone().add(slotOffset(p, players));
        return tween(200, k => car.position.lerpVectors(a, b, k));
      }));
    }
  };
}
