// ไอคอนเส้นแบบ lucide ขนาดเดียว stroke เดียวทั้งเว็บ
function Icon({ children, size = 18 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {children}
    </svg>
  );
}

export const IconOverview = (p) => (
  <Icon {...p}><path d="M3 3v18h18" /><path d="M7 15l4-4 3 3 5-6" /></Icon>
);
export const IconSessions = (p) => (
  <Icon {...p}><rect x="4" y="3" width="16" height="18" rx="2" /><path d="M8 8h8M8 12h8M8 16h5" /></Icon>
);
export const IconMenus = (p) => (
  <Icon {...p}><path d="M4 11h16a8 8 0 0 1-16 0z" /><path d="M9 7c0-1.5 1-1.5 1-3M14 7c0-1.5 1-1.5 1-3" /></Icon>
);
export const IconUsers = (p) => (
  <Icon {...p}><circle cx="9" cy="8" r="3.5" /><path d="M2.5 20a6.5 6.5 0 0 1 13 0" /><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18.5 20a6.5 6.5 0 0 0-3-5.5" /></Icon>
);
export const IconLogout = (p) => (
  <Icon {...p}><path d="M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3" /><path d="M10 16l-4-4 4-4M6 12h10" /></Icon>
);
export const IconChevron = (p) => (
  <Icon {...p}><path d="M9 6l6 6-6 6" /></Icon>
);
export const IconBack = (p) => (
  <Icon {...p}><path d="M15 6l-6 6 6 6" /></Icon>
);
export const IconDownload = (p) => (
  <Icon {...p}><path d="M12 4v11M7 10l5 5 5-5M5 20h14" /></Icon>
);
export const IconPlus = (p) => (
  <Icon {...p}><path d="M12 5v14M5 12h14" /></Icon>
);
export const IconCheck = (p) => (
  <Icon {...p}><path d="M5 12.5l4.5 4.5L19 7.5" /></Icon>
);
