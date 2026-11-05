import { useState } from 'react';
import { useAuth } from '../auth.jsx';
import { Link, Navigate, useParams } from 'react-router-dom';
import { sessionDetail as d, sessions } from '../data/mock.js';
import RubricBars, { ResultChip } from '../components/RubricBars.jsx';
import ScoreRing from '../components/ScoreRing.jsx';
import { IconBack, IconCheck, IconDownload } from '../components/Icons.jsx';

const okMark = { yes: ['✓', 'ok', 'ถูกต้อง'], no: ['✗', 'no', 'ผิด'], neutral: ['−', 'neutral', 'ไม่นับ'] };

export default function SessionDetail() {
  const { id } = useParams();
  const s = sessions.find((x) => x.id === id);
  const isGuest = useAuth().user.role === 'guest';
  const [note, setNote] = useState('');
  const [saved, setSaved] = useState(false);

  if (!s) return <Navigate to="/sessions" replace />;

  const downloadJson = () => {
    const blob = new Blob([JSON.stringify({ session: s, log: d.log }, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${s.id}-action-log.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <>
      <Link to="/" className="back"><IconBack size={16} />กลับไปภาพรวม</Link>

      <header className="card session-head">
        <div className="session-title">
          <div className="title-row">
            <h1>{s.player} · {s.menu}</h1>
            <span className="tag">ข้อมูลตัวอย่าง</span>
          </div>
          <div className="meta">
            <span>โหมด {s.mode}</span>
            <span>รหัสรอบ <span className="mono">{s.id}</span></span>
            {d.meta.slice(1).map((m) => <span key={m}>{m}</span>)}
            <span>ใช้เวลา {s.time}</span>
          </div>
        </div>
        <div className="score">
          <ScoreRing score={s.score} />
          <div className="score-text">
            <ResultChip result={s.result} />
            <span className="small muted">{d.verdict}</span>
          </div>
        </div>
      </header>

      <section className="row">
        <div className="col-3">
          <div className="card">
            <div className="title-block">
              <h2>คะแนนตาม Recipe Rubric v1</h2>
              <div className="small muted legend">
                <span className="legend-swatch" aria-hidden="true" />สีส้ม = ต่ำกว่า 70% ควรฝึกเพิ่ม
              </div>
            </div>
            <RubricBars items={d.rubric} withNotes />
          </div>

          <div className="card">
            <h2>ลำดับขั้นภารกิจ</h2>
            <ol className="steps">
              {d.steps.map((st, i) => (
                <li key={st.name} className={'step' + (st.ok ? '' : ' warn')}>
                  <span className="step-dot" aria-hidden="true">{st.ok ? <IconCheck size={14} /> : i + 1}</span>
                  <span className="step-name">{st.name}</span>
                  <span className="step-time mono nowrap">{st.time}</span>
                  <span className="step-status nowrap">{st.ok ? 'ผ่าน' : 'มีข้อผิดพลาด'}</span>
                </li>
              ))}
            </ol>
          </div>
        </div>

        <div className="col-2">
          <div className="aunt">
            <div className="aunt-head">
              <div className="aunt-avatar" aria-hidden="true">แ</div>
              <div>
                <h2>ป้าแสงประเมิน</h2>
                <div className="sub">คำแนะนำจาก AI · คะแนนมาจากระบบกฎ</div>
              </div>
            </div>
            <p className="aunt-quote">{d.aunt.message}</p>
            <div className="aunt-tips">
              <div className="label">สิ่งที่ควรฝึกต่อ</div>
              <ul>
                {d.aunt.tips.map((t) => <li key={t}>{t}</li>)}
              </ul>
            </div>
            <div className="sub aunt-foot">สร้างโดย Gemini · ถ้า AI ไม่ตอบ ระบบจะแสดงข้อความสำรองจากกฎแทน</div>
          </div>

          <div className="card">
            <h2>ข้อผิดพลาดในรอบนี้</h2>
            <ul className="plain-list">
              {d.errors.map((e) => (
                <li key={e.text} className="plain-item">
                  <span>{e.text}</span>
                  <span className="num minus nowrap">{e.minus}</span>
                </li>
              ))}
            </ul>
          </div>

          <form
            className="card"
            onSubmit={(e) => {
              e.preventDefault();
              setSaved(true);
            }}
          >
            <div className="title-block">
              <h2>บันทึกของผู้สอน</h2>
              <label htmlFor="note" className="small muted">ความเห็นถึงผู้เล่น</label>
            </div>
            <textarea
              id="note"
              rows={4}
              placeholder={isGuest ? 'ผู้เยี่ยมชมพิมพ์ความเห็นไม่ได้' : 'พิมพ์ความเห็นของผู้สอน'}
              disabled={isGuest}
              value={note}
              onChange={(e) => {
                setNote(e.target.value);
                setSaved(false);
              }}
            />
            <div className="form-actions">
              <button type="submit" className="btn" disabled={!note.trim()}>บันทึก</button>
              {saved && <span className="small muted" role="status">บันทึกในเครื่องนี้แล้ว (ยังไม่ส่งขึ้นระบบ)</span>}
            </div>
          </form>
        </div>
      </section>

      <section className="card">
        <div className="card-head">
          <h2>Action Log</h2>
          <button type="button" className="btn secondary" onClick={downloadJson}>
            <IconDownload size={16} />ดาวน์โหลด JSON
          </button>
        </div>
        <div className="table-wrap">
          <table className="compact" style={{ minWidth: 720 }}>
            <thead>
              <tr>
                <th scope="col">เวลา</th>
                <th scope="col">action</th>
                <th scope="col">วัตถุดิบ</th>
                <th scope="col">stepId</th>
                <th scope="col">สถานะ</th>
                <th scope="col" className="center">ถูกต้อง</th>
              </tr>
            </thead>
            <tbody>
              {d.log.map((l) => {
                const [mark, tone, label] = okMark[l.ok];
                return (
                  <tr key={l.t} className={tone === 'no' ? 'row-wrong' : ''}>
                    <td className="mono muted nowrap">{l.t}</td>
                    <td className="mono">{l.a}</td>
                    <td>{l.i}</td>
                    <td className="mono muted">{l.s}</td>
                    <td>{l.st}</td>
                    <td className={'center mark ' + tone}>
                      <span aria-hidden="true">{mark}</span>
                      <span className="sr-only">{label}</span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );
}
