# Dashboard Export

Authenticated reporting users can call:

```text
GET /api/v1/reporting/dashboard/export?format=xlsx&from=<ISO>&to=<ISO>&branchId=<GUID>
GET /api/v1/reporting/dashboard/export?format=pdf&from=<ISO>&to=<ISO>&branchId=<GUID>
```

The endpoint uses `ReportingScopeFactory`, `IReportingDataStore` and `IAlertRepository`; it does not calculate KPI values in the browser. Branch validation and Tenant/Branch scope are identical to Dashboard reporting endpoints.

Excel includes date range, selected branch scope, Revenue, GMV, Total Orders, AOV, Gross Profit, Gross Margin, Cancellation/Return Rate, Dangerous Stock, open/urgent alerts, Top 10 SKU, Bottom 10 SKU and dangerous inventory. PDF provides the same MVP dashboard summary as a portable single-page document. Both file names include a UTC timestamp.

The frontend exposes `Xuất Excel` and `Xuất PDF` buttons on Dashboard. Export handles empty datasets and returns 400 for unsupported formats.
