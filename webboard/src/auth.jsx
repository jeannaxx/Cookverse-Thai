import { createContext, useContext, useState } from 'react';

// เข้าสู่ระบบแบบจำลอง — ใช้จนกว่าจะต่อ Supabase Auth
// อีเมลที่มีคำว่า "admin" = แอดมิน, อื่นๆ = ผู้สอน
// ผู้เยี่ยมชม (guest) = ดูข้อมูลตัวอย่างได้อย่างเดียว ไม่ต้องเข้าสู่ระบบ — เมื่อต่อ Supabase ให้โหมดนี้อ่านจาก mock.js ต่อไป
const AuthContext = createContext(null);
const KEY = 'cookverse-webboard-user';

function load() {
  try {
    return JSON.parse(sessionStorage.getItem(KEY));
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(load);

  const save = (next) => {
    try { sessionStorage.setItem(KEY, JSON.stringify(next)); } catch { /* ไม่มี storage ก็ใช้ได้ */ }
    setUser(next);
  };

  const login = (email) => {
    const isAdmin = email.toLowerCase().includes('admin');
    save({
      email,
      name: isAdmin ? '[ชื่อแอดมิน]' : '[ชื่ออาจารย์]',
      role: isAdmin ? 'admin' : 'teacher',
    });
  };

  const loginAsGuest = () => save({ email: '', name: 'ผู้เยี่ยมชม', role: 'guest' });

  const logout = () => {
    try { sessionStorage.removeItem(KEY); } catch { /* ignore */ }
    setUser(null);
  };

  return <AuthContext.Provider value={{ user, login, loginAsGuest, logout }}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
