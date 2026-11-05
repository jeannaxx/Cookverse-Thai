import { useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth.jsx';
import AuthShell from '../components/AuthShell.jsx';

export default function Login() {
  const { user, login, loginAsGuest } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  if (user) return <Navigate to="/" replace />;

  const submit = (e) => {
    e.preventDefault();
    if (!email.trim() || !password) {
      setError('กรอกอีเมลและรหัสผ่าน');
      return;
    }
    login(email.trim());
    navigate('/');
  };

  return (
    <AuthShell>
      <form className="login-form" onSubmit={submit} noValidate>
        <div className="title-block">
          <h2>เข้าสู่ระบบ</h2>
          <p className="muted">สำหรับผู้สอนและผู้ดูแลระบบ</p>
        </div>
        <label className="field">
          อีเมล
          <input type="email" placeholder="name@school.ac.th" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="username" />
        </label>
        <div className="field">
          <div className="field-row">
            <label htmlFor="password">รหัสผ่าน</label>
            <Link to="/forgot-password" className="field-link">ลืมรหัสผ่าน</Link>
          </div>
          <input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </div>
        {error && <div className="error" role="alert">{error}</div>}
        <button type="submit" className="btn">เข้าสู่ระบบ</button>
        <div className="or" aria-hidden="true"><span>หรือ</span></div>
        <button
          type="button"
          className="btn secondary"
          onClick={() => {
            loginAsGuest();
            navigate('/');
          }}
        >
          ดูตัวอย่างโดยไม่เข้าสู่ระบบ
        </button>
        <p className="small muted form-foot">
          ยังไม่มีบัญชี? ผู้ดูแลระบบจะส่งอีเมลเชิญให้ ติดต่อแอดมินของโรงเรียน
        </p>
        <p className="small muted demo-note">
          โหมดทดลอง: อีเมลที่มีคำว่า admin จะเข้าเป็นแอดมิน
        </p>
      </form>
    </AuthShell>
  );
}
