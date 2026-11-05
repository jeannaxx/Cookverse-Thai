# สร้างข้อมูลโมเดล low-poly ผักตกแต่งในสวน → Assets/Gameplay/Art/Models/VeggieModels.json
# ต้นหอม · ผักชี · ตะไคร้ · มะนาว · ข่า · หอมแดง · โหระพา · สะระแหน่ · มะเขือเปราะ · มะเขือพวง · กุยช่าย
# ผักชีฝรั่ง · มะกรูด · มะละกอ · มะม่วง · ถั่วฝักยาว (ค้าง) · มะเขือเทศ
# ต้น = ของตกแต่ง · ชิ้นที่เก็บได้ (ลูก/ยอด/ใบ/เหง้า) แยกไว้ใน VeggiePicks.json พร้อมช่องวางบนต้น
# M1VeggieSetupEditor อ่าน JSON ทั้งสองไฟล์แล้วสร้าง Mesh + ชิ้นที่เก็บได้ใน Unity
# รัน: python3 Tools/ModelGen/veggie_models.py
import json, math, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from chili_models import Builder, sweep

rng = np.random.default_rng(21)
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Gameplay', 'Art', 'Models', 'VeggieModels.json')
COLORS = {
    'Leaf': (0.20, 0.50, 0.16), 'LightLeaf': (0.42, 0.62, 0.25), 'Pale': (0.86, 0.88, 0.72),
    'Stem': (0.30, 0.55, 0.20), 'Bark': (0.38, 0.27, 0.18), 'Fruit': (0.38, 0.66, 0.16), 'Soil': (0.33, 0.22, 0.14),
    'PinkBase': (0.78, 0.55, 0.50), 'Bulb': (0.58, 0.22, 0.30), 'PurpleStem': (0.40, 0.18, 0.30), 'Flower': (0.60, 0.28, 0.55),
    'GlossLeaf': (0.17, 0.46, 0.14), 'MintLeaf': (0.36, 0.64, 0.26), 'DarkLeaf': (0.11, 0.32, 0.11), 'EggFruit': (0.80, 0.86, 0.64),
    'Calyx': (0.22, 0.40, 0.15), 'PeaFruit': (0.30, 0.55, 0.18), 'KaffirFruit': (0.22, 0.42, 0.12), 'PapayaTrunk': (0.56, 0.55, 0.44),
    'PapayaFruit': (0.40, 0.58, 0.16), 'MangoLeaf': (0.13, 0.36, 0.13), 'Mango': (0.86, 0.72, 0.22), 'Bamboo': (0.72, 0.62, 0.38),
    'Bean': (0.36, 0.60, 0.20), 'Tomato': (0.82, 0.16, 0.10),
}
UP = np.array([0, 0, 1.0])
SLOTS = {}   # ช่องวางชิ้นที่เก็บได้ต่อชนิดต้น: [(ตำแหน่ง, ทิศ)] พิกัดสคริปต์ Z ขึ้น


def slot(kind, pos, d=(0, 0, 1.0)):
    d = np.asarray(d, float); SLOTS.setdefault(kind, []).append((np.asarray(pos, float), d / np.linalg.norm(d)))


def blade(B, sub, base, d, length, width, droop, twist=0.0, n=6):
    # ใบแบนสองหน้า ไปตามโค้ง (โน้มลงปลายใบ) กว้างที่โคน แหลมที่ปลาย
    d = d / np.linalg.norm(d); side = np.cross(d, UP)
    side = side / np.linalg.norm(side) if np.linalg.norm(side) > 1e-6 else np.array([1.0, 0, 0])
    s = np.linspace(0, 1, n)
    path = [base + d * length * si - UP * droop * si * si for si in s]
    w = width * np.sin(np.minimum(s * 2.2 + 0.25, 1.0) * math.pi / 2) * (1 - s ** 1.5) + 0.0005
    left = [p + side * wi / 2 for p, wi in zip(path, w)]; right = [p - side * wi / 2 for p, wi in zip(path, w)]
    face = np.cross(side, d)
    for f in (face, -face):
        o = B.add_verts(left + right)
        for i in range(n - 1):
            B.tri(sub, o + i, o + i + 1, o + n + i + 1, f); B.tri(sub, o + i, o + n + i + 1, o + n + i, f)


def ball(B, sub, c, r, sides=6, rings=4, squash=1.0):
    path = [c + UP * r * squash * math.cos(math.pi * k / rings) for k in range(rings + 1)]
    radii = [max(r * math.sin(math.pi * k / rings), 1e-4) for k in range(rings + 1)]
    sweep(B, sub, path[::-1], radii[::-1], sides, cap_start=True, cap_end=True)


def soil(B, r):
    sweep(B, 'Soil', [[0, 0, -0.005], [0, 0, 0.01]], [r, r * 0.9], 7, cap_start=True)


def pack(B):
    U, subs = B.export()
    names = [n for n, t in subs.items() if t]
    return {'vertices': [round(float(x), 5) for x in U.reshape(-1)],
            'submeshes': [{'name': n, 'color': list(COLORS[n]), 'triangles': subs[n]} for n in names]}


def scallion():
    # ต้นหอม: กอละ 5 ต้น โคนขาว ใบกลมกลวงสีเขียวโค้งออก สูงราว 28 ซม.
    B = Builder(['Pale', 'Leaf', 'Soil'])
    for i in range(5):
        a = i * 2 * math.pi / 5 + rng.uniform(-0.3, 0.3); off = np.array([math.cos(a), math.sin(a), 0]) * 0.012
        sweep(B, 'Pale', [off, off + UP * 0.05], [0.0055, 0.005], 6, cap_start=True, cap_end=False)
        for k in range(2):
            la = a + (k - 0.5) * 0.6
            d = np.array([math.cos(la) * 0.25, math.sin(la) * 0.25, 1.0]); d /= np.linalg.norm(d)
            L = rng.uniform(0.2, 0.26); s = np.linspace(0, 1, 5)
            path = [off + UP * 0.045 + d * L * si + np.array([math.cos(la), math.sin(la), 0]) * 0.05 * si * si - UP * 0.04 * si ** 3 for si in s]
            sweep(B, 'Leaf', path, np.linspace(0.0035, 0.0008, 5), 4)
    soil(B, 0.035)
    return pack(B)


def coriander():
    # ผักชี: ก้านเล็กหลายก้านแตกจากกลาง ปลายก้านมีใบหยักเป็นพัด สูงราว 18 ซม.
    B = Builder(['Stem', 'LightLeaf', 'Soil'])
    for i in range(7):
        a = i * 2 * math.pi / 7 + rng.uniform(-0.2, 0.2)
        d = np.array([math.cos(a) * 0.55, math.sin(a) * 0.55, 1.0]); d /= np.linalg.norm(d)
        L = rng.uniform(0.11, 0.16); s = np.linspace(0, 1, 4)
        path = [d * L * si - UP * 0.02 * si * si for si in s]
        sweep(B, 'Stem', path, np.linspace(0.0018, 0.001, 4), 4, cap_start=True)
        tip = path[-1]
        for k in range(2):   # ใบย่อย 2 แฉก
            la = a + (k - 0.5) * 0.8
            blade(B, 'LightLeaf', tip, np.array([math.cos(la), math.sin(la), 0.35]), 0.032, 0.03, 0.006, n=4)
    soil(B, 0.05)
    return pack(B)


def lemongrass():
    # ตะไคร้: กอโคนหนาสีขาวอมเขียว ใบยาวแบนโค้งตกแบบน้ำพุ สูงราว 55 ซม.
    B = Builder(['Pale', 'Leaf', 'Soil'])
    for i in range(6):
        a = i * 2 * math.pi / 6 + rng.uniform(-0.2, 0.2); off = np.array([math.cos(a), math.sin(a), 0]) * 0.022
        sweep(B, 'Pale', [off, off + UP * 0.13], [0.011, 0.009], 6, cap_start=True, cap_end=True)
        for k in range(3):
            la = a + (k - 1) * 0.5
            d = np.array([math.cos(la) * 0.45, math.sin(la) * 0.45, 1.0])
            blade(B, 'Leaf', off + UP * 0.12, d, rng.uniform(0.38, 0.48), 0.016, rng.uniform(0.12, 0.2), n=7)
    soil(B, 0.06)
    return pack(B)


def lime_tree():
    # มะนาว: ต้นเล็กในโคก ลำต้น + กิ่ง 4 + พุ่มใบทรงกลม + ลูกมะนาว 8 ลูก สูงราว 85 ซม.
    B = Builder(['Bark', 'Leaf', 'Fruit', 'Soil'])
    trunk = [UP * z + np.array([0.01, 0, 0]) * z for z in np.linspace(0, 0.42, 5)]
    sweep(B, 'Bark', trunk, np.linspace(0.022, 0.014, 5), 6, cap_start=True)
    crowns = [(np.array([0, 0, 0.62]), 0.2)]
    for i in range(4):
        a = i * math.pi / 2 + 0.4
        end = np.array([math.cos(a) * 0.16, math.sin(a) * 0.16, 0.5 + 0.04 * (i % 2)])
        sweep(B, 'Bark', [trunk[-1], (trunk[-1] + end) / 2 + UP * 0.03, end], [0.012, 0.009, 0.006], 5)
        crowns.append((end + UP * 0.05, 0.13))
    for c, r in crowns:
        ball(B, 'Leaf', c, r, sides=8, rings=5, squash=0.85)
    # ลูกมะนาวเป็นชิ้นที่เก็บได้ (สร้างใน Unity) · ฝังครึ่งลูกที่ผิวพุ่มข้าง 4 พุ่ม พุ่มละ 2 ลูก ค่อนลงล่าง
    for k in range(8):
        i = k % 4; cc, rr = crowns[1 + i]
        a = i * math.pi / 2 + 0.4 + (0.55 if k >= 4 else -0.55)
        slot('lime', on_crown(cc, rr, 0.85, a, 0.55 if k >= 4 else 0.25, 0.95))
    soil(B, 0.09)
    return pack(B)


def oval_leaf(B, sub, base, d, L, W, droop=0.2):
    # ใบรูปไข่สองหน้า (ใช้กับโหระพา สะระแหน่ มะเขือ ถั่ว มะเขือเทศ)
    d = d / np.linalg.norm(d); s = np.cross(d, UP)
    s = s / np.linalg.norm(s) if np.linalg.norm(s) > 1e-6 else np.array([1.0, 0, 0])
    up = np.cross(s, d); dr = -droop * L
    pts = [base, base + d * L * 0.3 + s * W * 0.45, base + d * L * 0.65 + s * W * 0.4 + up * dr * 0.4, base + d * L + up * dr,
           base + d * L * 0.65 - s * W * 0.4 + up * dr * 0.4, base + d * L * 0.3 - s * W * 0.45, base + d * L * 0.5 + up * 0.002]
    for f in (up, -up):
        o = B.add_verts(pts)
        for a, b in ((0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 0)):
            B.tri(sub, o + a, o + b, o + 6, f)


def on_crown(c, r, squash, a, down, depth=0.92):
    # จุดบนผิวพุ่มใบทรงรี (ฝังเข้าไปเล็กน้อยด้วย depth) ตามมุม a และความเอียงลง down
    d = np.array([math.cos(a), math.sin(a), -down]); d /= np.linalg.norm(d)
    return c + r * depth * np.array([d[0], d[1], d[2] * squash])


def dirv(a, z):
    v = np.array([math.cos(a), math.sin(a), z]); return v / np.linalg.norm(v)


def stalk(B, sub, base, d, length, r0, r1, n=4, sides=5, bend=0.0):
    pts = [base + d * length * t - UP * bend * t * t for t in np.linspace(0, 1, n)]
    sweep(B, sub, pts, np.linspace(r0, r1, n), sides, cap_start=True)
    return pts


def galangal():
    # ข่า: ลำต้นเทียมโคนชมพู 5 ลำ ใบหอกเรียงสลับ สูงราว 65 ซม.
    B = Builder(['PinkBase', 'Stem', 'Leaf', 'Soil'])
    for i in range(5):
        a = i * 2 * math.pi / 5 + rng.uniform(-0.3, 0.3); off = np.array([math.cos(a), math.sin(a), 0]) * 0.03
        H = rng.uniform(0.5, 0.65); d = dirv(a, 6.0)
        sweep(B, 'PinkBase', [off, off + d * 0.08], [0.011, 0.009], 6, cap_start=True, cap_end=False)
        pts = stalk(B, 'Stem', off + d * 0.07, d, H - 0.07, 0.008, 0.004, 4, 5)
        for k in range(6):
            t = 0.3 + 0.13 * k; p = off + d * H * t
            la = a + (math.pi if k % 2 else 0) + rng.uniform(-0.3, 0.3)
            blade(B, 'Leaf', p, dirv(la, 0.9), rng.uniform(0.16, 0.22), 0.045, 0.05, n=5)
    soil(B, 0.07)
    return pack(B)


def shallot():
    # หอมแดง: หัวม่วงแดงโผล่จากดิน 5 หัว ใบกลมเล็ก 2 ใบต่อหัว สูงราว 25 ซม.
    B = Builder(['Bulb', 'Leaf', 'Soil'])
    for i in range(5):
        a = i * 2 * math.pi / 5 + rng.uniform(-0.3, 0.3); off = np.array([math.cos(a), math.sin(a), 0]) * 0.022
        ball(B, 'Bulb', off + UP * 0.012, 0.015, sides=6, rings=3, squash=1.3)
        for k in range(2):
            la = a + (k - 0.5) * 0.7
            pts = [off + UP * 0.03 + dirv(la, 4.0) * L for L in np.linspace(0, rng.uniform(0.18, 0.24), 4)]
            pts = [q - UP * 0.03 * (j / 3) ** 3 for j, q in enumerate(pts)]
            sweep(B, 'Leaf', pts, np.linspace(0.0025, 0.0007, 4), 4)
    soil(B, 0.04)
    return pack(B)


def thai_basil():
    # โหระพา: ก้านม่วง ใบเขียวมัน ช่อดอกม่วง สูงราว 30 ซม.
    B = Builder(['PurpleStem', 'GlossLeaf', 'Flower', 'Soil'])
    for i in range(6):
        a = i * 2 * math.pi / 6 + rng.uniform(-0.2, 0.2); d = dirv(a, 2.2); L = rng.uniform(0.18, 0.26)
        pts = stalk(B, 'PurpleStem', np.array([0, 0, 0.01]), d, L, 0.003, 0.0015, 4, 4)
        for k, t in enumerate((0.35, 0.6, 0.85)):
            p = np.array([0, 0, 0.01]) + d * L * t
            for sd in (0, math.pi):
                oval_leaf(B, 'GlossLeaf', p, dirv(a + k * 1.5 + sd, 0.3), 0.055 - 0.012 * k, 0.032 - 0.006 * k, 0.25)
        tip = pts[-1]
        sweep(B, 'Flower', [tip + UP * z for z in np.linspace(0, 0.045, 4)], [0.005, 0.005, 0.004, 0.001], 5)
    soil(B, 0.06)
    return pack(B)


def mint():
    # สะระแหน่: พุ่มเตี้ยแผ่ ใบกลมเล็กเขียวสด สูงราว 12 ซม.
    B = Builder(['Stem', 'MintLeaf', 'Soil'])
    for i in range(9):
        a = i * 2 * math.pi / 9 + rng.uniform(-0.2, 0.2); d = dirv(a, 0.9); L = rng.uniform(0.09, 0.13)
        stalk(B, 'Stem', np.array([0, 0, 0.005]), d, L, 0.0018, 0.001, 3, 4)
        for k, t in enumerate((0.4, 0.7, 1.0)):
            p = np.array([0, 0, 0.005]) + d * L * t
            for sd in (-1, 1):
                oval_leaf(B, 'MintLeaf', p, dirv(a + sd * 1.2, 0.5), 0.028, 0.024, 0.15)
    soil(B, 0.07)
    return pack(B)


def eggplant():
    # มะเขือเปราะ: ต้นสูงราว 45 ซม. ใบใหญ่ ลูกกลมขาวอมเขียว 3 ลูก
    B = Builder(['Stem', 'Leaf', 'EggFruit', 'Calyx', 'Soil'])
    trunk = stalk(B, 'Stem', np.zeros(3), UP, 0.36, 0.008, 0.004, 4, 5)
    for k in range(7):
        a = k * 2.4; h = 0.12 + 0.035 * k; p = UP * h
        sweep(B, 'Stem', [p, p + dirv(a, 0.4) * 0.05], [0.003, 0.002], 4)
        oval_leaf(B, 'Leaf', p + dirv(a, 0.4) * 0.05, dirv(a, 0.15), 0.12, 0.08, 0.25)
    for k in range(3):
        a = k * 2 * math.pi / 3 + 0.5; c = dirv(a, 0) * 0.09 + UP * (0.12 + 0.04 * k)
        sweep(B, 'Stem', [UP * (c[2] + 0.07), c + UP * 0.06 - dirv(a, 0) * 0.02, c + UP * 0.03], [0.0025, 0.0022, 0.002], 4)
        slot('eggplant', c)
    soil(B, 0.06)
    return pack(B)


def pea_eggplant():
    # มะเขือพวง: ต้นสูงราว 50 ซม. ใบหยัก ลูกเล็กเป็นพวง 3 พวง
    B = Builder(['Stem', 'Leaf', 'PeaFruit', 'Soil'])
    stalk(B, 'Stem', np.zeros(3), UP, 0.42, 0.007, 0.003, 4, 5)
    for k in range(6):
        a = k * 2.4 + 1; p = UP * (0.15 + 0.045 * k)
        oval_leaf(B, 'Leaf', p, dirv(a, 0.25), 0.09, 0.055, 0.25)
    for k in range(3):
        a = k * 2 * math.pi / 3; c = dirv(a, 0) * 0.07 + UP * (0.2 + 0.05 * k)
        sweep(B, 'Stem', [UP * (c[2] + 0.05), c + UP * 0.03], [0.002, 0.0015], 4)
        slot('peaEggplant', c)
    soil(B, 0.05)
    return pack(B)


def chives():
    # กุยช่าย: กอใบแบนเรียวยาว เขียวเข้ม สูงราว 30 ซม.
    B = Builder(['Leaf', 'Soil'])
    for i in range(12):
        a = i * 2 * math.pi / 12 + rng.uniform(-0.2, 0.2)
        blade(B, 'Leaf', np.array([math.cos(a), math.sin(a), 0]) * 0.008, dirv(a, 3.5), rng.uniform(0.22, 0.3), 0.007, 0.06, n=5)
    soil(B, 0.035)
    return pack(B)


def culantro():
    # ผักชีฝรั่ง: ใบยาวแผ่ราบเป็นกระจุก (rosette) สูงราว 8 ซม.
    B = Builder(['Leaf', 'Soil'])
    for i in range(9):
        a = i * 2 * math.pi / 9 + rng.uniform(-0.15, 0.15)
        blade(B, 'Leaf', UP * 0.01, dirv(a, 0.45), rng.uniform(0.1, 0.14), 0.028, 0.03, n=5)
    soil(B, 0.035)
    return pack(B)


def kaffir_tree():
    # มะกรูด: ต้นเล็กสูงราว 1.1 ม. พุ่มใบเขียวเข้ม ลูกผิวขรุขระ 6 ลูก
    B = Builder(['Bark', 'DarkLeaf', 'KaffirFruit', 'Soil'])
    trunk = [UP * z for z in np.linspace(0, 0.55, 4)]
    sweep(B, 'Bark', trunk, np.linspace(0.022, 0.014, 4), 6, cap_start=True)
    crowns = [(UP * 0.82, 0.24)]
    for i in range(3):
        a = i * 2 * math.pi / 3 + 0.3; end = dirv(a, 0) * 0.17 + UP * 0.68
        sweep(B, 'Bark', [trunk[-1], end], [0.011, 0.007], 5)
        crowns.append((end + UP * 0.04, 0.15))
    for c, r in crowns:
        ball(B, 'DarkLeaf', c, r, sides=8, rings=5, squash=0.85)
    for k in range(6):   # ลูกมะกรูด (ตกแต่ง) ฝังที่ผิวพุ่มข้าง 3 พุ่ม
        i = k % 3; cc, rr = crowns[1 + i]
        a = i * 2 * math.pi / 3 + 0.3 + (0.6 if k >= 3 else -0.6)
        ball(B, 'KaffirFruit', on_crown(cc, rr, 0.85, a, 0.4, 0.95), 0.026, sides=5, rings=4)
    for k in range(6):   # ใบมะกรูดที่เก็บได้ ปักออกจากผิวพุ่ม
        d = dirv(k * 2 * math.pi / 6 + 0.7, 0.35)
        slot('kaffirLime', UP * 0.8 + d * 0.22, d)
    soil(B, 0.1)
    return pack(B)


def papaya_tree():
    # มะละกอ: ลำต้นตรงสูงราว 1.9 ม. ใบแฉกกางเป็นร่มที่ยอด ลูกเขียวเกาะใต้ใบ
    B = Builder(['PapayaTrunk', 'Stem', 'Leaf', 'PapayaFruit', 'Soil'])
    H = 1.7
    sweep(B, 'PapayaTrunk', [UP * z for z in np.linspace(0, H, 5)], np.linspace(0.06, 0.035, 5), 7, cap_start=True)
    top = UP * H
    for i in range(9):
        a = i * 2 * math.pi / 9 + rng.uniform(-0.1, 0.1); d = dirv(a, 0.55 if i % 2 else 0.25)
        pts = stalk(B, 'Stem', top, d, 0.5, 0.008, 0.005, 3, 4, bend=0.08)
        tip = pts[-1]
        for k in range(5):
            la = a + (k - 2) * 0.55
            blade(B, 'Leaf', tip, dirv(la, 0.15), 0.26 - abs(k - 2) * 0.04, 0.12, 0.08, n=4)
    for k in range(7):
        a = k * 2 * math.pi / 7; c = top - UP * (0.12 + 0.07 * (k % 3)) + dirv(a, 0) * 0.09
        slot('papaya', c)
    soil(B, 0.12)
    return pack(B)


def mango_tree():
    # มะม่วง: ต้นใหญ่สูงราว 3 ม. พุ่มใบทรงโดม ลูกเหลืองห้อย 10 ลูก
    B = Builder(['Bark', 'MangoLeaf', 'Mango', 'Stem', 'Soil'])
    trunk = [np.array([0.03 * math.sin(z * 2), 0, z]) for z in np.linspace(0, 1.2, 5)]
    sweep(B, 'Bark', trunk, np.linspace(0.12, 0.08, 5), 7, cap_start=True)
    crowns = [(UP * 2.2, 0.75)]
    for i in range(4):
        a = i * math.pi / 2 + 0.4; end = dirv(a, 0) * 0.65 + UP * 1.75
        sweep(B, 'Bark', [trunk[-1], (trunk[-1] + end) / 2 + UP * 0.15, end], [0.06, 0.045, 0.03], 6)
        crowns.append((end + UP * 0.15, 0.55))
    for c, r in crowns:
        ball(B, 'MangoLeaf', c, r, sides=9, rings=6, squash=0.75)
    # ลูกมะม่วงห้อยรอบพุ่มแบบเดิม (ผู้ใช้เลือก 5 พ.ย.): วงรัศมี 0.95–1.12 ม. สูง 1.45–1.75 ม. ก้านขึ้นไปหาใบ
    for k in range(10):
        a = k * 2 * math.pi / 10 + rng.uniform(-0.15, 0.15); rr = rng.uniform(0.95, 1.12); h = rng.uniform(1.45, 1.75)
        c = dirv(a, 0) * rr + UP * h
        sweep(B, 'Stem', [c + UP * 0.16, c + UP * 0.05], [0.003, 0.003], 4)
        slot('mango', c)
    soil(B, 0.3)
    return pack(B)


def bean_trellis():
    # ถั่วฝักยาว: ค้างไม้ไผ่ทรง A ยาว 1.1 ม. (แกน X) เถาเลื้อย ใบ 3 แฉก ฝักยาวห้อย
    B = Builder(['Bamboo', 'Stem', 'Leaf', 'Bean'])
    L = 1.1; H = 1.3
    xs = np.linspace(-L / 2, L / 2, 4)
    for x in xs:
        for sy in (-1, 1):
            sweep(B, 'Bamboo', [np.array([x, sy * 0.16, 0]), np.array([x, 0, H])], [0.009, 0.007], 4, cap_start=True)
    sweep(B, 'Bamboo', [np.array([-L / 2 - 0.05, 0, H - 0.02]), np.array([L / 2 + 0.05, 0, H - 0.02])], [0.008, 0.008], 4, cap_start=True)
    for x in xs:
        for sy in (-1, 1):
            pts = [np.array([x, sy * 0.16 * (1 - t), H * t]) + np.array([0.025 * math.sin(t * 12), 0.02 * sy * math.cos(t * 12), 0]) for t in np.linspace(0, 0.95, 7)]
            sweep(B, 'Stem', pts, [0.003] * 7, 4)
            for j in (1, 3, 5):
                for k in range(3):
                    oval_leaf(B, 'Leaf', pts[j], dirv(k * 2.1 + j + sy, 0.2), 0.06, 0.04, 0.2)
            for j in (2, 4):
                p = pts[j] + np.array([0, sy * 0.03, 0])
                if j == 2:
                    slot('beanTrellis', p, -UP)   # ฝักล่างเอื้อมถึง = ชิ้นที่เก็บได้ (ห้อยลง)
                    continue
                bean = [p - UP * 0.3 * t + np.array([0.02 * t * t, 0, 0]) for t in np.linspace(0, 1, 4)]
                sweep(B, 'Bean', bean, [0.0035, 0.0035, 0.003, 0.0015], 4, cap_start=True)
    return pack(B)


def tomato():
    # มะเขือเทศ: ต้นผูกหลักไม้ไผ่ สูงราว 60 ซม. ใบหยัก ลูกแดง 3 ลูก เขียว 2 ลูก
    B = Builder(['Bamboo', 'Stem', 'Leaf', 'Tomato', 'Fruit', 'Soil'])
    sweep(B, 'Bamboo', [np.array([0.02, 0, 0]), np.array([0.02, 0, 0.7])], [0.006, 0.005], 4, cap_start=True)
    stalk(B, 'Stem', np.zeros(3), UP, 0.58, 0.006, 0.003, 4, 5)
    for k in range(6):
        a = k * 2.4; p = UP * (0.12 + 0.07 * k)
        tip = p + dirv(a, 0.2) * 0.1
        sweep(B, 'Stem', [p, tip], [0.002, 0.0015], 4)
        for j in range(3):
            oval_leaf(B, 'Leaf', p + dirv(a, 0.2) * (0.035 * (j + 1)), dirv(a + (j - 1) * 0.9, 0.1), 0.045, 0.03, 0.3)
    for k in range(5):
        a = k * 2 * math.pi / 5 + 0.3; c = dirv(a, 0) * 0.07 + UP * (0.2 + 0.06 * (k % 3))
        if k < 3:
            sweep(B, 'Stem', [UP * (c[2] + 0.05), c + UP * 0.024], [0.002, 0.0015], 4)
            slot('tomato', c)   # ลูกแดง = ชิ้นที่เก็บได้ · ลูกเขียวเป็นของตกแต่ง
        else:
            ball(B, 'Fruit', c, 0.024, sides=6, rings=4, squash=0.85)
    soil(B, 0.05)
    return pack(B)



# ---------- ชิ้นที่เก็บได้ (จุดกำเนิด = จุดปัก/ห้อย · ทิศ +Z ของสคริปต์ = ทิศของช่อง) ----------

def pick_scallion():
    B = Builder(['Pale', 'Leaf'])
    sweep(B, 'Pale', [np.zeros(3), UP * 0.06], [0.006, 0.0055], 6, cap_start=True, cap_end=False)
    for k in range(2):
        d = dirv(k * math.pi, 7.0)
        sweep(B, 'Leaf', [UP * 0.055 + d * L for L in np.linspace(0, 0.2, 4)], np.linspace(0.004, 0.001, 4), 5)
    return pack(B)


def pick_coriander():
    B = Builder(['Stem', 'LightLeaf'])
    for i in range(3):
        a = i * 2.1; d = dirv(a, 4.0)
        pts = stalk(B, 'Stem', np.zeros(3), d, 0.13, 0.0018, 0.001, 3, 4)
        for k in range(3):
            blade(B, 'LightLeaf', pts[-1], dirv(a + (k - 1) * 0.8, 0.6), 0.035, 0.032, 0.005, n=4)
    return pack(B)


def pick_culantro():
    B = Builder(['Leaf'])
    for i in range(3):
        blade(B, 'Leaf', np.zeros(3), dirv(i * 0.5 - 0.5, 4.0), 0.13, 0.03, 0.01, n=5)
    return pack(B)


def pick_lemongrass():
    B = Builder(['Pale', 'Leaf'])
    sweep(B, 'Pale', [np.zeros(3), UP * 0.2], [0.011, 0.009], 6, cap_start=True, cap_end=True)
    for k in range(2):
        blade(B, 'Leaf', UP * 0.19, dirv(k * math.pi, 5.0), 0.16, 0.016, 0.03, n=4)
    return pack(B)


def pick_lime():
    B = Builder(['Fruit', 'Stem'])
    ball(B, 'Fruit', np.zeros(3), 0.03, sides=8, rings=5, squash=1.1)
    sweep(B, 'Stem', [UP * 0.03, UP * 0.045], [0.002, 0.002], 4)
    return pack(B)


def pick_galangal():
    # เหง้าข่า: ก้อนชมพูอมเหลืองต่อกัน 3 ก้อน + หน่อสั้น (วางนอนบนดินข้างกอ)
    B = Builder(['PinkBase', 'Stem'])
    for k, (x, r) in enumerate(((-0.035, 0.016), (0.0, 0.02), (0.035, 0.015))):
        ball(B, 'PinkBase', np.array([x, 0, 0.012]), r, sides=6, rings=4, squash=0.8)
    sweep(B, 'Stem', [np.array([0, 0, 0.02]), np.array([0, 0, 0.06])], [0.006, 0.004], 5)
    return pack(B)


def pick_shallot():
    B = Builder(['Bulb', 'Leaf'])
    ball(B, 'Bulb', UP * 0.016, 0.018, sides=7, rings=4, squash=1.3)
    for k in range(2):
        d = dirv(k * math.pi, 6.0)
        sweep(B, 'Leaf', [UP * 0.035 + d * L for L in np.linspace(0, 0.12, 3)], [0.003, 0.002, 0.0008], 4)
    return pack(B)


def pick_herb_sprig(stem_sub, leaf_sub, L, leaf_l, leaf_w, flower):
    B = Builder([stem_sub, leaf_sub] + (['Flower'] if flower else []))
    pts = stalk(B, stem_sub, np.zeros(3), UP, L, 0.0028, 0.0014, 4, 5)
    for k, t in enumerate((0.35, 0.6, 0.85)):
        for sd in (0, math.pi):
            oval_leaf(B, leaf_sub, UP * L * t, dirv(k * 1.5 + sd, 0.4), leaf_l - 0.01 * k, leaf_w - 0.005 * k, 0.2)
    if flower:
        sweep(B, 'Flower', [pts[-1] + UP * z for z in np.linspace(0, 0.04, 4)], [0.005, 0.005, 0.004, 0.001], 5)
    return pack(B)


def pick_eggplant():
    B = Builder(['EggFruit', 'Calyx', 'Stem'])
    ball(B, 'EggFruit', np.zeros(3), 0.028, sides=8, rings=5, squash=0.9)
    ball(B, 'Calyx', UP * 0.022, 0.015, sides=6, rings=2, squash=0.5)
    sweep(B, 'Stem', [UP * 0.025, UP * 0.045], [0.0025, 0.002], 4)
    return pack(B)


def pick_pea_eggplant():
    B = Builder(['PeaFruit', 'Stem'])
    sweep(B, 'Stem', [UP * 0.03, np.zeros(3) - UP * 0.03], [0.0015, 0.0015], 4)
    for j in range(7):
        a = j * 2.4; q = np.array([math.cos(a) * 0.012, math.sin(a) * 0.012, 0.02 - 0.008 * j])
        ball(B, 'PeaFruit', q, 0.008, sides=6, rings=3)
    return pack(B)


def pick_chives():
    B = Builder(['Leaf'])
    for i in range(5):
        blade(B, 'Leaf', np.zeros(3), dirv(i * 1.26, 8.0), 0.24, 0.007, 0.03, n=5)
    return pack(B)


def pick_kaffir_leaf():
    # ใบมะกรูด: ใบคู่ต่อกันเป็นรูปเลข 8 (ลักษณะเด่น) บนก้านสั้น ชี้ออกตามทิศช่อง
    B = Builder(['DarkLeaf', 'Stem'])
    sweep(B, 'Stem', [np.zeros(3), UP * 0.015], [0.0015, 0.0015], 4, cap_start=True)
    oval_leaf(B, 'DarkLeaf', UP * 0.012, UP + np.array([0, 0.001, 0]), 0.045, 0.035, 0.05)
    oval_leaf(B, 'DarkLeaf', UP * 0.052, UP + np.array([0, 0.001, 0]), 0.06, 0.042, 0.05)
    return pack(B)


def pick_papaya():
    B = Builder(['PapayaFruit'])
    ball(B, 'PapayaFruit', np.zeros(3), 0.05, sides=8, rings=5, squash=1.5)
    return pack(B)


def pick_mango():
    B = Builder(['Mango'])
    ball(B, 'Mango', np.zeros(3), 0.05, sides=8, rings=5, squash=1.35)
    return pack(B)


def pick_bean():
    B = Builder(['Bean'])
    sweep(B, 'Bean', [UP * 0.3 * t + np.array([0.02 * t * t, 0, 0]) for t in np.linspace(0, 1, 5)],
          [0.0016, 0.0038, 0.0038, 0.0034, 0.0015], 5, cap_start=True)
    return pack(B)


def pick_tomato():
    B = Builder(['Tomato', 'Calyx'])
    ball(B, 'Tomato', np.zeros(3), 0.026, sides=8, rings=5, squash=0.85)
    ball(B, 'Calyx', UP * 0.02, 0.01, sides=5, rings=2, squash=0.4)
    return pack(B)


# ชนิดต้น → (ingredientId, ชื่อไทย, ฟังก์ชันสร้างชิ้น, สเกลชิ้นเทียบต้น)
PICKS = [
    ('scallion', 'spring_onion', 'ต้นหอม', pick_scallion, 1.0),
    ('coriander', 'coriander', 'ผักชี', pick_coriander, 1.0),
    ('culantro', 'culantro', 'ผักชีฝรั่ง', pick_culantro, 1.0),
    ('lemongrass', 'lemongrass', 'ตะไคร้', pick_lemongrass, 1.0),
    ('lime', 'lime', 'มะนาว', pick_lime, 1.2),
    ('galangal', 'galangal', 'ข่า', pick_galangal, 1.2),
    ('shallot', 'shallot', 'หอมแดง', pick_shallot, 1.0),
    ('thaiBasil', 'thai_basil', 'โหระพา', lambda: pick_herb_sprig('PurpleStem', 'GlossLeaf', 0.12, 0.05, 0.03, True), 1.2),
    ('mint', 'mint', 'สะระแหน่', lambda: pick_herb_sprig('Stem', 'MintLeaf', 0.09, 0.03, 0.026, False), 1.3),
    ('eggplant', 'thai_eggplant', 'มะเขือเปราะ', pick_eggplant, 1.0),
    ('peaEggplant', 'pea_eggplant', 'มะเขือพวง', pick_pea_eggplant, 1.2),
    ('chives', 'chinese_chives', 'กุยช่าย', pick_chives, 1.0),
    ('kaffirLime', 'kaffir_lime_leaf', 'ใบมะกรูด', pick_kaffir_leaf, 1.3),
    ('papaya', 'green_papaya', 'มะละกอดิบ', pick_papaya, 1.0),
    ('mango', 'mango', 'มะม่วง', pick_mango, 1.0),
    ('beanTrellis', 'long_bean', 'ถั่วฝักยาว', pick_bean, 1.0),
    ('tomato', 'tomato', 'มะเขือเทศ', pick_tomato, 1.0),
]
# ผักกอ/ใบ: ชิ้นปักกลางกอ (หรือข้างกอสำหรับเหง้าข่า)
CENTER_SLOTS = {'scallion': (0, 0, 0.005), 'coriander': (0, 0, 0.005), 'culantro': (0, 0, 0.01), 'lemongrass': (0.03, 0.03, 0.005),
                'shallot': (0, 0, 0.0), 'thaiBasil': (0, 0, 0.01), 'mint': (0, 0, 0.008), 'chives': (0, 0, 0.005),
                'galangal': (0.075, 0.0, 0.0)}


if __name__ == '__main__':
    data = {'scallion': scallion(), 'coriander': coriander(), 'lemongrass': lemongrass(), 'lime': lime_tree()}
    # ผักเพิ่มครบ 19 ชนิด (5 พ.ย.) · ต่อท้ายเพื่อให้โมเดลชุดแรกไม่เปลี่ยน (ใช้ rng ต่อกัน)
    for name, fn in (('galangal', galangal), ('shallot', shallot), ('thaiBasil', thai_basil), ('mint', mint),
                     ('eggplant', eggplant), ('peaEggplant', pea_eggplant), ('chives', chives), ('culantro', culantro),
                     ('kaffirLime', kaffir_tree), ('papaya', papaya_tree), ('mango', mango_tree),
                     ('beanTrellis', bean_trellis), ('tomato', tomato)):
        data[name] = fn()
    for k, pos in CENTER_SLOTS.items():
        slot(k, pos)
    items = []
    for plant, ing_id, name_th, fn, scale in PICKS:
        sl = SLOTS.get(plant, [])
        flat = []
        for pos, d in sl:
            flat += [round(float(pos[0]), 5), round(float(pos[2]), 5), round(float(pos[1]), 5),
                     round(float(d[0]), 4), round(float(d[2]), 4), round(float(d[1]), 4)]   # Unity (x, z, y)
        items.append({'plant': plant, 'id': ing_id, 'nameTh': name_th, 'scale': scale, 'slots': flat, 'mesh': fn()})
    with open(OUT.replace('VeggieModels.json', 'VeggiePicks.json'), 'w') as f:
        json.dump({'items': items}, f, ensure_ascii=False, separators=(',', ':'))
    for it in items:
        print('pick', it['plant'], it['id'], 'slots', len(it['slots']) // 6, 'tris', sum(len(s['triangles']) for s in it['mesh']['submeshes']) // 3)
    with open(OUT, 'w') as f:
        json.dump(data, f, separators=(',', ':'))
    for k, v in data.items():
        print(k, 'tris', sum(len(s['triangles']) for s in v['submeshes']) // 3)
    print('->', os.path.normpath(OUT))
