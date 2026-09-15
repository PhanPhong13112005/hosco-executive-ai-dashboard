export type Meta = {
  from: string | null
  to: string | null
  branchId: string | null
  lastUpdatedAt: string
  queryId: string
  correlationId: string
  isStale: boolean
}

export type Envelope<T> = { data: T; meta: Meta }
export type Branch = { id: string; code: string; name: string }
export type RevenuePoint = { date: string; amount: number; currency: string }
export type ProductRank = { productId: string; sku: string; name: string; quantity: number; amount: number; currency: string }
export type DangerousInventory = { branchId: string; productId: string; sku: string; productName: string; quantityOnHand: number; safetyStock: number }

export type DashboardSummary = {
  revenue: number
  totalOrders: number
  aov: number
  grossProfit: number
  grossMarginPercent: number
  cancellationRate: number
  returnRate: number
  dangerousStockCount: number
  currency: string
  openAlerts: number
  urgentAlerts: number
  definitionStatus: string
  note: string
}

export type KpiDrilldown = {
  metricId: string
  code: string
  name: string
  unit: string
  currentValue: number | null
  trend: { date: string; value: number }[]
  definitionStatus: string
  note: string | null
}

export type AlertItem = {
  id: string
  ruleCode: string
  branchId: string | null
  severity: 'Info' | 'Warning' | 'Critical'
  status: 'Open' | 'Acknowledged' | 'Resolved'
  title: string
  detectedAt: string
  detectedValue: number | null
  thresholdValue: number | null
}

export type AlertDetail = AlertItem & {
  ruleId: string | null
  tenantId: string
  message: string
  contextJson: string
  acknowledgedAt: string | null
  acknowledgedBy: string | null
  resolvedAt: string | null
  resolvedBy: string | null
  dedupKey: string
}

export type AlertPage = {
  items: AlertItem[]
  totalCount: number
  summary: { open: number; urgent: number; handled: number; resolved: number }
}

export type AlertRule = {
  id: string
  code: string
  name: string
  description: string
  severity: 'Info' | 'Warning' | 'Critical'
  isEnabled: boolean
  threshold: number | null
  baseline: number | null
  windowMinutes: number
  cooldownMinutes: number
  branchId: string | null
  configJson: string
  baStatus: string
}

