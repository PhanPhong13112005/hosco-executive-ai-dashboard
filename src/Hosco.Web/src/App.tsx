import { useEffect, useState } from 'react'
import { auth } from './api/client'
import { Shell, type Page } from './components/Shell'
import { LoginPage } from './pages/LoginPage'
import { DashboardPage } from './pages/DashboardPage'
import { AlertsPage } from './pages/AlertsPage'
import { RulesPage } from './pages/RulesPage'
import { ChatPage } from './pages/ChatPage'

export default function App() {
  const [authenticated, setAuthenticated] = useState(auth.hasToken())
  const [page, setPage] = useState<Page>('dashboard')
  useEffect(() => {
    const expired = () => { setAuthenticated(false); setPage('dashboard') }
    window.addEventListener('hosco:unauthorized', expired)
    return () => window.removeEventListener('hosco:unauthorized', expired)
  }, [])
  if (!authenticated) return <LoginPage onSuccess={() => setAuthenticated(true)}/>
  const logout = () => { auth.logout(); setAuthenticated(false) }
  return <Shell page={page} onNavigate={setPage} onLogout={logout}>
    {page === 'dashboard' && <DashboardPage openAlerts={() => setPage('alerts')}/>} 
    {page === 'alerts' && <AlertsPage/>}
    {page === 'chat' && <ChatPage/>}
    {page === 'rules' && <RulesPage/>}
  </Shell>
}

