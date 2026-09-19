import { useEffect, useMemo, useState } from 'react'
import { api, ApiError, type DashboardFilter } from '../api/client'
import type { Branch, DashboardSummary, DangerousInventory, KpiDrilldown, Meta, ProductRank, RevenuePoint } from '../types/api'
import { StatePanel } from '../components/StatePanel'
import { TrendChart } from '../components/TrendChart'

const money = (value: number | null, currency = 'VND') => value === null ? 'N/A' : new Intl.NumberFormat('vi-VN', { style: 'currency', currency, maximumFractionDigits: 0 }).format(value)
const percent = (value: number | null) => value === null ? 'N/A' : `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(value)}%`
const number = (value: number) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 1 }).format(value)
const businessTime = (value: string) => new Intl.DateTimeFormat('vi-VN', {
  dateStyle: 'short', timeStyle: 'medium', timeZone: 'Asia/Ho_Chi_Minh',
}).format(new Date(value))

export function DashboardPage({ openAlerts }: { openAlerts: () => void }) {
  const [filter, setFilter] = useState<DashboardFilter>({ from: '2026-01-01', to: '2026-06-30', branchId: '' })
  const [applied, setApplied] = useState(filter)
  const [branches, setBranches] = useState<Branch[]>([])
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [meta, setMeta] = useState<Meta | null>(null)
  const [revenue, setRevenue] = useState<RevenuePoint[]>([])
  const [inventory, setInventory] = useState<DangerousInventory[]>([])
  const [top, setTop] = useState<ProductRank[]>([])
  const [bottom, setBottom] = useState<ProductRank[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiError | null>(null)
  const [drilldown, setDrilldown] = useState<KpiDrilldown | null>(null)
  const [drillLoading, setDrillLoading] = useState(false)

  const load = async () => {
    setLoading(true); setError(null)
    try {
      const [summaryResult, revenueResult, stockResult, topResult, bottomResult, branchResult] = await Promise.all([
        api.dashboard(applied), api.revenue(applied), api.dangerous(applied), api.topProducts(applied), api.topProducts(applied, true), api.branches(),
      ])
      setSummary(summaryResult.data); setMeta(summaryResult.meta); setRevenue(revenueResult.data); setInventory(stockResult.data)
      setTop(topResult.data); setBottom(bottomResult.data); setBranches(branchResult.data)
    } catch (reason) { setError(reason instanceof ApiError ? reason : new ApiError(0, 'Không thể kết nối API.')) }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [applied])

  const openDrilldown = async (metric: string) => {
    setDrillLoading(true); setDrilldown(null)
    try { setDrilldown((await api.drilldown(metric, applied)).data) }
    catch (reason) { setError(reason instanceof ApiError ? reason : new ApiError(0, 'Không tải được drill-down.')) }
    finally { setDrillLoading(false) }
  }

  const cards = useMemo(() => summary ? [
    ['KPI-01', 'Doanh thu', money(summary.revenue, summary.currency), 'revenue'],
    ['KPI-02', 'GMV', money(summary.gmv, summary.currency), 'gmv'],
    ['KPI-03', 'Tổng đơn hàng', number(summary.totalOrders), 'total-orders'],
    ['KPI-04', 'AOV', money(summary.aov, summary.currency), 'aov'],
    ['KPI-05', 'Lợi nhuận gộp', money(summary.grossProfit, summary.currency), 'gross-profit'],
    ['KPI-05', 'Biên lợi nhuận', percent(summary.grossMarginPercent), 'gross-profit'],
    ['KPI-06', 'Tỷ lệ hủy / hoàn', percent(summary.cancellationReturnRate), 'cancel-return-rate'],
    ['KPI-08', 'Tồn kho nguy hiểm', number(summary.dangerousStockCount), 'dangerous-stock'],
  ] : [], [summary])

  return <>
    <header className="page-header"><div><span className="eyebrow">EXECUTIVE OVERVIEW</span><h1>Dashboard điều hành</h1><p>Toàn cảnh hiệu suất và tín hiệu cần chú ý.</p></div><span className="status-pill">● Dữ liệu demo</span></header>
    <section className="filter-bar">
      <label>Từ ngày<input type="date" value={filter.from} onChange={e => setFilter({ ...filter, from: e.target.value })}/></label>
      <label>Đến ngày<input type="date" value={filter.to} onChange={e => setFilter({ ...filter, to: e.target.value })}/></label>
      <label>Chi nhánh<select value={filter.branchId} onChange={e => setFilter({ ...filter, branchId: e.target.value })}><option value="">Tất cả được phép</option>{branches.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
      <button className="button primary" onClick={() => setApplied(filter)}>Áp dụng</button>
    </section>
    {loading && <StatePanel kind="loading" title="Đang tổng hợp dashboard" message="Hệ thống đang áp dụng Tenant/Branch scope và tải dữ liệu."/>}
    {!loading && error && <StatePanel kind={error.status === 403 ? 'permission' : 'error'} title={error.status === 403 ? 'Không có quyền truy cập' : 'Không tải được dashboard'} message={error.message} onRetry={load}/>} 
    {!loading && !error && summary && <>
      <div className={`provisional-note${meta?.isStale ? ' stale' : ''}`}><strong>{meta?.isStale ? 'Dữ liệu có thể đã cũ' : 'Final GD1 KPI'}</strong><span>{summary.note} Múi giờ nghiệp vụ: UTC+7.{meta && ` Cập nhật: ${businessTime(meta.lastUpdatedAt)}.`}</span></div>
      <section className="kpi-grid">{cards.map(([code, label, value, metric], index) => <button key={`${code}-${label}`} className={`kpi-card accent-${index % 4}`} onClick={() => void openDrilldown(metric)}><span>{code}</span><strong>{value}</strong><p>{label}</p><small>Xem drill-down →</small></button>)}</section>
      <section className="dashboard-grid">
        <article className="panel chart-panel"><div className="panel-head"><div><span className="eyebrow">REVENUE TREND · UTC+7</span><h2>Doanh thu theo ngày</h2></div><span className="preview-badge">IMPLEMENTED</span></div><TrendChart points={revenue.map(x => ({ date: x.date, value: x.amount }))}/></article>
        <article className="panel alert-highlight"><span className="eyebrow">SMART ALERT</span><h2>Tín hiệu cần xử lý</h2><div className="alert-numbers"><div><strong>{summary.openAlerts}</strong><span>Đang mở</span></div><div className="critical"><strong>{summary.urgentAlerts}</strong><span>Khẩn cấp</span></div></div><button className="button dark wide" onClick={openAlerts}>Mở Alert Center</button></article>
      </section>
      <section className="dashboard-grid tables">
        <article className="panel"><div className="panel-head"><div><span className="eyebrow">INVENTORY</span><h2>Tồn kho nguy hiểm</h2></div><span>{inventory.length} SKU</span></div>{inventory.length ? <div className="table-wrap"><table><thead><tr><th>SKU</th><th>Sản phẩm</th><th>OnHand</th><th>Reserved</th><th>Available</th><th>Safety</th></tr></thead><tbody>{inventory.map(x => <tr key={`${x.branchId}-${x.productId}`}><td><code>{x.sku}</code></td><td>{x.productName}</td><td>{x.quantityOnHand}</td><td>{x.reservedQuantity}</td><td><strong className="danger-text">{x.availableQuantity}</strong></td><td>{x.safetyStock}</td></tr>)}</tbody></table></div> : <StatePanel kind="empty" title="Không có tồn kho nguy hiểm" message="Không có SKU chạm ngưỡng trong scope hiện tại."/>}</article>
        <article className="panel"><div className="panel-head"><div><span className="eyebrow">SKU RANKING · VALID QUANTITY</span><h2>Top / Bottom 10 SKU</h2></div><span className="preview-badge">IMPLEMENTED</span></div><div className="rank-columns"><Rank title="Top" rows={top}/><Rank title="Bottom" rows={bottom}/></div></article>
      </section>
    </>}
    {(drillLoading || drilldown) && <div className="modal-backdrop" onClick={() => setDrilldown(null)}><section className="modal" onClick={e => e.stopPropagation()}><button className="modal-close" onClick={() => setDrilldown(null)}>×</button>{drillLoading ? <StatePanel kind="loading" title="Đang tải drill-down" message="Đang lấy dữ liệu KPI theo thời gian."/> : drilldown && <><span className="eyebrow">{drilldown.metricId} · {drilldown.definitionStatus}</span><h2>{drilldown.name}</h2><div className="drill-value">{drilldown.currentValue === null ? 'N/A' : number(drilldown.currentValue)}</div><p className="muted">Phạm vi: {applied.branchId || 'tất cả chi nhánh được cấp'} · {applied.from} → {applied.to} · UTC+7</p><p className="muted">{drilldown.note}</p><TrendChart color="#4c9b7b" points={drilldown.trend}/><button className="button secondary" onClick={() => setDrilldown(null)}>Quay lại Dashboard</button></>}</section></div>}
  </>
}

function Rank({ title, rows }: { title: string; rows: ProductRank[] }) {
  return <div><h3>{title}</h3>{rows.length ? rows.map((row, index) => <div className="rank-row" key={row.productId}><span>{index + 1}</span><div><strong>{row.sku}</strong><small>{row.name}</small></div><b>{number(row.quantity)}</b></div>) : <p className="muted">Không có dữ liệu.</p>}</div>
}
