#!/usr/bin/env python3
"""
Pregled cijele scene (OBJ iz unity-compile-check --scena) iz ugla kamere igre, bez Unity-ja.
Pravi z-bafer (kao grafička kartica), sunce, ambijent i magla. Služi za provjeru kompozicije i razmjera.

  python tools/pregled_scene.py scena.obj izlaz.png ex,ey,ez tx,ty,tz [fov]
"""
import math
import sys

import numpy as np
from PIL import Image


def main():
    path, out = sys.argv[1], sys.argv[2]
    eye = np.array(list(map(float, sys.argv[3].split(","))))
    target = np.array(list(map(float, sys.argv[4].split(","))))
    fov = float(sys.argv[5]) if len(sys.argv) > 5 else 46
    W, H = 1280, 720
    vs, cs, fs = [], [], []
    for line in open(path):
        if line.startswith("v "):
            p = line.split()
            vs.append([float(x) for x in p[1:4]])
            cs.append([float(x) for x in p[4:7]])
        elif line.startswith("f "):
            p = line.split()
            fs.append([int(p[1]) - 1, int(p[2]) - 1, int(p[3]) - 1])
    V = np.array(vs); C = np.array(cs); F = np.array(fs)

    f = target - eye; f /= np.linalg.norm(f)
    r = np.cross([0, 1, 0], f); r /= np.linalg.norm(r)
    u = np.cross(f, r)
    k = 1 / math.tan(math.radians(fov) / 2)
    Q = V - eye
    X, Y, Z = Q @ r, Q @ u, Q @ f
    SX = W / 2 + X * k / np.maximum(Z, 1e-3) * H / 2
    SY = H / 2 - Y * k / np.maximum(Z, 1e-3) * H / 2

    p0, p1, p2 = V[F[:, 0]], V[F[:, 1]], V[F[:, 2]]
    n = np.cross(p1 - p0, p2 - p0)
    nl = np.linalg.norm(n, axis=1) + 1e-12
    nn = n / nl[:, None]
    mid = (p0 + p1 + p2) / 3
    front = np.einsum("ij,ij->i", n, eye - mid) > 0          # Unity: prednja strana prema kameri
    zmin = np.minimum(np.minimum(Z[F[:, 0]], Z[F[:, 1]]), Z[F[:, 2]])
    keep = front & (zmin > 0.1) & (nl > 1e-10)
    sun = np.array([-0.45, 0.75, 0.3]); sun /= np.linalg.norm(sun)
    lit = 0.42 + 0.75 * np.clip((nn @ sun + 0.45) / 1.45, 0, 1)
    col = (C[F[:, 0]] + C[F[:, 1]] + C[F[:, 2]]) / 3 * lit[:, None]

    zbuf = np.full((H, W), np.inf)
    img = np.zeros((H, W, 3))
    t = np.linspace(0, 1, H)[:, None]
    sky = np.array([61, 142, 232]) / 255 + (np.array([191, 230, 255]) / 255 - np.array([61, 142, 232]) / 255) * np.minimum(1, t * 1.6)
    img[:] = sky[:, None, :]
    fog = np.array([207, 233, 251]) / 255
    idx = np.nonzero(keep)[0]
    for i in idx:
        a, b, c = F[i]
        xs = np.array([SX[a], SX[b], SX[c]]); ys = np.array([SY[a], SY[b], SY[c]]); zs = np.array([Z[a], Z[b], Z[c]])
        x0, x1 = int(max(0, math.floor(xs.min()))), int(min(W - 1, math.ceil(xs.max())))
        y0, y1 = int(max(0, math.floor(ys.min()))), int(min(H - 1, math.ceil(ys.max())))
        if x0 > x1 or y0 > y1:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        d = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
        if abs(d) < 1e-9:
            continue
        w0 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / d
        w1 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / d
        w2 = 1 - w0 - w1
        inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
        if not inside.any():
            continue
        # perspektivno ispravna dubina
        iz = w0 / zs[0] + w1 / zs[1] + w2 / zs[2]
        z = 1 / np.maximum(iz, 1e-9)
        sub = zbuf[y0:y1 + 1, x0:x1 + 1]
        m = inside & (z < sub)
        if not m.any():
            continue
        sub[m] = z[m]
        fk = np.clip((z[m] - 45) / (150 - 45), 0, 1)[:, None]
        img[y0:y1 + 1, x0:x1 + 1][m] = np.clip(col[i], 0, 1) * (1 - fk) + fog * fk
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(out)
    print(f"{len(idx)} trouglova")


if __name__ == "__main__":
    main()
