// Pozadinska muzika: originalna, mirna petlja sintetizovana u pregledaču (bez fajlova i bez autorskih prava).
// Akordi u D-duru sa laganim arpeđom; sviše tiše dok se odgovara na pitanje sa melodijom ili zvukom.
let ctx = null, master = null, timer = 0, on = false, step = 0;
const KEY = 'trka-muzika';

const CHORDS = [
  [50, 57, 62, 66], // D
  [47, 54, 59, 62], // Hm
  [43, 50, 55, 59], // G
  [45, 52, 57, 61]  // A
];
const freq = m => 440 * Math.pow(2, (m - 69) / 12);

function voice(f, t, dur, gain, type = 'sine') {
  const o = ctx.createOscillator(), g = ctx.createGain(), lp = ctx.createBiquadFilter();
  o.type = type; o.frequency.value = f;
  lp.type = 'lowpass'; lp.frequency.value = 1800;
  g.gain.setValueAtTime(0.0001, t);
  g.gain.exponentialRampToValueAtTime(gain, t + Math.min(0.6, dur * 0.3));
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  o.connect(lp); lp.connect(g); g.connect(master);
  o.start(t); o.stop(t + dur + 0.05);
}

function schedule() {
  if (!on) return;
  const t0 = ctx.currentTime + 0.05, beat = 0.5;
  const chord = CHORDS[step % CHORDS.length];
  // podloga (pad)
  chord.forEach(m => voice(freq(m), t0, beat * 8, 0.035, 'triangle'));
  voice(freq(chord[0] - 12), t0, beat * 8, 0.05, 'sine');
  // arpeđo
  const arp = [0, 2, 1, 3, 2, 1, 3, 2];
  arp.forEach((k, i) => voice(freq(chord[k] + 12), t0 + i * beat, beat * 1.6, 0.03, 'sine'));
  step++;
  timer = setTimeout(schedule, beat * 8 * 1000 - 60);
}

export function musicEnabled() {
  try { return localStorage.getItem(KEY) !== 'off'; } catch { return true; }
}
export function startMusic() {
  if (on) return;
  try {
    ctx = ctx || new (window.AudioContext || window.webkitAudioContext)();
    if (ctx.state === 'suspended') ctx.resume();
    master = master || ctx.createGain();
    master.gain.value = 0.55; master.connect(ctx.destination);
    on = true; schedule();
  } catch { on = false; }
}
export function stopMusic() {
  on = false; clearTimeout(timer);
  try { master?.disconnect(); master = null; } catch { /* */ }
}
export function duck(down) {
  if (master && ctx) master.gain.setTargetAtTime(down ? 0.12 : 0.55, ctx.currentTime, 0.3);
}
export function setMusic(enabled) {
  try { localStorage.setItem(KEY, enabled ? 'on' : 'off'); } catch { /* */ }
  enabled ? startMusic() : stopMusic();
}
export const isPlaying = () => on;
