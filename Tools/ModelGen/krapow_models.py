# สร้างข้อมูลโมเดล low-poly พุ่มกะเพราแดง + ยอดที่เก็บได้ → Assets/Gameplay/Art/Models/KrapowModels.json
# M1KrapowPlantSetupEditor อ่าน JSON แล้วสร้าง Mesh ใน Unity · ใช้ตัวช่วยสร้าง Mesh จาก chili_models.py
# รัน: python3 Tools/ModelGen/krapow_models.py
import json, math, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from chili_models import Builder, sweep

rng = np.random.default_rng(3)
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Gameplay', 'Art', 'Models', 'KrapowModels.json')
COLORS = {'Stem': (0.45, 0.20, 0.32), 'Leaf': (0.15, 0.42, 0.13), 'Flower': (0.62, 0.25, 0.52), 'Soil': (0.36, 0.24, 0.15)}


def leaf(B, base, d, L, W):
    d = d / np.linalg.norm(d); s = np.cross(d, [0, 0, 1.0])
    s = s / np.linalg.norm(s) if np.linalg.norm(s) > 1e-6 else np.array([1.0, 0, 0])
    up = np.cross(s, d); dr = -0.2 * L
    pts = [base, base + d * L * 0.3 + s * W * 0.45, base + d * L * 0.6 + s * W * 0.5 + up * dr * 0.3, base + d * L + up * dr,
           base + d * L * 0.6 - s * W * 0.5 + up * dr * 0.3, base + d * L * 0.3 - s * W * 0.45, base + d * L * 0.5 + up * 0.003]
    for face_up in (up, -up):
        o = B.add_verts(pts)
        for a, b in ((0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 0)):
            B.tri('Leaf', o + a, o + b, o + 6, face_up)


def sprig(B, base, direction, length):
    # กิ่งกะเพรา: ก้านม่วง · ใบคู่ 5 ระดับ เล็กลงไปทางยอด · ช่อดอกม่วงที่ปลาย
    d = direction / np.linalg.norm(direction)
    pts = np.array([base + d * length * s + np.array([0, 0, 1.0]) * 0.02 * s * s for s in np.linspace(0, 1, 5)])
    sweep(B, 'Stem', pts, np.linspace(0.0025, 0.0012, 5), 5, cap_start=True)
    for j, s in enumerate([0.15, 0.35, 0.55, 0.75, 0.92]):
        i0 = min(int(s * 4), 3); fr = s * 4 - i0; p = pts[i0] * (1 - fr) + pts[i0 + 1] * fr
        a = math.atan2(d[1], d[0]) + j * 1.6
        for sd in (0, math.pi):
            leaf(B, p, np.array([math.cos(a + sd), math.sin(a + sd), 0.25]), 0.05 - 0.02 * s, 0.03 - 0.01 * s)
    tip = pts[-1]
    sweep(B, 'Flower', [tip + np.array([0, 0, z]) for z in np.linspace(0, 0.03, 6)], [0.004, 0.005, 0.0045, 0.004, 0.003, 0.001], 6)


def pack(B, names):
    U, subs = B.export()
    return {'vertices': [round(float(x), 6) for x in U.reshape(-1)],
            'submeshes': [{'name': n, 'color': list(COLORS[n]), 'triangles': subs[n]} for n in names]}


def build_bush():
    B = Builder(['Stem', 'Leaf', 'Flower', 'Soil'])
    for i in range(9):
        a = i * 2 * math.pi / 9 + rng.uniform(-0.2, 0.2)
        sprig(B, np.array([0, 0, 0.01]), np.array([math.cos(a) * 0.6, math.sin(a) * 0.6, 1.0]), rng.uniform(0.13, 0.19))
    sweep(B, 'Soil', [[0, 0, -0.005], [0, 0, 0.012]], [0.08, 0.075], 8, cap_start=True)
    # ช่องยอดที่เก็บได้ 3 ช่อง: เริ่มกลางพุ่ม ชี้ออกและขึ้น เว้นระหว่างกิ่งตกแต่ง
    slots = []
    for k in range(3):
        a = k * 2 * math.pi / 3 + math.pi / 9
        pos = np.array([math.cos(a) * 0.035, math.sin(a) * 0.035, 0.07])
        d = np.array([math.cos(a) * 0.45, math.sin(a) * 0.45, 1.0]); d /= np.linalg.norm(d)
        slots += [pos[0], pos[2], pos[1], d[0], d[2], d[1]]          # Unity (x, z, y)
    return pack(B, ['Stem', 'Leaf', 'Flower', 'Soil']), slots


def build_sprig():
    B = Builder(['Stem', 'Leaf', 'Flower'])
    sprig(B, np.zeros(3), np.array([0, 0, 1.0]), 0.11)
    return pack(B, ['Stem', 'Leaf', 'Flower'])


if __name__ == '__main__':
    bush, slots = build_bush()
    data = {'plant': bush, 'fruit': build_sprig(), 'slots': [round(x, 5) for x in slots]}
    with open(OUT, 'w') as f:
        json.dump(data, f, separators=(',', ':'))
    for key in ('plant', 'fruit'):
        print(key, 'tris', sum(len(s['triangles']) for s in data[key]['submeshes']) // 3)
    print('slots', len(slots) // 6, '->', os.path.normpath(OUT))
