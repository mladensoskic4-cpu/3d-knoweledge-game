// Scena: renderer, kamera, svjetla, more, low-poly kopno, planine, oblaci i tweening.
// Projekcija se postavlja po mapi (Crna Gora, Balkan, Evropa, svijet), tako da je razmak polja uvijek isti.
import { pointInPolygon } from './geo.js';
import { reducedMotion } from './util.js';

const THREE = window.THREE;
export const LAND_TOP = 0.4;
export const TILE_SPACING = 1.45;

const proj = { lon0: 19, lat0: 43, K: 8, cos: Math.cos(43 * Math.PI / 180) };
export function toWorld(lon, lat, y = 0) {
  return new THREE.Vector3((lon - proj.lon0) * proj.K * proj.cos, y, -(lat - proj.lat0) * proj.K);
}
export function fromWorld(x, z) {
  return [proj.lon0 + x / (proj.K * proj.cos), proj.lat0 - z / proj.K];
}
// Projekcija tako da ruta sa n polja ima razmak TILE_SPACING.
export function setProjectionForRoute(route, tiles) {
  const lons = route.map(r => r[1]), lats = route.map(r => r[2]);
  proj.lon0 = (Math.min(...lons) + Math.max(...lons)) / 2;
  proj.lat0 = (Math.min(...lats) + Math.max(...lats)) / 2;
  proj.cos = Math.cos(proj.lat0 * Math.PI / 180);
  let len = 0;
  for (let i = 1; i < route.length; i++) len += Math.hypot((route[i][1] - route[i - 1][1]) * proj.cos, route[i][2] - route[i - 1][2]);
  proj.K = ((tiles - 1) * TILE_SPACING * 0.93) / len;
}

function shapeFrom(rings) {
  const toShapePts = ring => ring.map(([lon, lat]) => { const p = toWorld(lon, lat); return new THREE.Vector2(p.x, -p.z); });
  const s = new THREE.Shape(toShapePts(rings[0]));
  rings.slice(1).forEach(h => s.holes.push(new THREE.Path(toShapePts(h))));
  return s;
}

// --- jednostavan tween sistem, pokreće ga petlja renderovanja ---
const tasks = [];
export function tween(duration, fn) {
  const dur = reducedMotion() ? Math.min(duration, 60) : duration;
  return new Promise(resolve => {
    const t0 = performance.now();
    tasks.push(now => {
      const k = Math.min(1, (now - t0) / dur);
      fn(k);
      if (k >= 1) { resolve(); return true; }
      return false;
    });
  });
}
export const ease = k => (k < 0.5 ? 2 * k * k : 1 - (-2 * k + 2) ** 2 / 2);

function disposeTree(obj) {
  obj.traverse(o => {
    o.geometry?.dispose?.();
    const m = o.material;
    (Array.isArray(m) ? m : m ? [m] : []).forEach(x => { x.map?.dispose?.(); x.dispose?.(); });
  });
}

export function createWorld(container) {
  const renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  container.appendChild(renderer.domElement);

  const scene = new THREE.Scene();
  const sky = new THREE.Color(0x9ed6ef);
  scene.background = sky;
  scene.fog = new THREE.Fog(sky, 38, 130);

  const camera = new THREE.PerspectiveCamera(46, 1, 0.1, 500);
  camera.position.set(0, 60, 50);

  scene.add(new THREE.HemisphereLight(0xe8f6ff, 0x4f6f3e, 0.9));
  const sun = new THREE.DirectionalLight(0xfff0d2, 0.85);
  sun.position.set(-25, 45, 20);
  scene.add(sun);

  let mapGroup = null, seaGeo = null, seaBase = null;
  const center = new THREE.Vector3();
  let extent = 60;

  // Oblaci.
  const clouds = [];
  const cloudMat = new THREE.MeshStandardMaterial({ color: 0xffffff, flatShading: true, roughness: 1 });
  for (let i = 0; i < 14; i++) {
    const g = new THREE.Group();
    for (let j = 0; j < 3; j++) {
      const m = new THREE.Mesh(new THREE.IcosahedronGeometry(0.9 + Math.random() * 0.7, 0), cloudMat);
      m.position.set(j * 1.1 - 1.1, Math.random() * 0.3, Math.random() * 0.6);
      g.add(m);
    }
    g.userData.speed = 0.3 + Math.random() * 0.4;
    clouds.push(g);
    scene.add(g);
  }

  // Pravi more, kopno i jezera za mapu (prethodna se uklanja).
  function buildMap({ polygons, lakes }, route) {
    if (mapGroup) { scene.remove(mapGroup); disposeTree(mapGroup); }
    mapGroup = new THREE.Group();
    scene.add(mapGroup);

    const pts = route.map(r => toWorld(r[1], r[2]));
    const xs = pts.map(p => p.x), zs = pts.map(p => p.z);
    center.set((Math.min(...xs) + Math.max(...xs)) / 2, 0, (Math.min(...zs) + Math.max(...zs)) / 2);
    extent = Math.max(Math.max(...xs) - Math.min(...xs), Math.max(...zs) - Math.min(...zs));

    const size = extent + 200;
    seaGeo = new THREE.PlaneGeometry(size, size * 0.8, 64, 50);
    seaGeo.rotateX(-Math.PI / 2);
    seaBase = seaGeo.attributes.position.array.slice();
    const sea = new THREE.Mesh(seaGeo, new THREE.MeshStandardMaterial({ color: 0x1f86c2, flatShading: true, roughness: 0.55, metalness: 0.05 }));
    sea.position.set(center.x, 0.12, center.z);
    mapGroup.add(sea);

    const landMat = new THREE.MeshStandardMaterial({ color: 0x86c063, flatShading: true, roughness: 0.95 });
    const sandMat = new THREE.MeshStandardMaterial({ color: 0xe9d9a6, flatShading: true, roughness: 1 });
    polygons.forEach(rings => {
      const shape = shapeFrom(rings);
      const top = new THREE.Mesh(new THREE.ExtrudeGeometry(shape, { depth: LAND_TOP, bevelEnabled: false }), landMat);
      top.rotation.x = -Math.PI / 2;
      const rim = new THREE.Mesh(new THREE.ExtrudeGeometry(shape, { depth: 0.22, bevelEnabled: true, bevelThickness: 0.05, bevelSize: 0.35, bevelSegments: 1 }), sandMat);
      rim.rotation.x = -Math.PI / 2;
      mapGroup.add(top, rim);
    });
    const lakeMat = new THREE.MeshStandardMaterial({ color: 0x2b95cf, flatShading: true, roughness: 0.4 });
    lakes.forEach(poly => {
      const m = new THREE.Mesh(new THREE.ShapeGeometry(shapeFrom([poly])), lakeMat);
      m.rotation.x = -Math.PI / 2;
      m.position.y = LAND_TOP + 0.015;
      mapGroup.add(m);
    });
    clouds.forEach(g => g.position.set(center.x - extent / 2 - 20 + Math.random() * (extent + 40), 7 + Math.random() * 4, center.z - extent / 2 - 10 + Math.random() * (extent + 20)));
    return mapGroup;
  }

  // --- kamera ---
  let mode = 'orbit', orbitT = 0;
  const followTarget = new THREE.Vector3();
  let followSrc = followTarget;
  const look = new THREE.Vector3();

  const cam = {
    orbit() { mode = 'orbit'; },
    // v je živa referenca (pozicija žetona), pa kamera prati i tokom animacije.
    follow(v) { followSrc = v; mode = 'follow'; },
    finale(v) { followSrc = v; mode = 'finale'; orbitT = 0; }
  };

  function resize() {
    const w = container.clientWidth, h = container.clientHeight;
    renderer.setSize(w, h, false);
    camera.aspect = w / Math.max(1, h);
    camera.updateProjectionMatrix();
  }
  new ResizeObserver(resize).observe(container);
  resize();

  const frameHooks = [];
  let last = performance.now();
  function loop(now) {
    const dt = Math.min(0.05, (now - last) / 1000);
    last = now;
    for (let i = tasks.length - 1; i >= 0; i--) if (tasks[i](now)) tasks.splice(i, 1);
    frameHooks.forEach(f => f(dt, now));

    if (seaGeo) {
      const pos = seaGeo.attributes.position.array, t = now / 1000;
      for (let i = 0; i < pos.length; i += 3) {
        pos[i + 1] = seaBase[i + 1] + Math.sin(seaBase[i] * 0.35 + t * 1.1) * 0.06 + Math.cos(seaBase[i + 2] * 0.4 + t * 0.8) * 0.05;
      }
      seaGeo.attributes.position.needsUpdate = true;
      seaGeo.computeVertexNormals();
    }
    clouds.forEach(c => { c.position.x += c.userData.speed * dt; if (c.position.x > center.x + extent / 2 + 30) c.position.x = center.x - extent / 2 - 30; });

    followTarget.copy(followSrc);
    const portrait = camera.aspect < 0.9;
    const k = 1 - Math.exp(-dt * 2.2);
    if (mode === 'orbit') {
      orbitT += dt * 0.04;
      const r = Math.max(30, extent * 0.55) * (portrait ? 1.3 : 1);
      camera.position.lerp(new THREE.Vector3(center.x + Math.cos(orbitT) * r, r * (portrait ? 0.95 : 0.8), center.z + Math.sin(orbitT) * r * 0.7 + 10), k);
      look.lerp(center, k);
    } else if (mode === 'follow') {
      const shift = portrait ? 4.2 : 1.6;
      const desired = followTarget.clone().add(new THREE.Vector3(-1.5, portrait ? 13 : 9, portrait ? 12 : 9.5));
      camera.position.lerp(desired, k);
      look.lerp(followTarget.clone().add(new THREE.Vector3(0, 0, shift)), k);
    } else if (mode === 'finale') {
      orbitT += dt * 0.35;
      camera.position.lerp(followTarget.clone().add(new THREE.Vector3(Math.cos(orbitT) * 6, 4.5, Math.sin(orbitT) * 6)), k);
      look.lerp(followTarget, k);
    }
    camera.lookAt(look);
    renderer.render(scene, camera);
    requestAnimationFrame(loop);
  }
  requestAnimationFrame(loop);

  return { scene, camera, renderer, cam, buildMap, get mapGroup() { return mapGroup; },
    onFrame: f => { frameHooks.push(f); return () => { const i = frameHooks.indexOf(f); if (i >= 0) frameHooks.splice(i, 1); }; } };
}

// Planine i drveće (instancirano) oko rute, uz izbjegavanje staze.
export function scatterScenery(world, { polygons, lakes }, avoid, mapId) {
  const mountains = [], trees = [];
  const near = (p, r) => avoid.some(a => (a.x - p.x) ** 2 + (a.z - p.z) ** 2 < r * r);
  const onLand = (lon, lat) => polygons.some(rings => pointInPolygon(lon, lat, rings[0]) && !rings.slice(1).some(h => pointInPolygon(lon, lat, h)))
    && !lakes.some(l => pointInPolygon(lon, lat, l));
  const xs = avoid.map(a => a.x), zs = avoid.map(a => a.z), M = 24;
  const box = [Math.min(...xs) - M, Math.max(...xs) + M, Math.min(...zs) - M, Math.max(...zs) + M];
  const area = (box[1] - box[0]) * (box[3] - box[2]);
  const rand = () => [box[0] + Math.random() * (box[1] - box[0]), box[2] + Math.random() * (box[3] - box[2])];
  const target = Math.min(340, Math.round(area * 0.05)), treeTarget = Math.min(480, Math.round(area * 0.07));

  let tries = 0;
  while (mountains.length < target && tries++ < target * 20) {
    const [x, z] = rand(), [lon, lat] = fromWorld(x, z);
    if (!onLand(lon, lat)) continue;
    if (mapId === 'balkan' && lat > 44.9 && lon > 17.8 && lon < 22) continue; // Panonska nizija ostaje ravna
    const p = new THREE.Vector3(x, LAND_TOP, z);
    if (near(p, 1.4)) continue;
    // visina raste sa udaljenošću od staze, da planine ne zaklanjaju kameru
    const d = Math.sqrt(Math.min(...avoid.map(a => (a.x - p.x) ** 2 + (a.z - p.z) ** 2)));
    const south = avoid.some(a => p.z > a.z && p.z - a.z < 9 && Math.abs(p.x - a.x) < 4);
    const cap = 0.3 + d * (south ? 0.18 : 0.38);
    const rugged = Math.random() < 0.3 ? 0.8 + Math.random() * 1.6 : 0;
    const h = Math.min(cap, 0.35 + Math.random() * 0.8 + rugged);
    mountains.push({ p, h, r: 0.45 + h * 0.5 });
  }
  tries = 0;
  while (trees.length < treeTarget && tries++ < treeTarget * 20) {
    const [x, z] = rand(), [lon, lat] = fromWorld(x, z);
    if (!onLand(lon, lat)) continue;
    const p = new THREE.Vector3(x, LAND_TOP, z);
    if (near(p, 0.75)) continue;
    trees.push({ p, s: 0.7 + Math.random() * 0.6 });
  }

  const group = world.mapGroup;
  const m4 = new THREE.Matrix4(), q = new THREE.Quaternion(), c = new THREE.Color();
  const coneGeo = new THREE.ConeGeometry(1, 1, 5); coneGeo.translate(0, 0.5, 0);
  if (mountains.length) {
    const mMesh = new THREE.InstancedMesh(coneGeo, new THREE.MeshStandardMaterial({ flatShading: true, roughness: 1 }), mountains.length);
    const snow = mountains.filter(m => m.h > 1.55);
    const low = new THREE.Color(0x5f9a48), high = new THREE.Color(0x8d8f80);
    mountains.forEach((m, i) => {
      q.setFromAxisAngle(new THREE.Vector3(0, 1, 0), Math.random() * 6.28);
      m4.compose(m.p, q, new THREE.Vector3(m.r, m.h, m.r));
      mMesh.setMatrixAt(i, m4);
      mMesh.setColorAt(i, c.copy(low).lerp(high, Math.min(1, m.h / 2.4)));
    });
    group.add(mMesh);
    if (snow.length) {
      const sMesh = new THREE.InstancedMesh(coneGeo, new THREE.MeshStandardMaterial({ color: 0xffffff, flatShading: true, roughness: 0.8 }), snow.length);
      snow.forEach((m, i) => {
        const f = 0.36;
        m4.compose(m.p.clone().setY(m.p.y + m.h * (1 - f) - 0.01), q, new THREE.Vector3(m.r * f * 1.06, m.h * f * 1.02, m.r * f * 1.06));
        sMesh.setMatrixAt(i, m4);
      });
      group.add(sMesh);
    }
  }
  if (trees.length) {
    const treeGeo = new THREE.ConeGeometry(0.16, 0.5, 5); treeGeo.translate(0, 0.25, 0);
    const tMesh = new THREE.InstancedMesh(treeGeo, new THREE.MeshStandardMaterial({ flatShading: true, roughness: 1 }), trees.length);
    trees.forEach((t, i) => {
      m4.compose(t.p, q, new THREE.Vector3(t.s, t.s, t.s));
      tMesh.setMatrixAt(i, m4);
      tMesh.setColorAt(i, c.setHSL(0.3 + Math.random() * 0.06, 0.5, 0.25 + Math.random() * 0.1));
    });
    group.add(tMesh);
  }
}
