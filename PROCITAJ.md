# Trka do Durmitora

3D kviz-trka (Three.js). Igra je objavljena preko GitHub Pages. Lokalno se pokreće preko servera
(npr. u VS Code ekstenzija "Live Server"), jer pregledač ne dozvoljava učitavanje fajlova direktno sa diska.

## Šta je unutra
- 5.500 pitanja u 11 oblasti (po 500): geografija, istorija, sport, muzika i pop kultura, priroda i životinje,
  hrana i običaji, logika i matematika, filmovi i serije, nauka i svemir, jezici svijeta, tehnologija i gejming.
- Tipovi pitanja: višestruki izbor, tapni na mapi, procijeni broj (slajder ili tastatura), poredaj, spoji parove,
  mutna zastava (cijeli svijet), prepoznaj melodiju.
- Mini igre na stanicama (smjenjuju se): zastave u letu, glavni gradovi, memorija (zastava–država), mat u jednom potezu,
  oblik države, mapa svijeta (država, grad, rijeka), instrumenti (zvuk i foto), životinje (zvuk i foto), gdje je ovo (foto).
- Bonus runde (runde 4, 10 i 16): svi igrači igraju istu mini igru, pobjednik ide 3 polja naprijed.
- Rangovi, mape od Crne Gore do svijeta, garaža, power-upovi, rang-lista (Supabase), latinica / ćirilica / engleski,
  pozadinska muzika (dugme ♫).

## Unity verzija (Trka znanja)
U folderu `unity/` je nova verzija u Unity 6 (URP, 3D grafika, Android / iOS / PC / Web). Kako se otvara i testira:
vidi **`unity/PROCITAJ.md`**. Pitanja ostaju u `data/questions.json`; `python tools/pripremi_unity.py` ih kopira u Unity.

## Folderi
- `index.html` – stranica igre i stilovi
- `js/` – kod (scena, staza, kviz-engine, tipovi pitanja, mini igre, botovi, garaža, profil, mreža, muzika)
- `data/` – pitanja, zastave, mape, šah problemi, zvuci instrumenata
- `supabase/schema.sql` – šema baze
- `unity/` – Unity verzija igre (vidi `unity/PROCITAJ.md`)
- `tools/unity-core-tests`, `tools/unity-compile-check`, `tools/unity-stubs` – testovi Unity koda bez Unity-ja (.NET)

## Licence za sadržaj
- Zastave: flag-icons (MIT)
- Mape i granice: Natural Earth (javno vlasništvo); Kosovo je prikazano u sastavu Srbije
- Zvuci instrumenata: FluidR3 GM soundfont, Frank Wen, preko gleitz/midi-js-soundfonts (CC BY 3.0)
- Fotografije (instrumenti, životinje, mjesta) i zvuci životinja: učitavaju se uživo sa Wikipedije i Wikimedia Commonsa
- Ilustracije (rezerva kad fotografija nije dostupna): Microsoft Fluent Emoji (MIT)
- Šah problemi: generisani i provjereni bibliotekom chess.js
