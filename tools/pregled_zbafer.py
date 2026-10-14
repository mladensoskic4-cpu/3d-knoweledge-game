#!/usr/bin/env python3
"""Pregled OBJ modela sa z-baferom (tačno i za tanke ukrase zalijepljene na površinu).
  python tools/pregled_zbafer.py <folder> <izlaz.png> [filter] [yaw] [pitch]
Prednja strana trougla kao u Unity-ju: cross(p1-p0, p2-p0) gleda prema posmatraču."""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw

def load(path):
    v, c, f = [], [], []
    for line in open(path):
        p = line.split()
        if not p: continue
        if p[0] == 'v':
            v.append(list(map(float, p[1:4]))); c.append(list(map(float, p[4:7])) if len(p) >= 7 else [.8,.8,.8])
        elif p[0] == 'f':
            f.append([int(x) - 1 for x in p[1:4]])
    return np.array(v), np.array(c), np.array(f)

def render(v, c, f, size=420, yaw=55, pitch=24):
    lo, hi = v.min(0), v.max(0); center = (lo + hi) / 2; ext = (hi - lo).max() or 1
    dist = ext * 2.4
    ya, pa = math.radians(yaw), math.radians(pitch)
    eye = center + dist * np.array([math.sin(ya)*math.cos(pa), math.sin(pa), math.cos(ya)*math.cos(pa)])
    fwd = center - eye; fwd /= np.linalg.norm(fwd)
    right = np.cross([0,1,0], fwd); right /= np.linalg.norm(right)
    up = np.cross(fwd, right)
    light = np.array([0.4, 0.8, 0.5]); light /= np.linalg.norm(light)
    img = np.full((size, size, 3), [233, 241, 247], np.float32)
    zb = np.full((size, size), np.inf)
    q = v - eye
    X, Y, Z = q @ right, q @ up, q @ fwd
    scale = size / (ext * 1.2)
    sx = size/2 + X * scale * dist / Z; sy = size/2 - Y * scale * dist / Z
    for a, b, e in f:
        p0, p1, p2 = v[a], v[b], v[e]
        n = np.cross(p1 - p0, p2 - p0); nl = np.linalg.norm(n)
        if nl < 1e-14: continue
        n /= nl
        mid = (p0 + p1 + p2) / 3
        if n @ (eye - mid) <= 0: continue
        col = (c[a] + c[b] + c[e]) / 3 * (0.45 + 0.6 * max(0, n @ light)) * 255
        xs = np.array([sx[a], sx[b], sx[e]]); ys = np.array([sy[a], sy[b], sy[e]]); zs = np.array([Z[a], Z[b], Z[e]])
        x0, x1 = int(max(0, np.floor(xs.min()))), int(min(size - 1, np.ceil(xs.max())))
        y0, y1 = int(max(0, np.floor(ys.min()))), int(min(size - 1, np.ceil(ys.max())))
        if x1 < x0 or y1 < y0: continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        d = (ys[1]-ys[2])*(xs[0]-xs[2]) + (xs[2]-xs[1])*(ys[0]-ys[2])
        if abs(d) < 1e-9: continue
        w0 = ((ys[1]-ys[2])*(gx-xs[2]) + (xs[2]-xs[1])*(gy-ys[2])) / d
        w1 = ((ys[2]-ys[0])*(gx-xs[2]) + (xs[0]-xs[2])*(gy-ys[2])) / d
        w2 = 1 - w0 - w1
        m = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
        if not m.any(): continue
        z = w0*zs[0] + w1*zs[1] + w2*zs[2]
        sub = zb[y0:y1+1, x0:x1+1]
        upd = m & (z < sub)
        sub[upd] = z[upd]
        img[y0:y1+1, x0:x1+1][upd] = np.clip(col, 0, 255)
    return Image.fromarray(img.astype(np.uint8))

def main():
    folder, out = sys.argv[1], sys.argv[2]
    filt = sys.argv[3] if len(sys.argv) > 3 else ''
    yaw = float(sys.argv[4]) if len(sys.argv) > 4 else 55
    pitch = float(sys.argv[5]) if len(sys.argv) > 5 else 24
    files = sorted(x for x in os.listdir(folder) if x.endswith('.obj') and filt in x)
    cols, size = 4, 420
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * size, rows * (size + 24)), 'white')
    dr = ImageDraw.Draw(sheet)
    for i, name in enumerate(files):
        v, c, f = load(os.path.join(folder, name))
        im = render(v, c, f, size, yaw, pitch)
        x, y = (i % cols) * size, (i // cols) * (size + 24)
        sheet.paste(im, (x, y + 24)); dr.text((x + 6, y + 6), name[:-4], fill=(20, 35, 46))
    sheet.save(out)

if __name__ == '__main__':
    main()
