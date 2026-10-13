// Ekran „Zasluge”: autor i licenca za svaku fotografiju (Wikimedia Commons traži navođenje autora),
// plus izvori ostalih materijala u igri. Podaci se čitaju iz <kategorija>/podaci.json.
import { h, openCard } from './ui.js';
import { KATEGORIJE, loadSlike } from './slike.js';

const OSTALO = [
  ['Zastave', 'flag-icons, MIT licenca'],
  ['Mape i granice', 'Natural Earth (javno dobro), world-atlas'],
  ['Zvuci instrumenata', 'FluidR3 GM, Frank Wen, CC BY 3.0'],
  ['Zvuci životinja', 'snimci iz Wikipedia članaka (Wikimedia Commons); autor i licenca na stranici snimka'],
  ['Ilustracije', 'Microsoft Fluent Emoji, MIT licenca'],
  ['Šahovski problemi', 'provjereni bibliotekom chess.js (BSD licenca)']
];

export async function openCredits(onClose) {
  const card = openCard('credits-card');
  const back = h('button', { class: 'btn btn-primary', type: 'button', id: 'zasluge-nazad' }, 'Nazad');
  const body = h('div', { class: 'cr-body' }, h('p', { class: 'hint' }, 'Učitavam…'));
  card.append(h('h2', { class: 'q-text' }, 'Zasluge'), body, h('div', { class: 'row-bar end' }, back));
  back.addEventListener('click', () => onClose?.());

  const s = await loadSlike();
  const total = Object.values(s).reduce((a, l) => a + l.length, 0);
  const sections = Object.entries(KATEGORIJE).filter(([k]) => s[k].length).map(([k, label]) =>
    h('details', { class: 'cr-sec' },
      h('summary', {}, h('b', {}, label), h('span', { class: 'cr-count' }, `${s[k].length} fotografija`)),
      h('ol', { class: 'cr-list' }, s[k].map(x => h('li', {},
        h('img', { src: x.slika, alt: '', loading: 'lazy', class: 'cr-thumb' }),
        h('div', { class: 'cr-txt', translate: 'no' },
          h('b', {}, x.naziv),
          h('span', {}, `Autor: ${x.autor || 'nepoznat'}`),
          h('span', {}, 'Licenca: ', x.licenca_url ? h('a', { href: x.licenca_url, target: '_blank', rel: 'noopener' }, x.licenca) : (x.licenca || '—'),
            x.izvor ? [' · ', h('a', { href: x.izvor, target: '_blank', rel: 'noopener' }, 'izvor')] : null)))))));
  body.replaceChildren(
    h('p', { class: 'hint' }, total
      ? 'Fotografije su sa Wikimedia Commonsa, pod slobodnim licencama. Tapni kategoriju da vidiš autora i licencu svake slike.'
      : 'Fotografije još nisu dodate u igru.'),
    ...sections,
    h('details', { class: 'cr-sec', open: '' },
      h('summary', {}, h('b', {}, 'Ostali materijali')),
      h('ul', { class: 'cr-other', translate: 'no' }, OSTALO.map(([a, b]) => h('li', {}, h('b', {}, a + ': '), b)))));
}
