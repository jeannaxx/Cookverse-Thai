import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../auth.jsx';
import { ingredientsByMenu, menus, rubricWeights, users } from '../data/mock.js';
import { IconPlus } from '../components/Icons.jsx';
import InviteDialog from '../components/InviteDialog.jsx';

export default function Menus() {
  const { user } = useAuth();
  const isAdmin = user.role === 'admin';
  const { hash } = useLocation();
  const [picked, setPicked] = useState('krapow');
  const [weights, setWeights] = useState(rubricWeights);
  const [userList, setUserList] = useState(users);
  const [inviteOpen, setInviteOpen] = useState(false);

  useEffect(() => {
    if (hash) document.getElementById(hash.slice(1))?.scrollIntoView({ behavior: 'smooth' });
  }, [hash]);

  const current = menus.find((m) => m.id === picked);
  const ingredients = ingredientsByMenu[picked] ?? [];
  const total = weights.reduce((sum, w) => sum + (Number(w.v) || 0), 0);

  return (
    <>
      <header className="page-head">
        <div className="title-block">
          <h1>เมนูอาหาร</h1>
          <p className="muted">เมนูที่เปิดในเกม AI ป้าแสงจะแนะนำได้เฉพาะเมนูที่เปิดใช้ในหน้านี้</p>
        </div>
        {isAdmin && <button type="button" className="btn"><IconPlus size={16} />เพิ่มเมนู</button>}
      </header>

      <section className="menu-grid" aria-label="รายการเมนู">
        {menus.map((m) => (
          <button
            key={m.id}
            type="button"
            className={'menu-card' + (m.id === picked ? ' selected' : '')}
            aria-pressed={m.id === picked}
            onClick={() => setPicked(m.id)}
          >
            <span className="name">{m.name}</span>
            <span className="small muted">{m.meta}</span>
            <span className={'result' + (m.live ? ' good' : '')}><span className="result-dot" aria-hidden="true" />{m.status}</span>
          </button>
        ))}
      </section>

      <section className="row">
        <div className="col-3">
          <div className="card">
            <div className="card-head">
              <h2>วัตถุดิบ · {current.name}</h2>
              <span className="small muted mono">{current.missionId}</span>
            </div>
            <div className="table-wrap">
              <table style={{ minWidth: 560 }}>
                <thead>
                  <tr>
                    <th scope="col">ชื่อในเกม</th>
                    <th scope="col">ingredientId</th>
                    <th scope="col">จำนวน</th>
                    <th scope="col">แหล่งที่มา</th>
                  </tr>
                </thead>
                <tbody>
                  {ingredients.map((g) => (
                    <tr key={g.id}>
                      <td className="strong">{g.name}</td>
                      <td className="mono muted">{g.id}</td>
                      <td className="nowrap">{g.qty}</td>
                      <td>{g.src}</td>
                    </tr>
                  ))}
                  {ingredients.length === 0 && (
                    <tr>
                      <td colSpan={4} className="muted center">ยังไม่มีวัตถุดิบ รอยืนยันสูตรกับอาจารย์</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div className="col-2">
          <div className="card">
            <h2>น้ำหนักคะแนน Rubric</h2>
            {weights.map((w, i) => (
              <label key={w.label} className="weight-row">
                <span>{w.label}</span>
                <input
                  type="number"
                  min={0}
                  max={100}
                  value={w.v}
                  disabled={!isAdmin}
                  onChange={(e) =>
                    setWeights(weights.map((x, j) => (j === i ? { ...x, v: e.target.value } : x)))
                  }
                />
              </label>
            ))}
            <div className={'total' + (total === 100 ? '' : ' off')}>
              <span>รวม</span>
              <span>{total}{total === 100 ? '' : ' (ต้องเท่ากับ 100)'}</span>
            </div>
            <div className="small muted">ไก่ดิบหรืออาหารไหม้ = ไม่ผ่าน ไม่ว่าคะแนนรวมเท่าไร</div>
          </div>
        </div>
      </section>

      {isAdmin && (
        <section id="users" className="card">
          <div className="card-head">
            <h2>ผู้ใช้และสิทธิ์</h2>
            <button type="button" className="btn secondary" onClick={() => setInviteOpen(true)}><IconPlus size={16} />เชิญผู้ใช้</button>
          </div>
          <div className="table-wrap">
            <table style={{ minWidth: 560 }}>
              <thead>
                <tr>
                  <th scope="col">ชื่อ</th>
                  <th scope="col">อีเมล</th>
                  <th scope="col">สิทธิ์</th>
                  <th scope="col">สถานะ</th>
                </tr>
              </thead>
              <tbody>
                {userList.map((u) => (
                  <tr key={u.name + u.email}>
                    <td className="strong">{u.name}</td>
                    <td className="muted">{u.email}</td>
                    <td>{u.role}</td>
                    <td>
                      {u.pending
                        ? <span className="result warn"><span className="result-dot" aria-hidden="true" />{u.status}</span>
                        : u.status}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <InviteDialog
            open={inviteOpen}
            onClose={() => setInviteOpen(false)}
            existingEmails={userList.map((u) => u.email.toLowerCase())}
            onInvite={(inv) => setUserList((list) => [...list, { ...inv, status: 'รอตอบรับคำเชิญ', pending: true }])}
          />
        </section>
      )}
    </>
  );
}
