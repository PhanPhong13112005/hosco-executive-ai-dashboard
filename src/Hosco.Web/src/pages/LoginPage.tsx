import { useState } from 'react'
import { auth, ApiError } from '../api/client'

export function LoginPage({ onSuccess }: { onSuccess: () => void }) {
  const [email, setEmail] = useState('owner@hosco.local')
  const [password, setPassword] = useState('HoscoDemo!2026')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const submit = async (event: React.FormEvent) => {
    event.preventDefault(); setBusy(true); setError('')
    try { await auth.login(email, password); onSuccess() }
    catch (reason) { setError(reason instanceof ApiError ? reason.message : 'Không thể kết nối API.') }
    finally { setBusy(false) }
  }

  return <main className="login-page">
    <section className="login-story">
      <div className="brand light"><span className="brand-mark">H</span><div><strong>HOSCO</strong><small>EXECUTIVE CONSOLE</small></div></div>
      <div><span className="eyebrow">GD3 · SMART RETAIL OPERATIONS</span><h1>Thấy sớm.<br/>Quyết định nhanh.</h1><p>Một không gian điều hành tập trung cho chủ cửa hàng, quản lý chi nhánh và quản lý chuỗi.</p></div>
      <small>Dữ liệu demo · KPI Final GD1 · Alert có thể cấu hình</small>
    </section>
    <section className="login-panel">
      <form onSubmit={submit}>
        <span className="eyebrow">WELCOME BACK</span><h2>Đăng nhập HOSCO</h2><p>Sử dụng tài khoản demo theo phạm vi vai trò.</p>
        <label>Email<input value={email} onChange={e => setEmail(e.target.value)} type="email" autoComplete="username" required /></label>
        <label>Mật khẩu<input value={password} onChange={e => setPassword(e.target.value)} type="password" autoComplete="current-password" required /></label>
        {error && <div className="form-error" role="alert">{error}</div>}
        <button className="button primary wide" disabled={busy}>{busy ? 'Đang xác thực…' : 'Đăng nhập'}</button>
        <div className="demo-note"><strong>Tài khoản Owner demo</strong><code>owner@hosco.local</code><small>Credential chỉ dành cho Development.</small></div>
      </form>
    </section>
  </main>
}
