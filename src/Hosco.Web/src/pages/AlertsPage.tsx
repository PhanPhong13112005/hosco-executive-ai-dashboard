import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { AlertDetail, AlertItem, AlertPage, Branch } from '../types/api'
import { StatePanel } from '../components/StatePanel'

export function AlertsPage() {
  const [data, setData] = useState<AlertPage | null>(null)
  const [branches, setBranches] = useState<Branch[]>([])
  const [filter, setFilter] = useState({ branchId: '', severity: '', status: '' })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiError | null>(null)
  const [detail, setDetail] = useState<AlertDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [actionBusy, setActionBusy] = useState(false)

  const load = async () => {
    setLoading(true); setError(null)
    try {
      const [alerts, allowedBranches] = await Promise.all([api.alerts(filter), api.branches()])
      setData(alerts); setBranches(allowedBranches.data)
    } catch (reason) { setError(reason instanceof ApiError ? reason : new ApiError(0, 'Không thể kết nối API.')) }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [filter.branchId, filter.severity, filter.status])

  const open = async (id: string) => {
    setDetailLoading(true)
    try { setDetail(await api.alert(id)) }
    catch (reason) { setError(reason instanceof ApiError ? reason : new ApiError(0, 'Không tải được Alert.')) }
    finally { setDetailLoading(false) }
  }
  const transition = async (action: 'acknowledge' | 'resolve') => {
    if (!detail) return
    setActionBusy(true)
    try { setDetail(action === 'acknowledge' ? await api.acknowledge(detail.id) : await api.resolve(detail.id)); await load() }
    catch (reason) { setError(reason instanceof ApiError ? reason : new ApiError(0, 'Không cập nhật được Alert.')) }
    finally { setActionBusy(false) }
  }

  return <>
    <header className="page-header"><div><span className="eyebrow">OPERATIONAL AWARENESS</span><h1>Smart Alert Center</h1><p>Ưu tiên tín hiệu, xác nhận và đóng vòng xử lý.</p></div><button className="button secondary" onClick={() => void load()}>Làm mới</button></header>
    {data && <section className="summary-strip"><Summary label="Open" value={data.summary.open}/><Summary label="Urgent" value={data.summary.urgent} critical/><Summary label="Handled" value={data.summary.handled}/><Summary label="Resolved" value={data.summary.resolved}/></section>}
    <section className="filter-bar compact">
      <label>Trạng thái<select value={filter.status} onChange={e => setFilter({ ...filter, status: e.target.value })}><option value="">Tất cả</option><option>Open</option><option>Acknowledged</option><option>Resolved</option></select></label>
      <label>Mức độ<select value={filter.severity} onChange={e => setFilter({ ...filter, severity: e.target.value })}><option value="">Tất cả</option><option>Info</option><option>Warning</option><option>Critical</option></select></label>
      <label>Chi nhánh<select value={filter.branchId} onChange={e => setFilter({ ...filter, branchId: e.target.value })}><option value="">Tất cả được phép</option>{branches.map(x => <option value={x.id} key={x.id}>{x.name}</option>)}</select></label>
    </section>
    {loading && <StatePanel kind="loading" title="Đang tải Alert" message="Đang áp dụng bộ lọc và phạm vi truy cập."/>}
    {!loading && error && <StatePanel kind={error.status === 403 ? 'permission' : 'error'} title={error.status === 403 ? 'Không có quyền truy cập' : 'Không tải được Alert Center'} message={error.message} onRetry={load}/>} 
    {!loading && !error && data?.items.length === 0 && <StatePanel kind="empty" title="Không có Alert phù hợp" message="Thử thay đổi bộ lọc hoặc khoảng phạm vi chi nhánh."/>}
    {!loading && !error && data && data.items.length > 0 && <section className="panel alert-list"><div className="alert-list-head"><span>Mức độ</span><span>Nội dung</span><span>Chi nhánh</span><span>Phát hiện</span><span>Trạng thái</span></div>{data.items.map(item => <AlertRow key={item.id} item={item} onClick={() => void open(item.id)}/>)}</section>}
    {(detailLoading || detail) && <div className="drawer-backdrop" onClick={() => setDetail(null)}><aside className="detail-drawer" onClick={e => e.stopPropagation()}><button className="modal-close" onClick={() => setDetail(null)}>×</button>{detailLoading ? <StatePanel kind="loading" title="Đang tải Alert" message="Đang kiểm tra phạm vi truy cập."/> : detail && <><div className="detail-title"><span className={`severity ${detail.severity.toLowerCase()}`}>{detail.severity}</span><span>{detail.ruleCode}</span></div><h2>{detail.title}</h2><p>{detail.message}</p><dl><dt>Trạng thái</dt><dd>{detail.status}</dd><dt>Chi nhánh</dt><dd>{detail.branchId ?? 'Toàn Tenant'}</dd><dt>Giá trị phát hiện</dt><dd>{detail.detectedValue ?? '—'}</dd><dt>Ngưỡng cấu hình</dt><dd>{detail.thresholdValue ?? '—'}</dd><dt>Thời điểm</dt><dd>{new Date(detail.detectedAt).toLocaleString('vi-VN')}</dd><dt>Dedup key</dt><dd><code>{detail.dedupKey}</code></dd></dl><details><summary>Detection context</summary><pre>{prettyJson(detail.contextJson)}</pre></details><div className="drawer-actions">{detail.status === 'Open' && <button disabled={actionBusy} className="button secondary" onClick={() => void transition('acknowledge')}>Đánh dấu đã xem</button>}{detail.status !== 'Resolved' && <button disabled={actionBusy} className="button primary" onClick={() => void transition('resolve')}>Đánh dấu đã xử lý</button>}</div></>}</aside></div>}
  </>
}

function Summary({ label, value, critical = false }: { label: string; value: number; critical?: boolean }) { return <div className={critical ? 'critical' : ''}><span>{label}</span><strong>{value}</strong></div> }
function AlertRow({ item, onClick }: { item: AlertItem; onClick: () => void }) { return <button className="alert-row" onClick={onClick}><span><i className={`severity-dot ${item.severity.toLowerCase()}`}/>{item.severity}</span><span><strong>{item.title}</strong><small>{item.ruleCode}</small></span><code>{item.branchId?.slice(0, 8) ?? 'Tenant'}</code><span>{new Date(item.detectedAt).toLocaleString('vi-VN')}</span><span className={`status ${item.status.toLowerCase()}`}>{item.status}</span></button> }
function prettyJson(value: string) { try { return JSON.stringify(JSON.parse(value), null, 2) } catch { return value } }
