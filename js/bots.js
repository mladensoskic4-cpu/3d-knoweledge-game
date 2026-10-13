// Botovi: profil po oblasti (vjerovatnoća tačnog), prosječna brzina i tri nivoa težine.
import { clamp } from './util.js';

export const LEVELS = {
  lako:    { label: 'Lako',    acc: 0.72, time: 1.25 },
  srednje: { label: 'Srednje', acc: 0.9,  time: 1.0 },
  tesko:   { label: 'Teško',   acc: 1.08, time: 0.8 }
};

// speed = prosječni udio vremena koji bot potroši na odgovor (manje = brže).
export const ROSTER = [
  { id: 'vuk', name: 'Vuk', color: '#2b7de9', speed: 0.42, tagline: 'Brz na obaraču',
    skills: { geografija: 0.6, istorija: 0.4, sport: 0.88, muzika: 0.65, priroda: 0.5, hrana: 0.6, logika: 0.5, film: 0.75, nauka: 0.5, jezici: 0.55, tehnologija: 0.85 } },
  { id: 'jelena', name: 'Jelena', color: '#16a36a', speed: 0.58, tagline: 'Zna svaku uvalu',
    skills: { geografija: 0.88, istorija: 0.65, sport: 0.4, muzika: 0.55, priroda: 0.8, hrana: 0.7, logika: 0.6, film: 0.5, nauka: 0.7, jezici: 0.85, tehnologija: 0.5 } },
  { id: 'luka', name: 'Luka', color: '#f08a1c', speed: 0.7, tagline: 'Spor, ali temeljan',
    skills: { geografija: 0.6, istorija: 0.9, sport: 0.62, muzika: 0.4, priroda: 0.55, hrana: 0.65, logika: 0.85, film: 0.6, nauka: 0.88, jezici: 0.6, tehnologija: 0.65 } }
];

export function strongWeak(bot, areas) {
  const list = areas.map(a => [a, bot.skills[a] ?? 0.5]).sort((x, y) => y[1] - x[1]);
  return { strong: list[0][0], weak: list[list.length - 1][0] };
}

export function createBots(level) {
  return ROSTER.map(b => ({ ...b, level, bot: true }));
}

export function botSkill(bot, area, tezina = 2) {
  const base = bot.skills[area] ?? 0.5;
  return clamp(base * LEVELS[bot.level].acc - (tezina - 2) * 0.07, 0.05, 0.96);
}

export function botAnswer(bot, q, type, limit, rng = Math.random) {
  const p = botSkill(bot, q.oblast, q.tezina);
  let time = limit * bot.speed * LEVELS[bot.level].time * (0.6 + rng() * 0.8);
  time = clamp(time, 1.2, limit * 1.05);
  if (time >= limit) return { accuracy: 0, timeUsed: limit, timedOut: true };
  return { accuracy: type.simulateBot(q, p, rng), timeUsed: time, timedOut: false };
}

export function botMinigameSkill(bot) {
  const vals = Object.values(bot.skills);
  return clamp((vals.reduce((a, b) => a + b, 0) / vals.length) * LEVELS[bot.level].acc, 0.05, 0.95);
}
