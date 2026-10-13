// Mali sintisajzer (Web Audio) za melodije. Note se zapisuju kao "E4:1 D#5:.5 R:1" (R = pauza, broj = trajanje u dobama).
let ctx = null;

export function audioContext() {
  if (!ctx) {
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return null;
    ctx = new AC();
  }
  if (ctx.state === 'suspended') ctx.resume().catch(() => {});
  return ctx;
}

const SEMI = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };
export function noteFreq(name) {
  const m = /^([A-G])([#b]?)(-?\d)$/.exec(name);
  if (!m) return null;
  const midi = 12 * (Number(m[3]) + 1) + SEMI[m[1]] + (m[2] === '#' ? 1 : m[2] === 'b' ? -1 : 0);
  return 440 * Math.pow(2, (midi - 69) / 12);
}

export function parseMelody(str) {
  return str.trim().split(/\s+/).map(tok => {
    const [n, d] = tok.split(':');
    return { freq: n === 'R' ? null : noteFreq(n), beats: Number(d || 1) };
  });
}

// Pušta melodiju u petlji dok se ne pozove stop(). Vraća { stop, playing }.
export function playMelody(str, { tempo = 110, loop = true, volume = 0.18 } = {}) {
  const ac = audioContext();
  if (!ac) return { stop() {}, playing: false };
  const notes = parseMelody(str);
  const beat = 60 / tempo;
  const master = ac.createGain();
  master.gain.value = volume;
  master.connect(ac.destination);
  let stopped = false, timer = 0;

  function schedule(startAt) {
    let t = startAt;
    for (const n of notes) {
      const dur = n.beats * beat;
      if (n.freq) {
        const o1 = ac.createOscillator(), o2 = ac.createOscillator(), g = ac.createGain();
        o1.type = 'triangle'; o2.type = 'sine';
        o1.frequency.value = n.freq; o2.frequency.value = n.freq * 2;
        const g2 = ac.createGain(); g2.gain.value = 0.25;
        o1.connect(g); o2.connect(g2); g2.connect(g); g.connect(master);
        g.gain.setValueAtTime(0.0001, t);
        g.gain.exponentialRampToValueAtTime(1, t + 0.015);
        g.gain.exponentialRampToValueAtTime(0.35, t + Math.min(0.25, dur * 0.6));
        g.gain.exponentialRampToValueAtTime(0.0001, t + dur * 0.95 + 0.04);
        o1.start(t); o2.start(t); o1.stop(t + dur + 0.08); o2.stop(t + dur + 0.08);
      }
      t += dur;
    }
    return t;
  }
  const total = notes.reduce((s, n) => s + n.beats * beat, 0);
  let next = ac.currentTime + 0.08;
  const run = () => {
    if (stopped) return;
    next = schedule(next) + (loop ? beat * 2 : 0);
    if (loop) timer = setTimeout(run, Math.max(50, (next - ac.currentTime - 0.3) * 1000));
  };
  run();

  return {
    playing: true,
    total,
    stop() {
      stopped = true; clearTimeout(timer);
      try {
        master.gain.setTargetAtTime(0.0001, ac.currentTime, 0.05);
        setTimeout(() => master.disconnect(), 400);
      } catch { /* zvuk je već ugašen */ }
    }
  };
}
