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

    # šah (mat u 1)
    kopiraj(os.path.join(data, "sah.json"), os.path.join(CILJ, "sah.json"))

    # fotografije za mini igre: <kategorija>/podaci.json + slike → Resources/Slike/<kategorija>/<id>.jpg
    # (smanjene na najviše 512 px, bez isključenih), a spisak u Resources/Data/slike-<kategorija>.json
    slike_cilj = os.path.join(os.path.dirname(CILJ), "Slike")
    ukupno = 0
    for kat in ("mjesta", "zivotinje", "tijelo", "instrumenti"):
        put = os.path.join(KORIJEN, kat, "podaci.json")
        if not os.path.exists(put):
            continue
        with open(put, encoding="utf-8") as f:
            stavke = json.load(f)
        izlaz = []
        folder = os.path.join(slike_cilj, kat)
        os.makedirs(folder, exist_ok=True)
        for x in stavke:
            if x.get("iskljuceno") or not x.get("slika") or not x.get("naziv"):
                continue
            izvor = os.path.join(KORIJEN, x["slika"])
            if not os.path.exists(izvor):
                continue
            ime = x["id"]
            odr = os.path.join(folder, ime + ".jpg")
            if not os.path.exists(odr) or os.path.getmtime(odr) < os.path.getmtime(izvor):
                with Image.open(izvor) as im:
                    im = im.convert("RGB")
                    im.thumbnail((512, 512), Image.LANCZOS)
                    im.save(odr, "JPEG", quality=72, optimize=True, progressive=False)
            y = {k: v for k, v in x.items() if k not in ("slika", "licenca_url", "izvor", "iskljuceno")}
            y["slika"] = f"Slike/{kat}/{ime}"
            izlaz.append(y)
        with open(os.path.join(CILJ, f"slike-{kat}.json"), "w", encoding="utf-8") as f:
            json.dump(izlaz, f, ensure_ascii=False, indent=1)
        ukupno += len(izlaz)

    # zvukovi instrumenata (FluidR3 GM, CC BY 3.0): 4 note po instrumentu → Resources/Zvuk/Instrumenti/<folder>/<nota>.mp3
    with open(os.path.join(KORIJEN, "js", "minigames", "recognize.js"), encoding="utf-8") as f:
        src = f.read()
    blok = src[src.index("export const INSTRUMENTS"):src.index("].map(([name, folder")]
    instrumenti = [{"naziv": m.group(1), "folder": m.group(2)}
                   for m in re.finditer(r"\['([^']+)', '([a-z_]+)', '[^']+'", blok)]
    for ins in instrumenti:
        for nota in ("C4", "E4", "G4", "C5"):
            izvor = os.path.join(data, "zvuk", ins["folder"], nota + ".mp3")
            if os.path.exists(izvor):
                kopiraj(izvor, os.path.join(os.path.dirname(CILJ), "Zvuk", "Instrumenti", ins["folder"], nota + ".mp3"))
    with open(os.path.join(CILJ, "instrumenti-zvuk.json"), "w", encoding="utf-8") as f:
        json.dump(instrumenti, f, ensure_ascii=False, indent=1)

    # veliki gradovi i rijeke svijeta (iz js/world.js, preko Node-a)
    import subprocess
    skripta = ("import('./js/world.js').then(m => console.log(JSON.stringify({ gradovi: m.CITIES, rijeke: m.RIVERS })))")
    try:
        izlaz = subprocess.run(["node", "--input-type=module", "-e", skripta], cwd=KORIJEN, capture_output=True, text=True, check=True).stdout
        svijet = json.loads(izlaz)
        with open(os.path.join(CILJ, "svijet-gradovi-rijeke.json"), "w", encoding="utf-8") as f:
            json.dump(svijet, f, ensure_ascii=False)
        print(f"{len(svijet['gradovi'])} gradova i {len(svijet['rijeke'])} rijeka.")
    except (OSError, subprocess.CalledProcessError) as e:
        print("Upozorenje: gradovi i rijeke nisu prebačeni (treba Node.js):", e)

    print(f"{ukupno} fotografija, {len(instrumenti)} instrumenata sa zvukom i 45 šahovskih problema prebačeno.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
