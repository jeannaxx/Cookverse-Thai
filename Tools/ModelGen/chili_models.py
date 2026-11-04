# สร้างข้อมูลโมเดล low-poly ของต้นพริกและลูกพริก → Assets/Gameplay/Art/Models/ChiliModels.json
# M1ChiliPlantSetupEditor อ่าน JSON นี้แล้วสร้าง Mesh ใน Unity (ไม่ผ่านตัวนำเข้า OBJ)
# รัน: python3 Tools/ModelGen/chili_models.py  (ต้องมี numpy) · seed คงที่ ผลเหมือนเดิมทุกครั้ง
# พิกัดในสคริปต์: Z ขึ้น หน่วยเมตร · แปลงเป็น Unity (x, z, y) และหันผิวให้ cross(b-a, c-a) ชี้ออกนอก
import json, math, os
import numpy as np

rng = np.random.default_rng(7)
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Gameplay', 'Art', 'Models', 'ChiliModels.json')
COLORS = {'Stem': (0.33, 0.50, 0.20), 'Leaf': (0.16, 0.45, 0.14), 'Fruit': (0.82, 0.09, 0.06),
          'Calyx': (0.25, 0.52, 0.17), 'Soil': (0.36, 0.24, 0.15)}


class Builder:
    def __init__(self, names):
        self.v = []
        self.subs = {n: [] for n in names}   # (a, b, c, outward_ref or None)

    def add_verts(self, pts):
        base = len(self.v)
        self.v.extend([np.asarray(p, float) for p in pts])
        return base

    def tri(self, sub, a, b, c, ref):
        self.subs[sub].append((a, b, c, ref))

    def export(self):
        V = np.array(self.v)
        U = V[:, [0, 2, 1]]                      # Z-up → Unity Y-up
        out = {}
        for name, faces in self.subs.items():
            tris = []
            for a, b, c, ref in faces:
                if ref is not None:
                    r = np.asarray(ref, float)[[0, 2, 1]]
                    n = np.cross(U[b] - U[a], U[c] - U[a])
                    if np.dot(n, r) < 0:
                        b, c = c, b
                tris += [int(a), int(b), int(c)]
            out[name] = tris
        return U, out


def basis(t):
    up = np.array([0, 0, 1.0]) if abs(t[2]) < 0.95 else np.array([1.0, 0, 0])
    n = np.cross(t, up); n /= np.linalg.norm(n)
    return n, np.cross(t, n)


def sweep(B, sub, path, radii, sides, cap_start=False, cap_end=True):
    path = np.asarray(path, float); R = len(path); rings = []
    for i, (p, r) in enumerate(zip(path, radii)):
        t = path[min(i + 1, R - 1)] - path[max(i - 1, 0)]; t /= np.linalg.norm(t)
        n, b = basis(t)
        rings.append([p + r * (math.cos(2 * math.pi * k / sides) * n + math.sin(2 * math.pi * k / sides) * b) for k in range(sides)])
    base = B.add_verts([q for ring in rings for q in ring])
    for i in range(R - 1):
        for k in range(sides):
            a = base + i * sides + k; b = base + i * sides + (k + 1) % sides
            c = base + (i + 1) * sides + (k + 1) % sides; d = base + (i + 1) * sides + k
            mid = (B.v[a] + B.v[c]) / 2; ref = mid - (path[i] + path[i + 1]) / 2
            B.tri(sub, a, b, c, ref); B.tri(sub, a, c, d, ref)
    for end, flag in ((0, cap_start), (R - 1, cap_end)):
        if not flag:
            continue
        t = path[1] - path[0] if end == 0 else path[-1] - path[-2]
        ref = -t if end == 0 else t
        ci = B.add_verts([path[end]]); ring0 = base + end * sides
        for k in range(sides):
            B.tri(sub, ci, ring0 + k, ring0 + (k + 1) % sides, ref)


def chili_fruit(B, attach, tilt, length, sides=7):
    # ลูกพริกห้อยจากจุด attach: ก้านสั้น → กลีบเลี้ยง → ตัวพริกปลายแหลม
    tilt = np.asarray(tilt, float); tilt /= np.linalg.norm(tilt)
    sweep(B, 'Stem', [attach + np.array([0, 0, 0.006]), attach - tilt * 0.002], [0.0011, 0.0011], 5, cap_start=True)
    sweep(B, 'Calyx', [attach - tilt * 0.001, attach - tilt * 0.007], [0.0036, 0.005], sides, cap_start=True, cap_end=False)
    M = 9; s = np.linspace(0, 1, M)
    side = np.cross(tilt, [0, 0, 1.0]); side = side / np.linalg.norm(side) if np.linalg.norm(side) > 1e-6 else np.array([1.0, 0, 0])
    path = np.array([attach - tilt * (0.004 + length * si) + side * 0.004 * math.sin(si * math.pi) for si in s])
    r = 0.0042 * np.clip(np.sin(np.minimum(s * 1.6 + 0.35, 1.0) * math.pi / 2), 0, 1) * (1 - s ** 2.2) + 0.0002
    sweep(B, 'Fruit', path, r, sides)


def leaf(B, base, direction, length, width):
    d = direction / np.linalg.norm(direction); side = np.cross(d, [0, 0, 1.0])
    side = side / np.linalg.norm(side) if np.linalg.norm(side) > 1e-6 else np.array([1.0, 0, 0])
    up = np.cross(side, d); droop = -0.25 * length
    pts = [base, base + d * length * 0.35 + side * width * 0.5 + up * 0.01,
           base + d * length * 0.7 + side * width * 0.35 + up * droop * 0.4, base + d * length + up * droop,
           base + d * length * 0.7 - side * width * 0.35 + up * droop * 0.4,
           base + d * length * 0.35 - side * width * 0.5 + up * 0.01, base + d * length * 0.5 + up * 0.004]
    for face_up in (up, -up):                   # สองหน้า แยกจุด เพื่อให้ normal ไม่หักล้างกัน
        o = B.add_verts(pts)
        for a, b in ((0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 0)):
            B.tri('Leaf', o + a, o + b, o + 6, face_up)


def curve(p0, d, length, bend, n):
    d = d / np.linalg.norm(d)
    return np.array([p0 + d * length * s + np.array([0, 0, 1.0]) * bend * s * s for s in np.linspace(0, 1, n)])


def build_plant():
    B = Builder(['Stem', 'Leaf', 'Fruit', 'Calyx', 'Soil'])
    trunk = curve(np.zeros(3), np.array([0.02, 0, 1.0]), 0.22, 0.0, 6)
    sweep(B, 'Stem', trunk, np.linspace(0.006, 0.0035, 6), 6)
    attaches = []
    for i, h in enumerate([0.09, 0.12, 0.15, 0.18, 0.21]):
        base = np.array([0.02 * h / 0.22, 0, h]); ang = i * 2.4 + 0.3
        br = curve(base, np.array([math.cos(ang), math.sin(ang), 0.9 - 0.12 * i]), 0.16 - 0.018 * i, 0.03, 5)
        sweep(B, 'Stem', br, np.linspace(0.0035, 0.0018, 5), 5)
        for j, s in enumerate([0.35, 0.6, 0.85, 1.0]):
            p = br[min(int(s * 4), 4)]
            for sd in (-1, 1):
                la = ang + sd * rng.uniform(0.8, 1.3)
                leaf(B, p, np.array([math.cos(la), math.sin(la), rng.uniform(-0.1, 0.4)]), rng.uniform(0.05, 0.07), rng.uniform(0.022, 0.028))
            if j in (1, 3):
                attaches.append(p.copy())
    for k in range(4):
        a = k * math.pi / 2 + 0.4
        leaf(B, trunk[-1], np.array([math.cos(a), math.sin(a), 0.6]), 0.05, 0.022)
    sweep(B, 'Soil', [[0, 0, -0.005], [0, 0, 0.012]], [0.05, 0.045], 8, cap_start=True)

    # ลูกพริกทุกลูกเป็นชิ้นที่เก็บได้ (สร้างใน Unity) ต้นจึงไม่มีลูกตกแต่ง
    # เรียงช่องห้อยจากเอื้อมง่ายสุด (อยู่นอก สูงกลาง ๆ) · Unity ใช้ N ช่องแรก
    order = sorted(attaches, key=lambda a: -(math.hypot(a[0], a[1]) - abs(a[2] - 0.17)))
    return B, [[float(a[0]), float(a[2]), float(a[1])] for a in order]


def build_fruit():
    B = Builder(['Fruit', 'Calyx', 'Stem'])
    chili_fruit(B, np.zeros(3), [0.12, 0.0, 1.0], 0.045, sides=8)
    return B


def pack(B):
    U, subs = B.export()
    return {'vertices': [round(float(x), 6) for x in U.reshape(-1)],
            'submeshes': [{'name': n, 'color': list(COLORS[n]), 'triangles': t} for n, t in subs.items()]}


if __name__ == '__main__':
    plant, slots = build_plant()
    data = {'plant': pack(plant), 'fruit': pack(build_fruit()), 'plantFruitSlots': [c for sl in slots for c in sl]}
    with open(OUT, 'w') as f:
        json.dump(data, f, separators=(',', ':'))
    for key in ('plant', 'fruit'):
        print(key, 'verts', len(data[key]['vertices']) // 3, 'tris', sum(len(s['triangles']) for s in data[key]['submeshes']) // 3)
    print('slots', len(slots), '->', os.path.normpath(OUT))
