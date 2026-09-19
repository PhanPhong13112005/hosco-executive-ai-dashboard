import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { AlertRule } from '../types/api'
import { StatePanel } from '../components/StatePanel'

export function RulesPage() {
  const [rules, setRules] = useState<AlertRule[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiError | null>(null)
  const [saving, setSaving] = useState('')
  const load = async () => {
    setLoading(true); setError(null)
    try { setRules(await api.rules()) }
    catch (reason) { setError(reason instanceof ApiError ? reason : new ApiError(0, 'Không tải được cấu hình.')) }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [])
  const updateLocal = (id: string, patch: Partial<AlertRule>) => setRules(rows => rows.map(row => row.id === id ? { ...row, ...patch } : row))
  const save = async (rule: AlertRule) => {
    setSaving(rule.id); setError(null)
    try {
      updateLocal(rule.id, await api.updateRule(rule.id, {
        isEnabled: rule.isEnabled, severity: rule.severity, threshold: rule.threshold,
        baseline: rule.baseline, windowMinutes: rule.windowMinutes,
        cooldownMinutes: rule.cooldownMinutes, configJson: rule.configJson,
      }))
    } catch (reason) { setError(reason instanceof ApiError ? reason : new ApiError(0, 'Không lưu được cấu hình.')) }
    finally { setSaving('') }
  }
  return <>
    <header className="page-header"><div><span className="eyebrow">ALERT GOVERNANCE</span><h1>Cấu hình Alert</h1><p>Năm rule MVP theo Alert Catalog; giá trị số là BA đề xuất và có thể cấu hình.</p></div><span className="preview-badge">BA ĐỀ XUẤT · CONFIGURABLE</span></header>
    <div className="provisional-note"><strong>BA đề xuất / configurable</strong><span>Threshold, baseline, window và cooldown không phải business truth bất biến. Mọi thay đổi được audit.</span></div>
    {loading && <StatePanel kind="loading" title="Đang tải rule" message="Đang kiểm tra quyền và Tenant/Branch scope."/>}
    {!loading && error && <StatePanel kind={error.status === 403 ? 'permission' : 'error'} title={error.status === 403 ? 'Bạn không có quyền cấu hình' : 'Không tải được rule'} message={error.message} onRetry={load}/>} 
    {!loading && !error && rules.length === 0 && <StatePanel kind="empty" title="Chưa có rule" message="Chưa có rule trong phạm vi chi nhánh được cấp."/>}
    {!loading && rules.length > 0 && <section className="rule-grid">{rules.map(rule => <article className="rule-card" key={rule.id}>
      <div className="rule-top"><span className={`rule-code ${rule.severity.toLowerCase()}`}>{rule.code}</span><label className="switch"><input type="checkbox" checked={rule.isEnabled} onChange={e => updateLocal(rule.id, { isEnabled: e.target.checked })}/><span/></label></div>
      <h2>{rule.name}</h2><p>{rule.description}</p>
      <div className="rule-fields">
        <label>Severity mặc định<select value={rule.severity} onChange={e => updateLocal(rule.id, { severity: e.target.value as AlertRule['severity'] })}><option>Medium</option><option>High</option><option>Critical</option></select></label>
        <label>Threshold<input type="number" min="0" value={rule.threshold ?? ''} onChange={e => updateLocal(rule.id, { threshold: e.target.value === '' ? null : Number(e.target.value) })}/></label>
        <label>Baseline / ngưỡng 2<input type="number" min="0" value={rule.baseline ?? ''} onChange={e => updateLocal(rule.id, { baseline: e.target.value === '' ? null : Number(e.target.value) })}/></label>
        <label>Window (phút)<input type="number" min="1" max="43200" value={rule.windowMinutes} onChange={e => updateLocal(rule.id, { windowMinutes: Number(e.target.value) })}/></label>
        <label>Cooldown (phút)<input type="number" min="0" max="43200" value={rule.cooldownMinutes} onChange={e => updateLocal(rule.id, { cooldownMinutes: Number(e.target.value) })}/></label>
      </div>
      <label>Rule-specific config (JSON)<textarea value={rule.configJson} onChange={e => updateLocal(rule.id, { configJson: e.target.value })}/></label>
      <footer><span>{rule.baStatus} · branch {rule.branchId?.slice(0, 8)}</span><button className="button secondary" disabled={saving === rule.id} onClick={() => void save(rule)}>{saving === rule.id ? 'Đang lưu…' : 'Lưu cấu hình'}</button></footer>
    </article>)}</section>}
  </>
}
