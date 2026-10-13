#!/usr/bin/env python3
"""
Prebacuje podatke iz web verzije u Unity projekat (unity/Assets/TrkaZnanja/Resources/Data).

  python tools/pripremi_unity.py

Pokreće se iz glavnog foldera igre poslije svake izmjene pitanja, mapa ili zastava.
Pitanja ostaju na jednom mjestu (data/questions.json), a Unity dobija svoju kopiju.
Zastave su u web verziji WebP slike; Unity ne čita WebP, pa ih pretvaramo u PNG.

Potrebno: Pillow (pip install pillow).
"""
import base64
import io
import json
import os
import re
import shutil
import sys

from PIL import Image

KORIJEN = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CILJ = os.path.join(KORIJEN, "unity", "Assets", "TrkaZnanja", "Resources", "Data")


def kopiraj(izvor, odrediste):
    os.makedirs(os.path.dirname(odrediste), exist_ok=True)
    shutil.copyfile(izvor, odrediste)


def main():
    data = os.path.join(KORIJEN, "data")
    kopiraj(os.path.join(data, "questions.json"), os.path.join(CILJ, "pitanja.json"))
    kopiraj(os.path.join(data, "en-drzave.json"), os.path.join(CILJ, "en-drzave.json"))
    for ime in ("evropa", "svijet", "granice-cg", "granice-srb", "granice-balkan", "drzave", "kopno-svijet"):
        kopiraj(os.path.join(data, "maps", ime + ".json"), os.path.join(CILJ, "Mape", ime + ".json"))

    # zastave: data/flags.json (WebP base64) → Resources/Zastave/<kod>.png
    zastave = os.path.join(os.path.dirname(CILJ), "Zastave")
    os.makedirs(zastave, exist_ok=True)
    with open(os.path.join(data, "flags.json"), encoding="utf-8") as f:
        flags = json.load(f)
    for kod, uri in flags.items():
        b64 = uri.split(",", 1)[1]
        with Image.open(io.BytesIO(base64.b64decode(b64))) as im:
            im.convert("RGBA").save(os.path.join(zastave, kod + ".png"), optimize=True)
    # istorijska zastava SFRJ (nema je u flag-icons), crtamo je kao u web verziji
    sfrj = Image.new("RGBA", (300, 200), "#ffffff")
    from PIL import ImageDraw
    import math
    d = ImageDraw.Draw(sfrj)
    d.rectangle([0, 0, 300, 67], fill="#003893")
    d.rectangle([0, 133, 300, 200], fill="#de0000")

    def zvijezda(cx, cy, r, boja):
        pts = []
        for i in range(10):
            a = -math.pi / 2 + i * math.pi / 5
            rr = r * 0.42 if i % 2 else r
            pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
        d.polygon(pts, fill=boja)

    zvijezda(150, 100, 46, "#fcd116")
    zvijezda(150, 100, 39, "#de0000")
    sfrj.save(os.path.join(zastave, "sfrj.png"), optimize=True)

    # države (kod, naziv, glavni grad, kontinent) iz js/countries.js → drzave-lista.json
    drzave = []
    with open(os.path.join(KORIJEN, "js", "countries.js"), encoding="utf-8") as f:
        for red in f:
            m = re.match(r"\s*\['(\w+)','([^']*)',(null|'[^']*'),'([^']*)'", red)
            if m:
                drzave.append({"kod": m.group(1), "naziv": m.group(2),
                               "grad": None if m.group(3) == "null" else m.group(3).strip("'"), "kontinent": m.group(4)})
    with open(os.path.join(CILJ, "drzave-lista.json"), "w", encoding="utf-8") as f:
        json.dump(drzave, f, ensure_ascii=False, indent=1)
    print(f"Pitanja, 7 mapa, {len(drzave)} država i {len(flags)} zastava prebačeno u Unity.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
