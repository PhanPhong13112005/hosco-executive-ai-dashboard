import { useMemo, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { ChatContext } from '../types/api'

type Message = { id: string; role: 'user' | 'assistant'; text: string; time: Date; meta?: string }

const initialSuggestions = [
  'Doanh thu hôm nay?',
  'Top 5 sản phẩm bán chạy?',
  'Tồn kho nào đang nguy hiểm?',
  'Có cảnh báo Critical nào không?',
  'Doanh thu 7 ngày gần nhất?',
]

export function ChatPage() {
  const welcome = useMemo<Message>(() => ({
    id: crypto.randomUUID(), role: 'assistant', time: new Date(),
    text: 'Xin chào! Tôi là trợ lý báo cáo HOSCO. Tôi có thể giúp bạn xem KPI, xu hướng, sản phẩm, tồn kho và cảnh báo trong phạm vi được cấp quyền.',
  }), [])
  const [messages, setMessages] = useState<Message[]>([welcome])
  const [input, setInput] = useState('')
  const [context, setContext] = useState<ChatContext>()
  const [suggestions, setSuggestions] = useState(initialSuggestions)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>()
  const [lastMessage, setLastMessage] = useState('')

  const send = async (value = input, retry = false) => {
    const text = value.trim()
    if (!text || loading) return
    setInput(''); setError(undefined); setLastMessage(text); setLoading(true)
    if (!retry) setMessages(current => [...current, { id: crypto.randomUUID(), role: 'user', text, time: new Date() }])
    try {
      const response = await api.chat(text, context)
      setContext(response.context)
      if (response.status === 'Unavailable') { setError(response.message); return }
      setSuggestions(response.suggestions)
      setMessages(current => [...current, {
        id: crypto.randomUUID(), role: 'assistant', text: response.message, time: new Date(),
        meta: `${response.intent} · ${response.correlationId.slice(0, 8)}`,
      }])
    } catch (reason) {
      const message = reason instanceof ApiError && reason.status === 403
        ? 'Bạn không có quyền xem dữ liệu này.'
        : 'Hiện chưa thể lấy dữ liệu báo cáo. Vui lòng thử lại sau.'
      setError(message)
    } finally { setLoading(false) }
  }

  const clear = () => { setMessages([welcome]); setContext(undefined); setError(undefined); setSuggestions(initialSuggestions) }

  return <section className="chat-page">
    <header className="page-header">
      <div><p className="eyebrow">GD4 · READ-ONLY REPORTING</p><h1>Trợ lý AI</h1><p>Hỏi dữ liệu kinh doanh bằng ngôn ngữ tự nhiên.</p></div>
      <button className="button secondary" onClick={clear}>Xóa hội thoại</button>
    </header>
    <div className="chat-card">
      <div className="chat-messages" aria-live="polite">
        {messages.map(message => <article key={message.id} className={`chat-message ${message.role}`}>
          <div className="chat-avatar">{message.role === 'assistant' ? '✦' : 'Bạn'}</div>
          <div><p>{message.text}</p><small>{message.time.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}{message.meta ? ` · ${message.meta}` : ''}</small></div>
        </article>)}
        {loading && <article className="chat-message assistant"><div className="chat-avatar">✦</div><div><p className="chat-typing">Đang lấy dữ liệu báo cáo…</p></div></article>}
        {error && <div className="chat-error"><span>{error}</span><button onClick={() => send(lastMessage, true)}>Thử lại</button></div>}
      </div>
      <div className="chat-suggestions">
        {suggestions.map(suggestion => <button key={suggestion} onClick={() => send(suggestion)} disabled={loading}>{suggestion}</button>)}
      </div>
      <form className="chat-composer" onSubmit={event => { event.preventDefault(); send() }}>
        <textarea aria-label="Câu hỏi báo cáo" value={input} onChange={event => setInput(event.target.value)} maxLength={1000} rows={2}
          placeholder="Ví dụ: Doanh thu tuần này?" onKeyDown={event => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); send() } }}/>
        <button className="button" disabled={loading || !input.trim()}>Gửi</button>
      </form>
      <p className="chat-disclaimer">Trợ lý chỉ đọc dữ liệu từ Reporting API trong phạm vi quyền của bạn.</p>
    </div>
  </section>
}
