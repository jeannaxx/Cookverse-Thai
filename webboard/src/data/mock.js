// ข้อมูลตัวอย่าง — ใช้จนกว่าจะต่อ Supabase (players · sessions · action_logs · results · menus · users)
// เมื่อต่อจริง ให้แทนที่ฟังก์ชันใน src/data/api.js โดยไม่ต้องแก้หน้าเว็บ

export const PASS_LINE = 70; // ต่ำกว่านี้ = ควรฝึก/สอนเพิ่ม

export const sessions = [
  { id: 'session-001', player: 'ผู้เล่น A', menu: 'ผัดกะเพราไก่', mode: 'Challenge', score: 76, result: 'ผ่าน', time: '12:40 นาที' },
  { id: 'session-002', player: 'ผู้เล่น B', menu: 'ผัดกะเพราไก่', mode: 'Training', score: 92, result: 'ผ่านดีมาก', time: '9:15 นาที' },
  { id: 'session-003', player: 'ผู้เล่น C', menu: 'ผัดกะเพราไก่', mode: 'Challenge', score: 41, result: 'ไม่ผ่าน', time: '15:02 นาที' },
  { id: 'session-004', player: 'ผู้เล่น D', menu: 'ผัดกะเพราไก่', mode: 'Training', score: 64, result: 'ต้องฝึกเพิ่ม', time: '11:28 นาที' },
  { id: 'session-005', player: 'ผู้เล่น A', menu: 'ผัดกะเพราไก่', mode: 'Training', score: 85, result: 'ผ่านดีมาก', time: '10:03 นาที' },
  { id: 'session-006', player: 'ผู้เล่น E', menu: 'ผัดกะเพราไก่', mode: 'Challenge', score: 71, result: 'ผ่าน', time: '13:51 นาที' },
];

export const overview = {
  kpis: [
    { label: 'รอบการเล่นทั้งหมด', value: '24', sub: 'ผู้เล่น 9 คน' },
    { label: 'คะแนนเฉลี่ย', value: '72', sub: 'จาก 100 คะแนน' },
    { label: 'อัตราผ่าน', value: '67%', sub: 'คะแนน 70 ขึ้นไป' },
    { label: 'ใช้คำใบ้เฉลี่ย', value: '1.8', sub: 'ครั้งต่อรอบ (Challenge)' },
  ],
  rubric: [
    { label: 'วัตถุดิบถูกชนิดและครบ', text: '18.6 / 20', pct: 93 },
    { label: 'การล้างและเตรียม', text: '12.1 / 15', pct: 81 },
    { label: 'ลำดับการปรุง', text: '16.0 / 25', pct: 64 },
    { label: 'ความสุกและการใช้ไฟ', text: '13.8 / 20', pct: 69 },
    { label: 'การเสิร์ฟและจบภารกิจ', text: '8.9 / 10', pct: 89 },
    { label: 'เวลาและการทำซ้ำ', text: '5.2 / 10', pct: 52 },
  ],
  mistakes: [
    { text: 'ใส่ใบกะเพราก่อนไก่สุก', count: '11 ครั้ง' },
    { text: 'ไม่ปิดไฟก่อนเสิร์ฟ', count: '7 ครั้ง' },
    { text: 'ไม่ล้างผักจากสวน', count: '5 ครั้ง' },
    { text: 'ใส่ไก่ก่อนน้ำมัน', count: '3 ครั้ง' },
  ],
};

// รายละเอียดตัวอย่าง (ทุกรอบใช้ชุดเดียวกันจนกว่าจะมีข้อมูลจริง)
export const sessionDetail = {
  meta: ['รหัสรอบ session-001', 'Quest 3 · WebXR', 'ขอคำใบ้ 2 ครั้ง'],
  verdict: 'ไก่สุก ไม่ไหม้ — ผ่านเงื่อนไขบังคับ',
  rubric: [
    { label: 'วัตถุดิบถูกชนิดและครบจำนวน', text: '20 / 20', pct: 100, note: 'ครบทุกชนิด ไม่วางของซ้ำ' },
    { label: 'การล้างและเตรียม', text: '15 / 15', pct: 100, note: 'ล้างผัก ไม่ล้างไก่ หั่นครบ' },
    { label: 'ลำดับการปรุง', text: '17 / 25', pct: 68, note: 'ใส่ใบกะเพราก่อนไก่สุก' },
    { label: 'ความสุกและการใช้ไฟ', text: '15 / 20', pct: 75, note: 'ไก่สุก ไม่ไหม้ แต่ไม่ปิดไฟก่อนเสิร์ฟ' },
    { label: 'การเสิร์ฟและจบภารกิจ', text: '7 / 10', pct: 70, note: 'เสิร์ฟขณะเตายังเปิดอยู่' },
    { label: 'เวลาและการทำซ้ำที่ไม่จำเป็น', text: '2 / 10', pct: 20, note: 'ขอคำใบ้ 2 ครั้ง ใช้เวลานานกว่าเกณฑ์' },
  ],
  steps: [
    { name: 'รับภารกิจจากป้าแสง', time: '0:12', ok: true },
    { name: 'เก็บกะเพราและพริกจากสวน', time: '2:05', ok: true },
    { name: 'หยิบไก่จากตู้เย็นและกระเทียม', time: '3:40', ok: true },
    { name: 'ล้างกะเพราและพริก', time: '4:55', ok: true },
    { name: 'หั่นไก่ พริก กระเทียม', time: '6:30', ok: true },
    { name: 'ผัดกะเพราไก่ตามลำดับ', time: '10:48', ok: false },
    { name: 'ตักใส่จาน วางจุดเสิร์ฟ', time: '11:52', ok: false },
    { name: 'กลับไปหาป้าแสง', time: '12:40', ok: true },
  ],
  aunt: {
    message:
      'ทำสำเร็จแล้วจ้า ไก่สุกพอดีและเลือกวัตถุดิบครบ แต่รอบนี้ใส่ใบกะเพราเร็วไปนิด ใบเลยสลดก่อนไก่สุก ลองรอให้ไก่สุกทั่วก่อนแล้วค่อยใส่กะเพราเป็นอย่างสุดท้ายนะ อีกเรื่องคือก่อนตักใส่จานอย่าลืมบิดปิดไฟก่อนจ้ะ',
    tips: ['รอไก่สุกก่อนใส่ใบกะเพรา', 'ปิดเตาก่อนเสิร์ฟทุกครั้ง', 'ลองทำ Challenge โดยไม่ขอคำใบ้'],
  },
  errors: [
    { text: 'ใส่ใบกะเพราก่อนไก่สุก', minus: '−8' },
    { text: 'ไม่ปิดไฟก่อนเสิร์ฟ', minus: '−8' },
    { text: 'ขอคำใบ้ 2 ครั้ง', minus: '−4' },
  ],
  log: [
    { t: '00:12', a: 'MissionAccepted', i: '—', s: 'accept_mission', st: '—', ok: 'yes' },
    { t: '01:20', a: 'IngredientCollected', i: 'ใบกะเพรา', s: 'collect_garden', st: 'Raw → Collected', ok: 'yes' },
    { t: '04:31', a: 'IngredientWashed', i: 'พริก', s: 'wash_ingredients', st: 'Collected → Washed', ok: 'yes' },
    { t: '06:02', a: 'IngredientCut', i: 'เนื้อไก่', s: 'prepare_ingredients', st: 'Collected → Cut', ok: 'yes' },
    { t: '07:15', a: 'HeatChanged', i: '—', s: 'cook_krapow', st: 'ไฟแรง (3)', ok: 'yes' },
    { t: '08:40', a: 'IngredientAddedToPan', i: 'ใบกะเพรา', s: 'cook_krapow', st: 'Washed → AddedToPan', ok: 'no' },
    { t: '09:05', a: 'HintRequested', i: '—', s: 'cook_krapow', st: '—', ok: 'neutral' },
    { t: '11:52', a: 'DishServed', i: '—', s: 'serve_dish', st: 'เตายังเปิด', ok: 'no' },
  ],
};

export const menus = [
  { id: 'krapow', name: 'ผัดกะเพราไก่', meta: 'วัตถุดิบ 8 ชนิด · 8 ขั้น', status: 'เปิดใช้ในเกม', live: true, missionId: 'mission_krapow_chicken_01' },
  { id: 'menu2', name: '[เมนูที่ 2]', meta: 'รอยืนยันกับอาจารย์', status: 'ฉบับร่าง', live: false, missionId: '—' },
  { id: 'menu3', name: '[เมนูที่ 3]', meta: 'รอยืนยันกับอาจารย์', status: 'ฉบับร่าง', live: false, missionId: '—' },
];

export const ingredientsByMenu = {
  krapow: [
    { name: 'ใบกะเพรา', id: 'krapow_leaf', qty: '1 กำ', src: 'สวน' },
    { name: 'พริก', id: 'chili', qty: '3 เม็ด', src: 'สวน' },
    { name: 'กระเทียม', id: 'garlic', qty: '3 กลีบ', src: 'ตะกร้าในครัว' },
    { name: 'เนื้อไก่', id: 'chicken', qty: '1 Portion', src: 'ตู้เย็น' },
    { name: 'น้ำมัน', id: 'cooking_oil', qty: '1 Portion', src: 'ชั้นเครื่องปรุง' },
    { name: 'ซอสหอยนางรม', id: 'oyster_sauce', qty: '1 Portion', src: 'ชั้นเครื่องปรุง' },
    { name: 'น้ำปลา', id: 'fish_sauce', qty: '1 Portion', src: 'ชั้นเครื่องปรุง' },
    { name: 'น้ำตาล', id: 'sugar', qty: '1 Portion', src: 'ชั้นเครื่องปรุง' },
  ],
  menu2: [],
  menu3: [],
};

export const rubricWeights = [
  { label: 'วัตถุดิบถูกชนิดและครบ', v: 20 },
  { label: 'การล้างและเตรียม', v: 15 },
  { label: 'ลำดับการปรุง', v: 25 },
  { label: 'ความสุกและการใช้ไฟ', v: 20 },
  { label: 'การเสิร์ฟและจบภารกิจ', v: 10 },
  { label: 'เวลาและการทำซ้ำ', v: 10 },
];

export const users = [
  { name: '[ชื่ออาจารย์]', email: '[อีเมลอาจารย์]', role: 'ผู้สอน', status: 'ใช้งาน' },
  { name: '[ชื่อแอดมิน]', email: '[อีเมลแอดมิน]', role: 'แอดมิน', status: 'ใช้งาน' },
  { name: 'ผู้เล่น A', email: '—', role: 'ผู้เล่น', status: 'ใช้งาน' },
];
