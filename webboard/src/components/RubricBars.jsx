import { PASS_LINE } from '../data/mock.js';

// แถบคะแนน Rubric: ต่ำกว่า PASS_LINE เป็นสีส้ม ที่เหลือเป็นสีกลาง และมีขีดบอกเส้นผ่าน
export default function RubricBars({ items, withNotes = false }) {
  return (
    <div className={'rubric' + (withNotes ? ' with-notes' : '')}>
      {items.map((r) => {
        const low = r.pct < PASS_LINE;
        return (
          <div key={r.label} className={'rubric-item' + (low ? ' low' : '')}>
            <div className="rubric-line">
              <span>{r.label}</span>
              <span className="rubric-score">{r.text}</span>
            </div>
            <div
              className="bar"
              role="progressbar"
              aria-label={r.label}
              aria-valuenow={r.pct}
              aria-valuemin={0}
              aria-valuemax={100}
            >
              <div className="bar-fill" style={{ width: `${r.pct}%` }} />
              <div className="bar-pass" style={{ left: `${PASS_LINE}%` }} />
            </div>
            {withNotes && <div className="small muted">{r.note}</div>}
          </div>
        );
      })}
    </div>
  );
}

export function ResultChip({ result }) {
  const tone = { 'ผ่านดีมาก': 'good', 'ผ่าน': '', 'ต้องฝึกเพิ่ม': 'warn', 'ไม่ผ่าน': 'bad' }[result] ?? '';
  return (
    <span className={'result ' + tone}>
      <span className="result-dot" aria-hidden="true" />
      {result}
    </span>
  );
}
