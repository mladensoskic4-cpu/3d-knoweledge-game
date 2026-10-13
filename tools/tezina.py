import json, re, sys, collections
sys.path.insert(0, '/tmp/claude-0/-home-claude-3d-knoweledge-game/8256b797-5ba7-5634-98e5-2ca43ec2fccb/scratchpad')
from labels import L

# (oblast prefiks, tip ili None, regex na tekst pitanja, osnovna težina 0..1)
# 0 = pitanje za djecu (poznato mlađima: igre, crtani, životinje, planete, CG priroda)
# 1 = pitanje za odrasle (klasika, stari datumi, daleke države, precizni brojevi)
RULES = [
 ('film','izbor',r'Iz koje zemlje je serija',0.3), ('film','izbor',r'Ko je režirao',0.7),
 ('film','poredaj',r'',0.6), ('film','procjena',r'Koje godine',0.8), ('film','spoji',r'sa režiserom',0.75),
 ('film','spoji',r'Spoji lik sa glumcem',0.45), ('film','spoji',r'seriju sa zemljom',0.4),
 ('film','spoji',r'crtan',0.1), ('film','spoji',r'Spoji režisera',0.85),
 ('geo','izbor',r'kontinent',0.05), ('geo','izbor',r'nacionaln|jezer|planin|Crn[aoe] Gor|Durmitor|Lovć|Skadar|Tar',0.15),
 ('geo','izbor',r'glavni grad',0.5), ('geo','mutna',r'',0.4), ('geo','poredaj',r'rijek',0.2),
 ('geo','poredaj',r'površin',0.6), ('geo','spoji',r'glavnim gradom',0.65), ('geo','procjena',r'površin',0.9),
 ('hra','spoji',r'jelo sa zemljom',0.75), ('hra','spoji',r'piće sa sirovinom',0.8), ('hra','spoji',r'sir sa zemljom',0.75),
 ('hra','izbor',r'Iz koje zemlje',0.7), ('hra','poredaj',r'praznik|običaj|obrok|jela',0.15),
 ('hra','poredaj',r'korak|faz',0.3), ('hra','procjena',r'ključa|temperatur',0.85),
 ('ist','poredaj',r'',0.8), ('ist','spoji',r'sa godinom',0.85), ('ist','spoji',r'ličnost',0.75),
 ('ist','procjena',r'Koje godine',0.85), ('ist','izbor',r'Podgoric|Titograd|Cetinj|Crn[aoe] Gor|Njegoš|Petrović',0.2),
 ('ist','izbor',r'',0.7),
 ('jez','izbor',r'Kako se na jeziku',0.3), ('jez','izbor',r'Na kom jeziku',0.35), ('jez','izbor',r'službeni jezik',0.3),
 ('jez','izbor',r'pismom',0.15), ('jez','spoji',r'službenim jezikom',0.2), ('jez','spoji',r'sa pismom',0.4),
 ('jez','spoji',r'Spoji riječ',0.7),
 ('log',None,r'×',0.45), ('log','izbor',r'Ako je danas',0.1), ('log','izbor',r'rimsk',0.4),
 ('log','poredaj',r'jedinic',0.4), ('teh','poredaj',r'jedinic',0.4), ('spo',None,r'Koliko igrača|Koliko traje|visini je|dug',0.3),
 ('geo','mapa',r'',0.25), ('ist','mapa',r'',0.55), ('hra','mapa',r'',0.4),
 ('log','izbor',r'niz',0.4), ('log','izbor',r'%',0.6), ('log','izbor',r'x ako je',0.5), ('log','izbor',r'√',0.4),
 ('log','izbor',r'minuta|sati|sekund',0.3), ('log','izbor',r'površin|obim|stranic|ugl',0.05),
 ('log','poredaj',r'brojeve',0.2), ('log','poredaj',r'razlom',0.3), ('log','procjena',r'Procijeni',0.7),
 ('log','procjena',r'\+',0.2), ('log','spoji',r'procen',0.75), ('log','spoji',r'izraz',0.4),
 ('muz','izbor',r'izvođač',0.15), ('muz','izbor',r'kompozitor',0.6), ('muz','izbor',r'znači',0.7),
 ('muz','melodija',r'',0.5), ('muz','poredaj',r'',0.85), ('muz','spoji',r'instrument',0.3),
 ('muz','spoji',r'izvođača',0.3), ('muz','spoji',r'kompozitora sa djelom',0.4), ('muz','spoji',r'pojam',0.75),
 ('muz','procjena',r'rođen',0.9),
 ('nau','izbor',r'otkriću|izumu',0.75), ('nau','izbor',r'po redu planeta',0.1), ('nau','izbor',r'simbol',0.6),
 ('nau','poredaj',r'element',0.75), ('nau','poredaj',r'planet',0.05), ('nau','procjena',r'atomski',0.9),
 ('nau','spoji',r'element',0.6), ('nau','spoji',r'mjern',0.45), ('nau','spoji',r'otkriće',0.75),
 ('pri','izbor',r'grupu životinja',0.1), ('pri','spoji',r'element',0.6), ('pri','spoji',r'sa grupom',0.05),
 ('pri','spoji',r'kontinent',0.2), ('pri','spoji',r'ishran',0.15), ('pri','poredaj',r'planet',0.1),
 ('pri','izbor',r'Bar|Durmitor|Tar|Lovć|Skadar|Crn[aoe] Gor|Biograd|Prokletij|maslin',0.15),
 ('pri','procjena',r'Koje godine',0.45),
 ('spo','izbor',r'olimpijske',0.8), ('spo','izbor',r'klub',0.35), ('spo','izbor',r'domaćin',0.6),
 ('spo','mapa',r'',0.4), ('spo','poredaj',r'olimpij',0.85), ('spo','spoji',r'Svjetsko prvenstvo',0.7),
 ('spo','spoji',r'klub',0.35), ('spo','spoji',r'sa državom',0.35), ('spo','spoji',r'sa sportom',0.1),
 ('spo','procjena',r'rođen',0.4),
 ('teh','izbor',r'Iz koje zemlje je kompanija',0.1), ('teh','izbor',r'napravio igru',0.3),
 ('teh','izbor',r'binarni',0.6), ('teh','izbor',r'heksadek',0.85), ('teh','poredaj',r'igre',0.15),
 ('teh','poredaj',r'kompanij',0.6), ('teh','procjena',r'igra',0.3),
 ('teh','procjena',r'Mojang|Nintendo|Epic|Rockstar|Valve|Roblox|Riot|Ubisoft|Blizzard|Supercell|Rovio|Sony|Sega',0.3),
 ('teh','procjena',r'',0.6), ('teh','spoji',r'osnivač',0.6), ('teh','spoji',r'skraćen',0.75),
]
def base(q):
    for a, t, rx, v in RULES:
        if q['oblast'].startswith(a) and (t is None or q['tip'] == t) and re.search(rx, q['pitanje']):
            return v, True
    return None, False

def years(q):
    ys = []
    for s in (q.get('oznake') or []) + [str(q.get('tacno'))]:
        ys += [int(y) for y in re.findall(r'\b(1[5-9]\d\d|20[0-2]\d)\b', str(s))]
    return ys

def score(q):
    b, hit = base(q)
    tz = ((q.get('tezina') or 2) - 1) / 2
    if b is None: b = tz
    s = 0.7 * b + 0.3 * tz
    ys = years(q)
    if ys and q['oblast'] in ('film', 'tehnologija', 'sport', 'muzika'):
        if min(ys) >= 2000: s -= 0.12
        elif max(ys) < 1980: s += 0.1
    if q['oblast'] == 'logika' and q['tip'] in ('izbor', 'procjena'):
        nums = [int(n) for n in re.findall(r'\d+', q['pitanje'])]
        if len(nums) >= 2 and '×' in q['pitanje'] and min(nums[:2]) >= 10: s += 0.2
        if nums and max(nums) < 10: s -= 0.15
        elif nums and max(nums) >= 100: s += 0.1
    if q['oblast'] != 'istorija' and re.search(r'Crn[aoe] Gor|Podgoric|Durmitor|Žabljak|Budv|Kotor|Bar\b|Ulcinj|Nikšić|Cetinj|Lovćen|Skadar|Tar[ae]?\b|Vučević|Biograd', q['pitanje'] + str(q.get('tacno'))):
        s -= 0.1
    return max(0, min(1, s)), hit

if __name__ == '__main__':
    data = json.load(open('/home/claude/trka/data/questions.json'))
    Q = data['pitanja']
    byid = {q['id']: q for q in Q}
    sample = json.load(open('/tmp/claude-0/-home-claude-3d-knoweledge-game/8256b797-5ba7-5634-98e5-2ca43ec2fccb/scratchpad/uzorak50.json'))
    ok = n = 0
    for s, lab in zip(sample, L):
        if lab is None: continue
        sc, _ = score(byid[s['id']])
        pred = 1 if sc < 0.5 else 3
        n += 1; ok += pred == lab
        if pred != lab: print('NE SLAŽE SE', lab, round(sc, 2), s['tekst'][:90])
    print(f'slaganje sa oznakama: {ok}/{n}')
    sc = [score(q) for q in Q]
    print('bez pravila:', sum(1 for s, h in sc if not h))
    vals = sorted(s for s, _ in sc)
    print('kvantili', [round(vals[int(len(vals) * p)], 2) for p in (0.1, 0.25, 0.35, 0.5, 0.65, 0.75, 0.9)])
