export function StatePanel({ kind, title, message, onRetry }: {
  kind: 'loading' | 'empty' | 'error' | 'permission'
  title: string
  message: string
  onRetry?: () => void
}) {
  return <div className={`state-panel state-${kind}`} role={kind === 'error' ? 'alert' : 'status'}>
    <div className="state-icon">{kind === 'loading' ? '◌' : kind === 'empty' ? '◇' : kind === 'permission' ? '⊘' : '!'}</div>
    <div><strong>{title}</strong><p>{message}</p></div>
    {onRetry && <button className="button secondary" onClick={onRetry}>Thử lại</button>}
  </div>
}

