import { useState } from 'react';
import { Link } from 'react-router-dom';
import AuthShell from '../components/AuthShell.jsx';
import { IconBack } from '../components/Icons.jsx';

// ลืมรหัสผ่าน — เมื่อต่อ Supabase ให้เรียก supabase.auth.resetPasswordForEmail(email, { redirectTo: `${origin}/set-password?type=reset` })
export default function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [sentTo, setSentTo] = useState('');

  const submit = (e) => {
    e.preventDefault();
    if (!/^\S+@\S+\.\S+$/.test(email.trim())) {
      setError('กรอกอีเมลให้ถูกต้อง');
      return;
    }
    setError('');
    setSentTo(email.trim());
  };

  return (
    <AuthShell>
      <div className="login-form">
        <Link to="/login" className="back"><IconBack size={16} />กลับไปเข้าสู่ระบบ</Link>

        {sentTo ? (
          <>
            <div className="title-block">
              <h2>ตรวจสอบอีเมลของคุณ</h2>
              <p className="muted">
                ถ้ามีบัญชีของ <strong className="ink">{sentTo}</strong> ในระบบ เราส่งลิงก์ตั้งรหัสผ่านใหม่ไปแล้ว ลิงก์ใช้ได้ 1 ชั่วโมง
              </p>
            </div>
            <div className="notice">
              โหมดทดลอง: ยังไม่ส่งอีเมลจริง กดลิงก์ด้านล่างเพื่อดูหน้าที่ผู้ใช้จะเห็นหลังกดลิงก์ในอีเมล
            </div>
            <Link className="btn" to={`/set-password?type=reset&email=${encodeURIComponent(sentTo)}`}>
              เปิดหน้าตั้งรหัสผ่านใหม่ (ตัวอย่าง)
            </Link>
            <button type="button" className="btn secondary" onClick={() => setSentTo('')}>
              ส่งอีกครั้ง
            </button>
          </>
        ) : (
          <form className="form-stack" onSubmit={submit} noValidate>
            <div className="title-block">
              <h2>ลืมรหัสผ่าน</h2>
              <p className="muted">กรอกอีเมลที่ใช้เข้าสู่ระบบ เราจะส่งลิงก์ตั้งรหัสผ่านใหม่ให้</p>
            </div>
            <label className="field">
              อีเมล
              <input type="email" placeholder="name@school.ac.th" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" autoFocus />
            </label>
            {error && <div className="error" role="alert">{error}</div>}
            <button type="submit" className="btn">ส่งลิงก์ตั้งรหัสผ่าน</button>
          </form>
        )}
      </div>
    </AuthShell>
  );
}
