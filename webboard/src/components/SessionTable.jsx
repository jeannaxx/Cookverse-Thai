import { Link } from 'react-router-dom';
import { PASS_LINE } from '../data/mock.js';
import { ResultChip } from './RubricBars.jsx';
import { IconChevron } from './Icons.jsx';

export default function SessionTable({ rows }) {
  return (
    <div className="table-wrap">
      <table style={{ minWidth: 600 }}>
        <thead>
          <tr>
            <th scope="col">ผู้เล่น</th>
            <th scope="col">เมนู</th>
            <th scope="col">โหมด</th>
            <th scope="col" className="right">คะแนน</th>
            <th scope="col">ผล</th>
            <th scope="col" className="right">เวลา</th>
            <th scope="col"><span className="sr-only">ดูรายละเอียด</span></th>
          </tr>
        </thead>
        <tbody>
          {rows.map((s) => (
            <tr key={s.id}>
              <td className="strong">{s.player}</td>
              <td>{s.menu}</td>
              <td className="muted">{s.mode}</td>
              <td className={'num right' + (s.score < PASS_LINE ? ' low' : '')}>{s.score}</td>
              <td><ResultChip result={s.result} /></td>
              <td className="muted right nowrap">{s.time}</td>
              <td className="right">
                <Link className="row-link" to={`/sessions/${s.id}`} aria-label={`ดูรายละเอียด ${s.player} ${s.id}`}>
                  ดู <IconChevron size={16} />
                </Link>
              </td>
            </tr>
          ))}
          {rows.length === 0 && (
            <tr>
              <td colSpan={7} className="muted center">ไม่มีรอบการเล่นตามตัวกรองนี้</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
