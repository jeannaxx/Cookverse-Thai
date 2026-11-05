import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import AuthShell from '../components/AuthShell.jsx';
import { IconCheck } from '../components/Icons.jsx';

// หน้าตั้งรหัสผ่าน ใช้ทั้งตอนรับคำเชิญ (?type=invite) และตอนรีเซ็ต (?type=reset)
// เมื่อต่อ Supabase: ผู้ใช้มาจากลิงก์ในอีเมล session ถูกสร้างแล้ว ให้เรียก supabase.auth.updateUser({ password })
const rules = [
  { test: (p) => p.length >= 8, label: 'อย่างน้อย 8 ตัวอักษร' },
  { test: (p) => /[0-9]/.test(p), label: 'มีตัวเลขอย่างน้อย 1 ตัว' },
  { test: (p) => /[A-Za-z]/.test(p), label: 'มีตัวอักษรภาษาอังกฤษอย่างน้อย 1 ตัว' },
];

export default function SetPassword() {
  const [params] = useSearchParams();
  const isInvite = params.get('type') === 'invite';
  const email = params.get('email') || '';
  const name = params.get('name') || '';

  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);

  const passed = rules.map((r) => r.test(password));

  const submit = (e) => {
    e.preventDefault();
    if (!passed.every(Boolean)) {
      setError('รหัสผ่านยังไม่ตรงตามเงื่อนไข');
      return;
    }
    if (password !== confirm) {
      setError('รหัสผ่านทั้งสองช่องไม่ตรงกัน');
      return;
    }
    setError('');
    setDone(true);
  };

  if (done) {
    return (
      <AuthShell>
        <div className="login-form">
          <div className="done-mark" aria-hidden="true"><IconCheck size={22} /></div>
          <div className="title-block">
            <h2>{isInvite ? 'สร้างบัญชีเรียบร้อย' : 'ตั้งรหัสผ่านใหม่แล้ว'}</h2>
            <p className="muted">ใช้อีเมล{email ? <> <strong className="ink">{email}</strong> </> : ''}และรหัสผ่านใหม่เพื่อเข้าสู่ระบบ</p>
          </div>
          <Link className="btn" to="/login">ไปหน้าเข้าสู่ระบบ</Link>
          <p className="small muted demo-note">โหมดทดลอง: รหัสผ่านยังไม่ถูกบันทึกขึ้นระบบ</p>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell>
      <form className="login-form" onSubmit={submit} noValidate>
        <div className="title-block">
          <h2>{isInvite ? 'ตั้งรหัสผ่านเพื่อเริ่มใช้งาน' : 'ตั้งรหัสผ่านใหม่'}</h2>
          <p className="muted">
            {isInvite
              ? `${name ? `สวัสดี ${name} ` : ''}คุณได้รับเชิญให้ใช้ Cookverse WebBoard ตั้งรหัสผ่านสำหรับเข้าสู่ระบบครั้งต่อไป`
              : 'ตั้งรหัสผ่านใหม่สำหรับบัญชีของคุณ'}
          </p>
        </div>

        {email && (
          <label className="field">
            อีเมล
            <input type="email" value={email} readOnly autoComplete="username" />
          </label>
        )}
        <label className="field">
          รหัสผ่านใหม่
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" autoFocus />
        </label>
        <ul className="pw-rules" aria-label="เงื่อนไขรหัสผ่าน">
          {rules.map((r, i) => (
            <li key={r.label} className={passed[i] ? 'ok' : ''}>
              <span className="pw-dot" aria-hidden="true">{passed[i] && <IconCheck size={12} />}</span>
              {r.label}
              <span className="sr-only">{passed[i] ? 'ผ่าน' : 'ยังไม่ผ่าน'}</span>
            </li>
          ))}
        </ul>
        <label className="field">
          ยืนยันรหัสผ่าน
          <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} autoComplete="new-password" />
        </label>
        {error && <div className="error" role="alert">{error}</div>}
        <button type="submit" className="btn">{isInvite ? 'สร้างบัญชี' : 'บันทึกรหัสผ่านใหม่'}</button>
      </form>
    </AuthShell>
  );
}
