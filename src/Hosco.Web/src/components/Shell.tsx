export type Page = 'dashboard' | 'alerts' | 'rules'

export function Shell({ page, onNavigate, onLogout, children }: {
  page: Page
  onNavigate: (page: Page) => void
  onLogout: () => void
  children: React.ReactNode
}) {
  return <div className="app-shell">
    <aside className="sidebar">
      <div className="brand"><span className="brand-mark">H</span><div><strong>HOSCO</strong><small>EXECUTIVE CONSOLE</small></div></div>
      <nav>
        <button className={page === 'dashboard' ? 'active' : ''} onClick={() => onNavigate('dashboard')}><span>⌁</span> Dashboard</button>
        <button className={page === 'alerts' ? 'active' : ''} onClick={() => onNavigate('alerts')}><span>⚡</span> Cảnh báo</button>
        <button disabled title="Sẽ triển khai ở GD4" aria-label="Trợ lý AI, sẽ triển khai ở GD4"><span>✦</span> Trợ lý AI <small>GD4</small></button>
        <button className={page === 'rules' ? 'active' : ''} onClick={() => onNavigate('rules')}><span>⚙</span> Cấu hình cảnh báo</button>
      </nav>
      <div className="sidebar-bottom">
        <button className="logout" onClick={onLogout}>Đăng xuất</button>
      </div>
    </aside>
    <main className="main-content">{children}</main>
  </div>
}
