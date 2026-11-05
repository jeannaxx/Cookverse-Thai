# สร้างข้อมูลโมเดลแปลงผักยกพื้นขอบไม้ → Assets/Gameplay/Art/Models/GardenBedModels.json
# M1GardenBedSetupEditor อ่าน JSON แล้วสร้าง Mesh ใน Unity · ใช้ตัวช่วยจาก chili_models.py
# แปลง 0.8 × 1.2 ม. (ขนาดเดิมของกล่องแปลงในฉาก) · จุดกำเนิด = กลางแปลงที่ระดับพื้นสวน · Unity Y ขึ้น
# 4 แบบตามจำนวนสันดิน: mound (โคกกลาง), ridge1, ridge2, ridge3 · สันดินวิ่งตามแกน Z (แนวแถวต้น)
# + planter: กระบะยาว 0.45 × 2.4 ม. สำหรับค้างถั่วฝักยาว/มะเขือเทศ
# รัน: python3 Tools/ModelGen/garden_bed_models.py
import json, math, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from chili_models import Builder

rng = np.random.default_rng(11)
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Gameplay', 'Art', 'Models', 'GardenBedModels.json')
COLORS = {'Soil': (0.30, 0.20, 0.13), 'Plank': (0.62, 0.45, 0.28), 'Post': (0.48, 0.33, 0.20)}

W, L = 0.8, 1.2            # กว้าง (X) · ยาว (Z)
WALL_H, WALL_T = 0.18, 0.03
POST = 0.05                # หลักมุม 5×5 ซม. สูงพ้นขอบ 3 ซม.
SOIL_BASE = 0.13           # ระดับดินในร่อง (ต่ำกว่าขอบ 5 ซม.)
RIDGE_H = 0.035            # สันดินสูงกว่าร่อง
NX, NZ = 17, 25


def box(B, sub, center, size):
    # กล่อง (พิกัดสคริปต์ Z ขึ้น) 8 จุด 12 สามเหลี่ยม หันผิวออกนอก
    c = np.asarray(center, float); h = np.asarray(size, float) / 2
    pts = [c + h * np.array([sx, sy, sz]) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
    o = B.add_verts(pts)
    faces = [(0, 1, 3, 2, [-1, 0, 0]), (4, 6, 7, 5, [1, 0, 0]), (0, 4, 5, 1, [0, -1, 0]),
             (2, 3, 7, 6, [0, 1, 0]), (0, 2, 6, 4, [0, 0, -1]), (1, 5, 7, 3, [0, 0, 1])]
    for a, b, cc, d, n in faces:
        B.tri(sub, o + a, o + b, o + cc, n); B.tri(sub, o + a, o + cc, o + d, n)


def ridge_centers(kind):
    inner = W / 2 - WALL_T
    return {'mound': [], 'ridge1': [0.0], 'ridge2': [-0.2, 0.2], 'ridge3': [-0.24, 0.0, 0.24]}[kind], inner


def soil_height(x, z, kind):
    ridges, inner = ridge_centers(kind)
    if kind == 'mound':
        r = math.hypot(x / (W * 0.42), z / (L * 0.42))
        bump = max(0.0, math.cos(min(r, 1.0) * math.pi / 2)) * RIDGE_H * 1.4
    else:
        bump = max(max(0.0, math.cos(min(abs(x - rc) / 0.13, 1.0) * math.pi / 2)) for rc in ridges) * RIDGE_H
        # ปลายสันลาดลงใกล้ขอบหัวท้ายแปลง
        bump *= min(1.0, (L / 2 - WALL_T - abs(z)) / 0.08)
    return SOIL_BASE + bump


def build(kind):
    B = Builder(['Soil', 'Plank', 'Post'])
    ix = W / 2 - WALL_T; iz = L / 2 - WALL_T
    xs = np.linspace(-ix, ix, NX); zs = np.linspace(-iz, iz, NZ)
    grid = []
    for zi, z in enumerate(zs):
        for xi, x in enumerate(xs):
            edge = xi in (0, NX - 1) or zi in (0, NZ - 1)
            y = soil_height(x, z, kind) + (0 if edge else rng.uniform(-0.004, 0.004))
            grid.append([x, z, y])          # พิกัดสคริปต์: (x, z_แปลง→y_สคริปต์, ความสูง→z_สคริปต์)
    o = B.add_verts(grid)
    for zi in range(NZ - 1):
        for xi in range(NX - 1):
            a = o + zi * NX + xi; b = a + 1; c = a + NX + 1; d = a + NX
            B.tri('Soil', a, b, c, [0, 0, 1]); B.tri('Soil', a, c, d, [0, 0, 1])
    # ผนังข้างดิน (เติมช่องระหว่างผิวดินกับพื้น) ไม่จำเป็น เพราะขอบไม้บังไว้
    # ขอบไม้ 4 ด้าน
    box(B, 'Plank', [0, -L / 2 + WALL_T / 2, WALL_H / 2], [W, WALL_T, WALL_H])
    box(B, 'Plank', [0, L / 2 - WALL_T / 2, WALL_H / 2], [W, WALL_T, WALL_H])
    box(B, 'Plank', [-W / 2 + WALL_T / 2, 0, WALL_H / 2], [WALL_T, L - 2 * WALL_T, WALL_H])
    box(B, 'Plank', [W / 2 - WALL_T / 2, 0, WALL_H / 2], [WALL_T, L - 2 * WALL_T, WALL_H])
    # หลักมุม
    for sx in (-1, 1):
        for sz in (-1, 1):
            box(B, 'Post', [sx * (W / 2 - POST / 2 + 0.008), sz * (L / 2 - POST / 2 + 0.008), (WALL_H + 0.03) / 2],
                [POST, POST, WALL_H + 0.03])
    U, subs = B.export()
    return {'vertices': [round(float(v), 5) for v in U.reshape(-1)],
            'submeshes': [{'name': n, 'color': list(COLORS[n]), 'triangles': subs[n]} for n in ('Soil', 'Plank', 'Post')]}


if __name__ == '__main__':
    data = {kind: build(kind) for kind in ('mound', 'ridge1', 'ridge2', 'ridge3')}
    # กระบะยาวสำหรับค้างถั่วฝักยาว + มะเขือเทศ 0.45 × 2.4 ม. สันเดียว (ใช้ตัวแปรกลางชุดเดียวกัน)
    W, L, NX, NZ = 0.45, 2.4, 9, 41
    data['planter'] = build('ridge1')
    data['soilBase'] = SOIL_BASE; data['ridgeHeight'] = RIDGE_H
    with open(OUT, 'w') as f:
        json.dump(data, f, separators=(',', ':'))
    for k in ('mound', 'ridge1', 'ridge2', 'ridge3', 'planter'):
        print(k, 'tris', sum(len(s['triangles']) for s in data[k]['submeshes']) // 3)
    print('->', os.path.normpath(OUT))
