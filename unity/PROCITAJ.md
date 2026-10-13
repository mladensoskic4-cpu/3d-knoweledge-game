# Trka znanja – Unity verzija

Ovo je Unity 6 (URP) verzija igre. Pitanja, mape i zastave su iste kao u web verziji. Web verzija na GitHub Pages radi i dalje, dok ova ne bude spremna.

Ovo je **prva verzija jezgra**:
- meni;
- svjetovi koji se otključavaju rangom: CG → Balkan → Evropa → Svijet;
- trka od 5, 10 ili 15 minuta;
- svih 7 tipova pitanja;
- botovi sa nasumičnim imenima;
- stanice i bonus runde sa mini igrom „Zastave u letu”;
- postolje, vatromet i konfete na cilju;
- garaža sa 3D izlogom: 15 životinja, 12 vozila, boje, kape i naočare;
- podešavanja (jezik, kvalitet grafike, zvuk, vibracija);
- Supabase (gost nalog i rezultati) i igra bez interneta.

Ostalih 10 mini igara stiže jedna po jedna.

## 1. Instalacija (jednom)

1. Instaliraj **Unity Hub**: https://unity.com/download
2. U Hubu: **Installs → Install Editor → Unity 6 (LTS)**, najnovija 6000.0.x. Uz nju označi module:
   - **Android Build Support** (sa OpenJDK i Android SDK & NDK);
   - **WebGL Build Support**;
   - **Windows Build Support** (već je uključen na Windowsu);
   - iOS Build Support samo ako radiš na Macu (iOS build se pravi samo na Macu).
3. Prijavi se u Hub sa Unity nalogom i aktiviraj besplatnu **Personal** licencu.

## 2. Otvaranje projekta

1. Unity Hub → **Projects → Add → Add project from disk**.
2. Izaberi folder **`unity`** u repou (`3d-knoweledge-game/unity`).
3. Otvori ga sa Unity 6. Prvo otvaranje traje nekoliko minuta (uvoz paketa).
4. Kad se otvori, igra se **sama podesi**:
   - URP sa sjenkama i post-processingom;
   - scena „Igra”;
   - polegnut ekran;
   - ime „Trka znanja”.

   U konzoli piše: „Trka znanja: projekat podešen”. Ako se to ne desi, u meniju izaberi **Trka znanja → Podesi projekat**.
5. Otvori scenu **Assets/TrkaZnanja/Scenes/Igra** i pritisni **Play** ▶.
6. U prozoru Game izaberi rezoluciju **Full HD (1920x1080)**, jer je igra polegnuta.

Ako nešto pukne pri prvom otvaranju (crvene greške u konzoli), uslikaj konzolu i pošalji mi. Ja ovdje nemam Unity, pa je moguća sitnica koju odmah ispravim.

## 3. Testovi

U Unity-ju otvori **Window → General → Test Runner**:
- **EditMode → Run All**: logika igre (26 testova: pitanja, špil, bodovanje, trka, profil, teren, ćirilica, zvuk).
- **PlayMode → Run All**: igra se pokrene, napravi svaku mapu u svakoj atmosferi, sve životinje i vozila, otvori svaki tip pitanja i **odigra cijelu trku sama** (AutoPlay). Traje 1–3 minuta.

Testovi koriste poseban profil (`profil-test.json`) i ne diraju tvoj napredak.

Iste testove logike i provjeru da se sav kod kompajlira pokrećem i ja, bez Unity-ja:

```
dotnet run --project tools/unity-core-tests
dotnet build tools/unity-compile-check
```

## 4. Šta da commituješ poslije prvog otvaranja

Unity napravi nekoliko fajlova koji moraju u repo (GitHub Desktop → Commit → Push):
- `unity/ProjectSettings/` (cijeli folder, sa `ProjectVersion.txt`);
- `unity/Packages/packages-lock.json`;
- svi **`.meta`** fajlovi u `unity/Assets/`;
- `unity/Assets/TrkaZnanja/Settings/` (URP podešavanja) i `unity/Assets/TrkaZnanja/Scenes/`.

`Library`, `Temp` i `Logs` se ne commituju (već su u `.gitignore`).

## 5. Automatski Unity testovi na GitHub-u (GameCI)

Poslije svakog pusha GitHub sam pokreće testove (Actions → „Unity testovi”):
- **Logika i kompajliranje** radi odmah, bez licence.
- **Unity EditMode + PlayMode** radi kad dodaš licencu:
  1. Aktiviraj Personal licencu u Unity Hubu (ako već nisi).
  2. Nađi fajl licence:
     - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
     - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
  3. Na GitHub-u: repo → **Settings → Secrets and variables → Actions → New repository secret**. Dodaj tri tajne:
     - `UNITY_LICENSE`: cijeli sadržaj `.ulf` fajla (otvori ga u Notepadu i kopiraj sve);
     - `UNITY_EMAIL`: email Unity naloga;
     - `UNITY_PASSWORD`: šifra Unity naloga.
  4. Commituj `unity/ProjectSettings` (korak 4), da GitHub zna verziju Unity-ja.

Tajne vidi samo GitHub Actions; niko ih ne može pročitati iz repoa.

## 6. Pravi zvukovi (CC0) – opciono

Igra trenutno sama sintetiše zvukove i muziku sa domaćim motivima (frula, harmonika, ritam kola). Ako dodaš pravi zvuk sa istim imenom, igra koristi njega:
1. Skini besplatne CC0 pakete sa **kenney.nl** (Interface Sounds, Impact Sounds, Music Jingles).
2. Izaberi zvukove i preimenuj ih (`.ogg` ili `.wav`):
   - `klik`, `tacno`, `netacno`, `novcic`, `fanfara`, `vatromet`;
   - `muzika` (petlja za pozadinu);
   - `vozilo-auto`, `vozilo-motor`… (jedan „korak” vozila).
3. Stavi ih u **`unity/Assets/TrkaZnanja/Resources/Zvuk/`**.

CC0 znači da ih smiješ koristiti i u igri koja zarađuje, bez navođenja autora (ali je lijepo napisati u Zaslugama).

## 7. Podaci (pitanja, mape, zastave)

Pitanja se uređuju na jednom mjestu, u `data/questions.json`, kao i do sada. Poslije izmjene pokreni iz glavnog foldera:

```
python tools/pripremi_unity.py
```

Skripta kopira pitanja, mape i države u Unity i pretvara zastave iz WebP u PNG (Unity ne čita WebP).

## 8. Build za telefon

**Android:**
1. File → Build Profiles → Android → **Switch Platform**.
2. Uključi telefon u USB (sa uključenim „USB debugging”).
3. **Build And Run**.

ID aplikacije je `me.trkaznanja.igra`, a projekat je već podešen za IL2CPP i ARM64, kako Google Play traži.

**PC:** Build Profiles → Windows → Build.

**Web:** Build Profiles → Web → Build.

## Gdje je šta

| Folder | Šta |
|---|---|
| `Assets/TrkaZnanja/Core` | logika bez Unity-ja (pitanja, špil, trka, botovi, profil, teren, prevodi, sintisajzer) |
| `Assets/TrkaZnanja/Game/World` | 3D: teren, voda, staza, znamenitosti, životinje i vozila, kamera, efekti, atmosfera |
| `Assets/TrkaZnanja/Game/UI` | ekrani: meni, pitanja (7 tipova), HUD, garaža, mini igre, cilj |
| `Assets/TrkaZnanja/Game/Services` | podaci, zvuk, vibracija, Supabase, čuvanje profila |
| `Assets/TrkaZnanja/Resources` | šejderi, fontovi (Nunito, OFL), pitanja, mape, zastave |
| `Assets/TrkaZnanja/Editor` | automatsko podešavanje projekta |
| `Assets/TrkaZnanja/Tests` | EditMode i PlayMode testovi |
