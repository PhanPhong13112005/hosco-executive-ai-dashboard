export function TrendChart({ points, color = '#ff8b5c' }: { points: { date: string; value: number }[]; color?: string }) {
  if (!points.length) return <div className="chart-empty">Không có dữ liệu trong khoảng đã chọn.</div>
  const width = 720, height = 210, pad = 18
  const max = Math.max(...points.map(x => x.value), 1)
  const coords = points.map((point, index) => ({
    x: pad + (index * (width - pad * 2)) / Math.max(points.length - 1, 1),
    y: height - pad - (point.value / max) * (height - pad * 2),
  }))
  const line = coords.map((point, index) => `${index ? 'L' : 'M'} ${point.x} ${point.y}`).join(' ')
  const area = `${line} L ${coords.at(-1)!.x} ${height - pad} L ${coords[0].x} ${height - pad} Z`
  return <div className="trend-chart">
    <svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Biểu đồ xu hướng">
      <defs><linearGradient id="chart-fill" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor={color} stopOpacity=".28"/><stop offset="1" stopColor={color} stopOpacity="0"/></linearGradient></defs>
      {[.25, .5, .75, 1].map(level => <line key={level} x1={pad} x2={width-pad} y1={height-pad-level*(height-pad*2)} y2={height-pad-level*(height-pad*2)} className="grid-line" />)}
      <path d={area} fill="url(#chart-fill)" />
      <path d={line} fill="none" stroke={color} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
      {coords.filter((_, i) => i === 0 || i === coords.length - 1).map((point, i) => <circle key={i} cx={point.x} cy={point.y} r="4" fill={color} />)}
    </svg>
    <div className="chart-axis"><span>{new Date(points[0].date).toLocaleDateString('vi-VN')}</span><span>{new Date(points.at(-1)!.date).toLocaleDateString('vi-VN')}</span></div>
  </div>
}

