#!/usr/bin/env python3
"""
Pregled 3D modela bez Unity-ja: čita OBJ (sa bojama vrhova) i crta ih kao Unity
(lijeva koordinatna ruka, prednja strana trougla = cross(p1-p0, p2-p0) prema posmatraču).
Ako je neki trougao okrenut naopako, vidi se rupa: tako se provjerava i redoslijed vrhova.

  python tools/pregled_modela.py <folder sa .obj> <izlaz.png> [filter]
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFont


def load(path):
    v, c, f = [], [], []
    for line in open(path):
        p = line.split()
        if not p:
            continue
        if p[0] == "v":
            v.append(tuple(map(float, p[1:4])))
            c.append(tuple(map(float, p[4:7])) if len(p) >= 7 else (0.8, 0.8, 0.8))
        elif p[0] == "f":
            f.append(tuple(int(x) - 1 for x in p[1:4]))
    return v, c, f


def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def norm(a):
    m = math.sqrt(dot(a, a)) or 1
    return (a[0] / m, a[1] / m, a[2] / m)


def render(v, c, f, size=360, yaw=35, pitch=22):
    xs = [p[0] for p in v]; ys = [p[1] for p in v]; zs = [p[2] for p in v]
    center = ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (min(zs) + max(zs)) / 2)
    ext = max(max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)) or 1
    dist = ext * 2.2
    ya, pa = math.radians(yaw), math.radians(pitch)
    eye = (center[0] + dist * math.sin(ya) * math.cos(pa), center[1] + dist * math.sin(pa), center[2] + dist * math.cos(ya) * math.cos(pa))
    fwd = norm(sub(center, eye))
    right = norm(cross((0, 1, 0), fwd))     # Unity: right = up × forward
    up = cross(fwd, right)
    light = norm((0.4, 0.8, 0.5))
    img = Image.new("RGB", (size, size), (233, 241, 247))
    d = ImageDraw.Draw(img)
    tris = []
    scale = size / (ext * 1.25)
    for (a, b, e) in f:
        p0, p1, p2 = v[a], v[b], v[e]
        n = cross(sub(p1, p0), sub(p2, p0))
        if dot(n, n) < 1e-14:
            continue
        nn = norm(n)
        mid = ((p0[0] + p1[0] + p2[0]) / 3, (p0[1] + p1[1] + p2[1]) / 3, (p0[2] + p1[2] + p2[2]) / 3)
        if dot(nn, sub(eye, mid)) <= 0:      # zadnja strana: Unity je ne crta
            continue
        depth = dot(sub(mid, eye), fwd)
        col = [(c[a][i] + c[b][i] + c[e][i]) / 3 for i in range(3)]
        k = 0.45 + 0.6 * max(0, dot(nn, light))
        pts = []
        for p in (p0, p1, p2):
            q = sub(p, eye)
            x, y, z = dot(q, right), dot(q, up), dot(q, fwd)
            pts.append((size / 2 + x * scale * dist / z, size / 2 - y * scale * dist / z))
        tris.append((depth, pts, tuple(min(255, int(255 * ch * k)) for ch in col)))
    tris.sort(key=lambda t: -t[0])
    for _, pts, col in tris:
        d.polygon(pts, fill=col)
    return img


def main():
    folder, out = sys.argv[1], sys.argv[2]
    filt = sys.argv[3] if len(sys.argv) > 3 else ""
    files = sorted(x for x in os.listdir(folder) if x.endswith(".obj") and filt in x)
    cols = 4
    size = 360
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * size, rows * (size + 24)), "white")
    d = ImageDraw.Draw(sheet)
    for i, name in enumerate(files):
        v, c, f = load(os.path.join(folder, name))
        img = render(v, c, f, size)
        x, y = (i % cols) * size, (i // cols) * (size + 24)
        sheet.paste(img, (x, y + 24))
        d.text((x + 6, y + 6), name.replace(".obj", ""), fill=(20, 35, 46))
    sheet.save(out)


if __name__ == "__main__":
    main()
