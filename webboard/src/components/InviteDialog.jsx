import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { IconCheck } from './Icons.jsx';

// ป๊อปอัปเชิญผู้ใช้ — เมื่อต่อ Supabase ให้เรียก Edge Function ฝั่งหลังบ้านที่ใช้
// supabase.auth.admin.inviteUserByEmail(email, { data: { name, role }, redirectTo: `${origin}/set-password?type=invite` })
// (ห้ามเรียก admin API จากหน้าเว็บโดยตรง เพราะต้องใช้ service role key)
export default function InviteDialog({ open, onClose, onInvite, existingEmails }) {
  const ref = useRef(null);
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('ผู้สอน');
  const [error, setError] = useState('');
  const [sent, setSent] = useState(null);

  useEffect(() => {
    const d = ref.current;
    if (!d) return;
    if (open && !d.open) d.showModal();
    if (!open && d.open) d.close();
  }, [open]);

  const reset = () => {
    setName('');
    setEmail('');
    setRole('ผู้สอน');
    setError('');
    setSent(null);
  };

  const close = () => {
    reset();
    onClose();
  };

  const submit = (e) => {
    e.preventDefault();
    const em = email.trim().toLowerCase();
    if (!name.trim()) return setError('กรอกชื่อผู้ใช้');
    if (!/^\S+@\S+\.\S+$/.test(em)) return setError('กรอกอีเมลให้ถูกต้อง');
    if (existingEmails.includes(em)) return setError('อีเมลนี้มีในระบบแล้ว');
    const invite = { name: name.trim(), email: em, role };
    onInvite(invite);
    setSent(invite);
  };

  const demoLink = sent
    ? `/set-password?type=invite&email=${encodeURIComponent(sent.email)}&name=${encodeURIComponent(sent.name)}`
    : '';

  return (
    <dialog ref={ref} className="dialog" onClose={close} aria-labelledby="invite-title">
      {sent ? (
        <div className="dialog-body">
          <div className="done-mark" aria-hidden="true"><IconCheck size={22} /></div>
          <div className="title-block">
            <h2 id="invite-title">ส่งคำเชิญแล้ว</h2>
            <p className="muted">
              <strong className="ink">{sent.name}</strong> ({sent.email}) จะได้รับอีเมลพร้อมลิงก์ตั้งรหัสผ่าน
              สิทธิ์: {sent.role} · ลิงก์ใช้ได้ 7 วัน
            </p>
          </div>
          <div className="notice">
            โหมดทดลอง: ยังไม่ส่งอีเมลจริง เปิดลิงก์ด้านล่างเพื่อดูหน้าที่ผู้ได้รับเชิญจะเห็น
          </div>
          <div className="dialog-actions">
            <button type="button" className="btn secondary" onClick={reset}>เชิญอีกคน</button>
            <Link className="btn" to={demoLink}>เปิดหน้ารับคำเชิญ (ตัวอย่าง)</Link>
          </div>
        </div>
      ) : (
        <form className="dialog-body" onSubmit={submit} noValidate>
          <div className="title-block">
            <h2 id="invite-title">เชิญผู้ใช้</h2>
            <p className="muted">ระบบจะส่งอีเมลเชิญ ผู้ใช้กดลิงก์แล้วตั้งรหัสผ่านเอง</p>
          </div>
          <label className="field">
            ชื่อ
            <input value={name} onChange={(e) => setName(e.target.value)} placeholder="ชื่อที่แสดงในระบบ" autoFocus />
          </label>
          <label className="field">
            อีเมล
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="name@school.ac.th" />
          </label>
          <fieldset className="field role-pick">
            <legend>สิทธิ์</legend>
            {[
              ['ผู้สอน', 'ดูผลการเล่น เขียนบันทึกถึงผู้เล่น'],
              ['แอดมิน', 'ทำได้ทุกอย่าง รวมถึงแก้น้ำหนัก Rubric และเชิญผู้ใช้'],
            ].map(([r, desc]) => (
              <label key={r} className={'role-option' + (role === r ? ' selected' : '')}>
                <input type="radio" name="role" value={r} checked={role === r} onChange={() => setRole(r)} />
                <span>
                  <span className="role-name">{r}</span>
                  <span className="small muted">{desc}</span>
                </span>
              </label>
            ))}
          </fieldset>
          {error && <div className="error" role="alert">{error}</div>}
          <div className="dialog-actions">
            <button type="button" className="btn secondary" onClick={close}>ยกเลิก</button>
            <button type="submit" className="btn">ส่งคำเชิญ</button>
          </div>
        </form>
      )}
    </dialog>
  );
}
