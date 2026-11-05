import { useState } from 'react';
import { sessions } from '../data/mock.js';
import SessionTable from '../components/SessionTable.jsx';

export default function Sessions() {
  const [q, setQ] = useState('');
  const [mode, setMode] = useState('all');

  const rows = sessions.filter(
    (s) => (mode === 'all' || s.mode === mode) && (q === '' || s.player.includes(q) || s.id.includes(q))
  );

  return (
    <>
      <header className="page-head">
        <div className="title-block">
          <div className="title-row">
            <h1>รอบการเล่น</h1>
            <span className="tag">ข้อมูลตัวอย่าง</span>
          </div>
          <p className="muted">ทุกรอบที่ส่งเข้ามาจากแว่นและเว็บ เรียงจากล่าสุด</p>
        </div>
        <div className="filters">
          <label className="field">
            ค้นหา
            <input type="search" placeholder="ชื่อผู้เล่นหรือรหัสรอบ" value={q} onChange={(e) => setQ(e.target.value)} />
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

      <div className="card">
        <SessionTable rows={rows} />
      </div>
    </>
  );
}
