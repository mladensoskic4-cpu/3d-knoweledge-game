# Trka do Durmitora

3D kviz-trka (Three.js). Igra se pokreće otvaranjem `index.html` preko lokalnog servera
(npr. u VS Code ekstenzija "Live Server"), jer pregledač ne dozvoljava učitavanje fajlova direktno sa diska.

## Folderi
- `index.html` – stranica igre i stilovi
- `js/` – kod (scena, staza, kviz-engine, tipovi pitanja, mini igre, botovi, garaža, profil, mreža)
- `data/` – pitanja (`questions.json`), zastave, ilustracije, mape
- `supabase/schema.sql` – šema baze za Supabase (pokreni u SQL Editoru)

## Supabase
Upiši Project URL i anon (publishable) ključ u `js/net.js` (CONFIG).
Service role ključ nikad ne ide u kod.

## Licence za sadržaj
- Zastave: flag-icons (MIT)
- Ilustracije instrumenata i životinja: Microsoft Fluent Emoji (MIT)
- Mape: Natural Earth (javno vlasništvo)
