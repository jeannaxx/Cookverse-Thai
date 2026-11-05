import { useState } from 'react';
import { Link } from 'react-router-dom';
import { overview, sessions } from '../data/mock.js';
import RubricBars from '../components/RubricBars.jsx';
import SessionTable from '../components/SessionTable.jsx';

export default function Overview() {
  const [menu, setMenu] = useState('all');
  const [mode, setMode] = useState('all');

  const rows = sessions.filter(
    (s) => (menu === 'all' || s.menu === menu) && (mode === 'all' || s.mode === mode)
  );
  const maxMistake = Math.max(...overview.mistakes.map((m) => parseInt(m.count, 10)));

  return (
    <>
      <header className="page-head">
        <div className="title-block">
          <div className="title-row">
            <h1>ภาพรวมการเรียน</h1>
            <span className="tag">ข้อมูลตัวอย่าง</span>
          </div>
          <p className="muted">ผลจากเกม VR ที่ส่งเข้ามาหลังผู้เล่นเสิร์ฟอาหารให้ป้าแสง</p>
        </div>
        <div className="filters">
          <label className="field">
            เมนู
            <select value={menu} onChange={(e) => setMenu(e.target.value)}>
              <option value="all">ทุกเมนู</option>
              <option value="ผัดกะเพราไก่">ผัดกะเพราไก่</option>
            </select>
          </label>
          <label className="field">
            โหมด
            <select value={mode} onChange={(e) => setMode(e.target.value)}>
              <option value="all">ทั้งหมด</option>
              <option value="Training">Training</option>
              <option value="Challenge">Challenge</option>
            </select>
          </label>
        </div>
      </header>

      <section className="stats card" aria-label="สรุปตัวเลข">
        {overview.kpis.map((k) => (
          <div key={k.label} className="stat">
            <div className="stat-label">{k.label}</div>
            <div className="value">{k.value}</div>
            <div className="small muted">{k.sub}</div>
          </div>
        ))}
      </section>

      <section className="row">
        <div className="col-3">
          <div className="card">
            <div className="card-head">
              <h2>รอบการเล่นล่าสุด</h2>
              <Link to="/sessions" className="text-link">ดูทั้งหมด</Link>
            </div>
            <SessionTable rows={rows} />
          </div>
        </div>

        <div className="col-2">
          <div className="card">
            <div className="card-head">
              <div className="title-block">
                <h2>คะแนนเฉลี่ยตาม Rubric</h2>
                <div className="small muted legend">
                  <span className="legend-swatch" aria-hidden="true" />สีส้ม = ต่ำกว่า 70% ควรสอนเพิ่ม
                </div>
              </div>
            </div>
            <RubricBars items={overview.rubric} />
          </div>

          <div className="card">
            <h2>ข้อผิดพลาดที่พบบ่อย</h2>
            <ol className="ranked">
              {overview.mistakes.map((m) => (
                <li key={m.text} className="ranked-item">
                  <div className="ranked-line">
                    <span>{m.text}</span>
                    <span className="num nowrap">{m.count}</span>
                  </div>
                  <div className="ranked-bar" aria-hidden="true">
                    <div style={{ width: `${(parseInt(m.count, 10) / maxMistake) * 100}%` }} />
                  </div>
                </li>
              ))}
            </ol>
          </div>
        </div>
      </section>
    </>
  );
}
