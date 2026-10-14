# Backlog ngoài MVP
Các mục dưới đây chưa triển khai, cần BA/mentor ưu tiên và acceptancecriteria.
| Hạng mục | Điều kiện trước phát triển |
|---|---|
| Zalo OA | OAapproved, OAuth/webhook/security/privacy/budget/deliverycontract |
| RAG dài | nguồn tài liệu được phép, chunk/version/tenant retrieval isolation/evaluation |
| Streaming | transport/auth/cancellation/backpressure/partial-error UI |
| OLAP | workload/storemodel/ETL/freshness/reconciliation vàtenantaccess |
| Customer/employee/expense/branch comparison | canonicalmetrics/queryendpoints được BA duyệt trước LLMintent |
| Cashier authrole/provisioning | tách employeeentity khỏi identity, permissions/scope/test matrix |
| Safe contextslots | tái authorize inheritedbranch/date/limit, decisionBA09 |
| Historical stock | snapshots/sourcefrequency vàas-ofsemantics đượcBA duyệt |
| Multi-instance scheduler | distributed coordination/exactly-once effects/unique constraints/operationaltests |
| Production hardening | runtimepatches/TLS/secretmanager/databasebackup/loadtests/providerprivacy |
Không tự triển khai các mục này trong GD6 để biến phaseprepared thànhaccepted.
