#!/usr/bin/env python3
"""
Priprema slika za igru, poslije skidanja skriptom skini_sve_slike.py.

  python tools/obradi_slike.py

Pokreće se iz glavnog foldera igre. Za svaku kategoriju (mjesta, zivotinje, tijelo, instrumenti):
  - smanji sliku na najviše 800 px širine i sačuva je kao JPG (manje MB, brže na telefonu);
    providne PNG slike dobiju bijelu pozadinu, a animirani GIF prvu sličicu;
  - u podaci.json upiše novu putanju (.jpg);
  - dopiše koordinate mjestima kojima ih Wikipedia ne daje (KOORDINATE ispod);
  - označi loše slike (karta, satelitski snimak, logo…) poljem "iskljuceno", pa ih igra preskače.
    Oznaka važi samo za tu datoteku: kad u ZAMJENE upišeš bolju sliku i ponovo skineš,
    nova slika se automatski koristi.

Potrebno: Pillow (pip install pillow).
"""
import json
import os
import sys

from PIL import Image, ImageOps

SIRINA = 800
KVALITET = 76

# Mjesta bez koordinata na Wikipediji (lat, lon).
KOORDINATE = {
    "Grand Canal (Venice)": (45.4375, 12.3358),
    "Stari Most": (43.3373, 17.8150),
    "Walls of Dubrovnik": (42.6410, 18.1100),
    "Petra": (30.3285, 35.4444),
    "Carthage": (36.8528, 10.3233),
    "Mount Kilimanjaro": (-3.0674, 37.3556),
    "Las Vegas Strip": (36.1147, -115.1728),
    "Moai": (-27.1127, -109.3497),
    "Twelve Apostles (Victoria)": (-38.6662, 143.1051),
    "Stari Bar": (42.0937, 19.1361),
    "Đurđevića Tara Bridge": (43.1505, 19.2955),
    "Mehmed Paša Sokolović Bridge": (43.7825, 19.2880),
    "Pliva Waterfall": (44.3420, 17.2697),
    "Arslanagić Bridge": (42.7203, 18.3437),
    "Zagreb Cathedral": (45.8143, 15.9799),
}

# Slike koje ne valjaju za igru: ključ je ime datoteke na Commonsu (kraj polja "izvor").
# Oznaka važi samo za tu datoteku, pa zamjena (ZAMJENE u skini_sve_slike.py) radi automatski.
ISKLJUCI = {
    "File:2011_Dimos_Thiras.png": "karta umjesto fotografije",
    "File:Lake_Ohrid,_Macedonia-Albania_viewed_from_a_NASA_satellite.jpg": "satelitski snimak",
    "File:1957_map_of_Victoria_Harbour_by_the_British_War_Office.jpg": "stara karta umjesto fotografije",
    "File:Taipei_101_logo.svg": "logo umjesto fotografije",
    "File:Petronas_Towers_Logo.svg": "logo umjesto fotografije",
    "File:Baikal.A2001296.0420.250m-NASA.jpg": "satelitski snimak",
    "File:CN_Tower_logo.png": "logo umjesto fotografije",
    "File:Panama_Canal_Map_EN.svg": "karta umjesto fotografije",
    "File:Lake_Titicaca_ESA22522896.jpeg": "satelitski snimak",
    "File:ISS-45_StoryOfWater,_Great_Barrier_Reef,_Australia.jpg": "satelitski snimak",
    "File:Bora_Bora_ISS006.jpg": "satelitski snimak",
    "File:Gray945.png": "mikroskopski crtež, ne prepoznaje se",
    "File:Lion_Showing_off_His_Mouth_(21454262492).jpg": "na slici su usta lava",
    "File:VolRenderShearWarp.gif": "tamni CT snimak, ne prepoznaje se",
    "File:Tubular_bells_-_dusseldorf_germany_-_2014_12_27.webm": "na slici je hor, instrument se ne vidi",
}


def obradi_sliku(putanja):
    """Vrati novu putanju (.jpg) poslije smanjivanja."""
    nova = os.path.splitext(putanja)[0] + ".jpg"
    with Image.open(putanja) as im:
        im.seek(0)
        im = ImageOps.exif_transpose(im)
        if im.mode in ("P", "LA", "RGBA", "PA") or "transparency" in im.info:
            im = im.convert("RGBA")
            pozadina = Image.new("RGB", im.size, "white")
            pozadina.paste(im, mask=im.split()[-1])
            im = pozadina
        else:
            im = im.convert("RGB")
        if im.width > SIRINA:
            im = im.resize((SIRINA, round(im.height * SIRINA / im.width)), Image.LANCZOS)
        im.save(nova + ".tmp", "JPEG", quality=KVALITET, optimize=True, progressive=True)
    os.replace(nova + ".tmp", nova)
    return nova


def main():
    ukupno = 0
    for kat in ("mjesta", "zivotinje", "tijelo", "instrumenti"):
        pj = os.path.join(kat, "podaci.json")
        if not os.path.exists(pj):
            continue
        with open(pj, encoding="utf-8") as f:
            podaci = json.load(f)
        for s in podaci:
            stara = s["slika"]
            if not os.path.exists(stara):
                print("Nema slike:", stara)
                continue
            # već obrađena (JPG, ne šira od SIRINA) se ne dira
            with Image.open(stara) as im:
                gotova = stara.endswith(".jpg") and im.format == "JPEG" and im.width <= SIRINA
            if not gotova:
                s["slika"] = obradi_sliku(stara).replace(os.sep, "/")
                if os.path.normpath(s["slika"]) != os.path.normpath(stara):
                    os.remove(stara)  # original (PNG/GIF/JPEG) više ne treba
            if kat == "mjesta" and s.get("lat") is None and s.get("wikipedia") in KOORDINATE:
                s["lat"], s["lon"] = KOORDINATE[s["wikipedia"]]
            razlog = ISKLJUCI.get(s.get("izvor", "").rsplit("/", 1)[-1])
            if razlog:
                s["iskljuceno"] = razlog
            else:
                s.pop("iskljuceno", None)
            ukupno += 1
        with open(pj, "w", encoding="utf-8") as f:
            json.dump(podaci, f, ensure_ascii=False, indent=2)
        iskljucene = [s["naziv"] for s in podaci if s.get("iskljuceno")]
        print(f"{kat}: {len(podaci)} slika, isključeno {len(iskljucene)}: {', '.join(iskljucene) or '-'}")
    print(f"Gotovo ({ukupno} slika).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
