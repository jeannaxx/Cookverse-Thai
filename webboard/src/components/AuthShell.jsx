import { useState } from 'react';
import { Link } from 'react-router-dom';
import Logo from './Logo.jsx';
import auntKitchen from '../assets/aunt-saeng-kitchen.jpg';
import auntTable from '../assets/aunt-saeng-table.jpg';
import thaiFeast from '../assets/thai-feast.jpg';

// ภาพพื้นหลังหน้าเข้าสู่ระบบ สุ่มใหม่ทุกครั้งที่เปิดหน้า (บังคับภาพด้วย ?scene=1|2|3)
// pos = จุดครอปของภาพ, shift = เลื่อนภาพไปทางขวา (px) ให้พ้นหัวเรื่อง, scrim = ความเข้มเงาด้านซ้าย (0–1)
const scenes = [
  { src: auntKitchen, pos: '100% 50%', shift: 80, scrim: 0.86, alt: 'ป้าแสงยืนยิ้มยื่นจานผัดกะเพราไก่ไข่ดาวในครัวไทย' },
  { src: auntTable, pos: '100% 50%', shift: 150, scrim: 0.92, alt: 'ป้าแสงยื่นจานผัดกะเพราไก่ไข่ดาวที่โต๊ะอาหารเต็มไปด้วยกับข้าวไทย' },
  { src: thaiFeast, pos: '45% 50%', shift: 0, scrim: 0.94, alt: 'โต๊ะอาหารไทยในครัวไม้ มีผัดกะเพรา ต้มยำกุ้ง ส้มตำ และข้าวเหนียวมะม่วง' },
];

function pickScene() {
  const forced = Number(new URLSearchParams(window.location.search).get('scene'));
  if (forced >= 1 && forced <= scenes.length) return scenes[forced - 1];
  return scenes[Math.floor(Math.random() * scenes.length)];
}

// โครงหน้าเข้าสู่ระบบ ลืมรหัสผ่าน และตั้งรหัสผ่าน: ภาพด้านซ้าย ฟอร์มด้านขวา
export default function AuthShell({ children }) {
  const [scene] = useState(pickScene);

  return (
    <div className="login">
      <section className="login-hero" style={{ '--scrim': scene.scrim, '--shift': `${scene.shift}px` }}>
        <img className="login-scene" src={scene.src} alt={scene.alt} style={{ objectPosition: scene.pos }} />
        <Link to="/login" className="brand brand-link">
          <div className="brand-tile"><Logo size={20} /></div>
          <div className="brand-name">Cookverse Thai</div>
        </Link>
        <div className="login-copy">
          <h1>ติดตามการเรียนทำอาหารไทยในโลก VR</h1>
          <p>ดูคะแนนตาม Rubric ขั้นตอนที่ผู้เล่นทำผิด Action Log และคำแนะนำจากป้าแสง ของทุกรอบที่เล่นบนแว่นและเว็บ</p>
        </div>
      </section>

      <section className="login-side">{children}</section>
    </div>
  );
}
