// กระทะ + ไอร้อน
export default function Logo({ size = 24, color = '#3A1D0E' }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="10" cy="13" r="7" />
      <path d="M17 13h5" />
      <path d="M7 6c0-2 2-2 2-4M11 6c0-2 2-2 2-4" />
    </svg>
  );
}
