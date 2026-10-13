// Rang-lista i predlaganje pitanja. Bez servera prikazuje tvoje rezultate i čuva predloge na uređaju.
import { h, openCard, toast, AREAS } from './ui.js';
import { profile, save } from './profile.js';
import { isOnline, leaderboard, submitSuggestion } from './net.js';

const SCOPES = [['danas', 'Danas'], ['sedmica', 'Sedmica'], ['ukupno', 'Ukupno']];
const FILTERS = [['svi', 'Svi'], ['grad', 'Moj grad'], ['skola', 'Moja škola']];

export function openLeaderboard(onClose) {
  const P = profile();
  let scope = 'sedmica', filter = 'svi';
  const card = openCard('leaderboard');
  const list = h('ol', { class: 'podium lb-list' });
  const note = h('p', { class: 'hint' });
  const tabsScope = h('div', { class: 'seg' });
  const tabsFilter = h('div', { class: 'seg' });
  const back = h('button', { class: 'btn btn-primary', type: 'button', id: 'lb-back' }, 'Nazad');
  card.append(h('h2', { class: 'q-text' }, 'Rang-lista'), tabsScope, tabsFilter, note, list, h('div', { class: 'row-bar end' }, back));

  function tabs(el, items, cur, set) {
    el.replaceChildren(...items.map(([k, l]) => {
      const b = h('button', { type: 'button', id: `lb-${k}`, 'aria-checked': String(cur === k), role: 'radio' }, l);
      b.addEventListener('click', () => { set(k); render(); });
      return b;
    }));
  }
  async function render() {
    tabs(tabsScope, SCOPES, scope, k => (scope = k));
    tabs(tabsFilter, FILTERS, filter, k => (filter = k));
    if ((filter === 'grad' && !P.grad) || (filter === 'skola' && !P.skola)) {
      note.textContent = filter === 'grad' ? 'Upiši svoj grad u meniju da vidiš rang-listu grada.' : 'Upiši svoju školu u meniju da vidiš rang-listu škole.';
      list.replaceChildren();
      return;
    }
    if (!isOnline()) {
      note.textContent = 'Rang-lista sa drugim igračima se uključuje kad povežemo server. Do tada vidiš svoje posljednje trke.';
      const since = scope === 'danas' ? new Date().setHours(0, 0, 0, 0) : scope === 'sedmica' ? Date.now() - 7 * 864e5 : 0;
      const mine = (P.istorija || []).filter(r => new Date(r.datum).getTime() >= since);
      list.replaceChildren(...(mine.length ? mine.slice(0, 20).map((r, i) => h('li', { class: 'me' },
        h('span', { class: 'place' }, `${i + 1}.`), h('i', { class: 'dot', style: { background: '#e0312f' } }),
        h('span', { class: 'sum-name' }, `${r.mjesto}. mjesto`),
        h('span', { class: 'sum-text' }, `${r.tacni}/${r.odgovori} dobrih · ${new Date(r.datum).toLocaleDateString('sr-Latn')}`),
        h('span', { class: 'sum-time' }, `+${r.bodovi}`))) : [h('li', {}, h('span', { class: 'sum-text' }, 'Još nema trka u ovom periodu.'))]));
      return;
    }
    note.textContent = 'Učitavam…';
    const data = await leaderboard(scope, filter === 'grad' ? { grad: P.grad } : filter === 'skola' ? { skola: P.skola } : {});
    note.textContent = data ? '' : 'Rang-lista se trenutno ne može učitati. Provjeri internet i pokušaj ponovo.';
    list.replaceChildren(...(data || []).map((r, i) => h('li', { class: r.ja ? 'me' : '' },
      h('span', { class: 'place' }, `${i + 1}.`), h('i', { class: 'dot', style: { background: r.ja ? '#e0312f' : '#8f9ba6' } }),
      h('span', { class: 'sum-name' }, r.ime), h('span', { class: 'sum-text' }, [r.grad, r.skola].filter(Boolean).join(' · ')),
      h('span', { class: 'sum-time' }, `${r.bodovi}`))));
  }
  render();
  back.addEventListener('click', () => onClose?.());
}

export function openSuggest(onClose) {
  const P = profile();
  const card = openCard('suggest');
  const area = h('select', { id: 'sg-oblast', class: 'flag-select' }, Object.entries(AREAS).map(([k, a]) => h('option', { value: k }, a.label)));
  const text = h('textarea', { id: 'sg-pitanje', class: 'sg-text', rows: 2, maxlength: 300, placeholder: 'Npr. Koja rijeka teče kroz Bijelo Polje?' });
  const opts = [0, 1, 2, 3].map(i => h('input', { id: `sg-o${i}`, class: 'sg-opt', type: 'text', maxlength: 80, placeholder: i === 0 ? 'Tačan odgovor' : `Netačan odgovor ${i}` }));
  const err = h('p', { class: 'hint sg-err', role: 'alert' });
  const send = h('button', { class: 'btn btn-primary', type: 'button', id: 'sg-send' }, 'Pošalji');
  const back = h('button', { class: 'btn btn-ghost', type: 'button', id: 'sg-back' }, 'Nazad');
  card.append(
    h('h2', { class: 'q-text' }, 'Predloži pitanje'),
    h('p', { class: 'lede small' }, 'Napiši pitanje sa četiri ponuđena odgovora. Prvi odgovor je tačan, a u igri se odgovori miješaju. Pitanje ulazi u igru kad ga odobrimo.'),
    h('div', { class: 'sg-form' },
      h('label', { for: 'sg-oblast' }, 'Oblast'), area,
      h('label', { for: 'sg-pitanje' }, 'Pitanje'), text,
      h('span', { class: 'sg-lbl' }, 'Odgovori'), h('div', { class: 'sg-opts' }, opts)),
    err,
    h('div', { class: 'row-bar end' }, back, send));

  send.addEventListener('click', async () => {
    const pitanje = text.value.trim(), o = opts.map(i => i.value.trim());
    if (pitanje.length < 10) { err.textContent = 'Pitanje je prekratko. Napiši bar 10 znakova.'; text.focus(); return; }
    if (o.some(x => !x)) { err.textContent = 'Popuni sva četiri odgovora.'; opts.find(i => !i.value.trim()).focus(); return; }
    if (new Set(o.map(x => x.toLowerCase())).size < 4) { err.textContent = 'Odgovori moraju biti različiti.'; return; }
    const s = { oblast: area.value, tip: 'izbor', pitanje, opcije: o, tacno: o[0] };
    send.disabled = true;
    const ok = await submitSuggestion(s);
    if (!ok) { P.predlozi = [...(P.predlozi || []), { ...s, datum: new Date().toISOString() }]; save(); }
    toast(ok ? 'Hvala! Pitanje je poslato na pregled.' : 'Hvala! Pitanje je sačuvano i biće poslato kad se poveže server.', '#1d8a4e');
    onClose?.();
  });
  back.addEventListener('click', () => onClose?.());
}
