import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth.jsx';
import Logo from './Logo.jsx';
import { IconLogout, IconMenus, IconOverview, IconSessions, IconUsers } from './Icons.jsx';

const links = [
  { to: '/', label: 'ภาพรวม', end: true, icon: IconOverview },
  { to: '/sessions', label: 'รอบการเล่น', icon: IconSessions },
  { to: '/menus', label: 'เมนูอาหาร', icon: IconMenus },
  { to: '/menus#users', label: 'ผู้ใช้และสิทธิ์', adminOnly: true, icon: IconUsers },
];

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const isAdmin = user.role === 'admin';
  const isGuest = user.role === 'guest';
  const roleLabel = isAdmin ? 'ผู้ดูแลระบบ' : isGuest ? 'ดูได้อย่างเดียว' : 'ผู้สอน';
  const signOut = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="shell">
      <nav className="sidebar" aria-label="เมนูหลัก">
        <div className="brand">
          <div className="brand-tile"><Logo size={20} /></div>
          <div className="brand-text">
            <div className="brand-name">Cookverse Thai</div>
            <div className="brand-sub">WebBoard {isAdmin ? 'แอดมิน' : isGuest ? 'ตัวอย่าง' : 'ผู้สอน'}</div>
          </div>
        </div>

        <div className="nav-links">
          {links
            .filter((l) => !l.adminOnly || isAdmin)
            .map(({ to, label, end, icon: I }) => (
              <NavLink
                key={to}
                to={to}
                end={end}
                className={({ isActive }) => 'nav-link' + (isActive && !to.includes('#') ? ' active' : '')}
              >
                <I />
                {label}
              </NavLink>
            ))}
        </div>

        <div className="user-card">
          <div className="avatar" aria-hidden="true">{user.name.replace(/[[\]]/g, '').slice(0, 1)}</div>
          <div className="user-text">
            <div className="user-name">{user.name}</div>
            <div className="role">{roleLabel}</div>
          </div>
          <button
            type="button"
            className="icon-btn"
            aria-label={isGuest ? 'ออกจากโหมดตัวอย่าง' : 'ออกจากระบบ'}
            title={isGuest ? 'ออกจากโหมดตัวอย่าง' : 'ออกจากระบบ'}
            onClick={signOut}
          >
            <IconLogout />
          </button>
        </div>
      </nav>

      <main className="main">
        {isGuest && (
          <div className="guest-bar" role="note">
            <span className="guest-dot" aria-hidden="true" />
            <span>โหมดผู้เยี่ยมชม · ดูข้อมูลตัวอย่างได้อย่างเดียว แก้ไขหรือบันทึกไม่ได้</span>
            <button type="button" className="text-btn" onClick={signOut}>เข้าสู่ระบบ</button>
          </div>
        )}
        <Outlet />
      </main>
    </div>
  );
}
