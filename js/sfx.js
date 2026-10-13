// Zvučni efekti (sintetizovani, bez fajlova): klik na dugme, tačno/netačno i zvuk vozila dok se kreće.
// Auto i kamion: motor koji „vuče” na svakom polju; konj: topot kopita.
import { audioCtx, noiseBuffer } from './music.js';

const KEY = 'trka-zvuk';
let out = null;
export function sfxEnabled() { try { return localStorage.getItem(KEY) !== 'off'; } catch { return true; } }
export function setSfx(on) { try { localStorage.setItem(KEY, on ? 'on' : 'off'); } catch { /* */ } }

function bus() {
  const c = audioCtx();
  if (!out) { out = c.createGain(); out.gain.value = 0.9; out.connect(c.destination); }
  return c;
}
function tone(f, t, dur, gain, type = 'sine', f2 = null) {
  const c = bus(), o = c.createOscillator(), g = c.createGain();
  o.type = type; o.frequency.setValueAtTime(f, t);
  if (f2) o.frequency.exponentialRampToValueAtTime(f2, t + dur);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.exponentialRampToValueAtTime(gain, t + 0.008);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  o.connect(g); g.connect(out); o.start(t); o.stop(t + dur + 0.02);
}
function burst(t, { dur = 0.05, gain = 0.3, type = 'bandpass', freq = 1500, q = 1.2 } = {}) {
  const c = bus(), s = c.createBufferSource(), f = c.createBiquadFilter(), g = c.createGain();
  s.buffer = noiseBuffer(); f.type = type; f.frequency.value = freq; f.Q.value = q;
  g.gain.setValueAtTime(gain, t); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  s.connect(f); f.connect(g); g.connect(out); s.start(t, Math.random() * 0.8, dur + 0.02);
}
const safe = fn => (...a) => { if (!sfxEnabled()) return; try { fn(...a); } catch { /* zvuk nije obavezan */ } };

export const click = safe(() => { const t = bus().currentTime; tone(1100, t, 0.06, 0.12, 'triangle', 700); });
export const correct = safe(() => { const t = bus().currentTime; tone(784, t, 0.14, 0.16, 'triangle'); tone(1175, t + 0.1, 0.24, 0.16, 'triangle'); });
export const wrong = safe(() => { const t = bus().currentTime; tone(220, t, 0.16, 0.12, 'square', 160); tone(165, t + 0.13, 0.26, 0.12, 'square', 110); });
export const coin = safe(() => { const t = bus().currentTime; tone(988, t, 0.08, 0.12, 'square'); tone(1319, t + 0.07, 0.22, 0.12, 'square'); });

// Vozilo: start() vraća objekat sa hop() (jedno polje) i stop().
export function vehicle(kind = 'auto') {
  const none = { hop() {}, stop() {} };
  if (!sfxEnabled()) return none;
  try {
    const c = bus();
    if (kind === 'konj') {
      return {
        hop() {
          // galop: tri udarca kopita po polju
          const t = c.currentTime;
          [0, 0.07, 0.15].forEach((d, i) => {
            burst(t + d, { dur: 0.045, gain: 0.5 - i * 0.08, freq: 900 + i * 350, q: 2.5 });
            tone(320 - i * 40, t + d, 0.05, 0.12, 'sine', 180);
          });
        },
        stop() { const t = c.currentTime; tone(500, t + 0.05, 0.35, 0.05, 'sawtooth', 380); }
      };
    }
    const truck = kind === 'kamion';
    const base = truck ? 42 : 68;
    const o1 = c.createOscillator(), o2 = c.createOscillator(), lp = c.createBiquadFilter(), g = c.createGain();
    o1.type = 'sawtooth'; o2.type = 'square';
    o1.frequency.value = base; o2.frequency.value = base * 1.01;
    lp.type = 'lowpass'; lp.frequency.value = truck ? 420 : 700; lp.Q.value = 3;
    g.gain.setValueAtTime(0.0001, c.currentTime);
    g.gain.exponentialRampToValueAtTime(truck ? 0.2 : 0.16, c.currentTime + 0.12);
    o1.connect(lp); o2.connect(lp); lp.connect(g); g.connect(out);
    o1.start(); o2.start();
    let n = 0;
    return {
      hop() {
        // gas na svakom polju: obrtaji porastu pa malo padnu
        const t = c.currentTime, up = base * (1.7 + Math.min(n, 4) * 0.12);
        n++;
        [o1, o2].forEach((o, i) => {
          o.frequency.cancelScheduledValues(t);
          o.frequency.setTargetAtTime(up * (i ? 1.01 : 1), t, 0.05);
          o.frequency.setTargetAtTime(base * 1.3 * (i ? 1.01 : 1), t + 0.18, 0.08);
        });
        if (truck) burst(t, { dur: 0.12, gain: 0.08, type: 'lowpass', freq: 300, q: 0.7 });
      },
      stop() {
        const t = c.currentTime;
        g.gain.cancelScheduledValues(t);
        g.gain.setTargetAtTime(0.0001, t, 0.12);
        [o1, o2].forEach(o => { o.frequency.setTargetAtTime(base * 0.8, t, 0.15); o.stop(t + 0.6); });
      }
    };
  } catch { return none; }
}

// Klik na svako dugme u igri.
export function installClicks() {
  document.addEventListener('pointerdown', e => {
    const b = e.target.closest?.('button, .mc-opt, [role="button"], .mem-card, .sq');
    if (b && !b.disabled) click();
  }, true);
}
