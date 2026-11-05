import { PASS_LINE } from '../data/mock.js';

// วงคะแนน: ส่วนโค้งตามคะแนน มีขีดบอกเส้นผ่านที่ PASS_LINE
export default function ScoreRing({ score, size = 128, stroke = 10 }) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const low = score < PASS_LINE;
  const a = (PASS_LINE / 100) * 2 * Math.PI - Math.PI / 2;
  const cx = size / 2;
  const tick = [r - stroke / 2 - 3, r + stroke / 2 + 3].map((d) => [cx + d * Math.cos(a), cx + d * Math.sin(a)]);

  return (
    <div className={'score-ring' + (low ? ' low' : '')} style={{ width: size, height: size }} aria-label={`คะแนน ${score} จาก 100`}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} aria-hidden="true">
        <circle cx={cx} cy={cx} r={r} className="ring-track" strokeWidth={stroke} fill="none" />
        <circle
          cx={cx} cy={cx} r={r} className="ring-arc" strokeWidth={stroke} fill="none" strokeLinecap="round"
          strokeDasharray={`${(score / 100) * c} ${c}`} transform={`rotate(-90 ${cx} ${cx})`}
        />
        <line x1={tick[0][0]} y1={tick[0][1]} x2={tick[1][0]} y2={tick[1][1]} className="ring-tick" />
      </svg>
      <div className="ring-label">
        <div className="big">{score}</div>
        <div className="of">/ 100</div>
      </div>
    </div>
  );
}
