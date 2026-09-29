import type {
  AlertDetail, AlertPage, AlertRule, Branch, DashboardSummary, DangerousInventory,
  Envelope, KpiDrilldown, ProductRank, RevenuePoint,
} from '../types/api'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
const tokenKey = 'hosco.gd3.token'

export class ApiError extends Error {
  constructor(public status: number, message: string, public code = 'request_failed') { super(message) }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem(tokenKey)
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  })
  if (!response.ok) {
    const payload = await response.json().catch(() => null) as { message?: string; code?: string } | null
    throw new ApiError(response.status, payload?.message ?? `HTTP ${response.status}`, payload?.code)
  }
  return response.json() as Promise<T>
}

const query = (params: Record<string, string | number | undefined | null>) => {
  const values = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => { if (value !== undefined && value !== null && value !== '') values.set(key, String(value)) })
  const result = values.toString()
  return result ? `?${result}` : ''
}

export const auth = {
  hasToken: () => Boolean(localStorage.getItem(tokenKey)),
  login: async (email: string, password: string) => {
    const result = await request<{ accessToken: string }>('/api/v1/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) })
    localStorage.setItem(tokenKey, result.accessToken)
  },
  logout: () => localStorage.removeItem(tokenKey),
}

export type DashboardFilter = { from: string; to: string; branchId: string }
const businessRange = (filter: DashboardFilter) => ({
  ...filter,
  from: filter.from ? `${filter.from}T00:00:00+07:00` : '',
  to: filter.to ? `${filter.to}T23:59:59.999+07:00` : '',
})
const filterQuery = (filter: DashboardFilter) => query(businessRange(filter))

export const api = {
  dashboard: (filter: DashboardFilter) => request<Envelope<DashboardSummary>>(`/api/v1/reporting/dashboard/summary${filterQuery(filter)}`),
  revenue: (filter: DashboardFilter) => request<Envelope<RevenuePoint[]>>(`/api/v1/reporting/revenue/trend${filterQuery(filter)}`),
  dangerous: (filter: DashboardFilter) => request<Envelope<DangerousInventory[]>>(`/api/v1/reporting/inventory/dangerous${query({ ...businessRange(filter), pageSize: 20 })}`),
  topProducts: (filter: DashboardFilter, bottom = false) => request<Envelope<ProductRank[]>>(`/api/v1/reporting/products/${bottom ? 'bottom' : 'top'}${query({ ...businessRange(filter), pageSize: 10 })}`),
  branches: () => request<Envelope<Branch[]>>('/api/v1/reporting/branches'),
  drilldown: (metricId: string, filter: DashboardFilter) => request<Envelope<KpiDrilldown>>(`/api/v1/reporting/kpis/${encodeURIComponent(metricId)}/drilldown${filterQuery(filter)}`),
  alerts: (params: { branchId?: string; severity?: string; status?: string }) => request<AlertPage>(`/api/v1/alerts${query(params)}`),
  alert: (id: string) => request<AlertDetail>(`/api/v1/alerts/${id}`),
  acknowledge: (id: string) => request<AlertDetail>(`/api/v1/alerts/${id}/acknowledge`, { method: 'POST' }),
  resolve: (id: string, note?: string) => request<AlertDetail>(`/api/v1/alerts/${id}/resolve`, { method: 'POST', body: JSON.stringify({ note: note || null }) }),
  rules: () => request<AlertRule[]>('/api/v1/alert-rules'),
  updateRule: (id: string, update: Partial<AlertRule>) => request<AlertRule>(`/api/v1/alert-rules/${id}`, { method: 'PATCH', body: JSON.stringify(update) }),
}
