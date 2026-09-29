# Notification Dispatcher MVP

## Flow

`AlertEngine -> NotificationDispatcher -> INotificationChannel -> Telegram Bot API`

Each attempt is stored in `NotificationDeliveries`. A deterministic SHA-256 idempotency key covers `AlertId + Purpose + Channel + RecipientKey`; a unique index prevents a completed delivery from being sent again. Initial and escalation deliveries are separate purposes.

Statuses are `Pending`, `Sent`, `Failed`, and `Skipped`. Transient HTTP 408/429/5xx, transport failures and timeout remain Pending until the bounded maximum is reached. Retry delay uses bounded exponential backoff and `TimeProvider`. A disabled or incomplete Telegram configuration becomes Skipped and cannot crash the scheduler.

The MVP destination is a tenant-scoped default Telegram chat. `RecipientKey` is derived from the existing `AlertRecipientPolicy`, Tenant and Branch; it never contains the chat ID or BotToken. A production recipient directory and per-user routing remain production hardening.

## Configuration

Safe repository defaults keep Telegram disabled. Enable it with environment variables or an approved secret provider:

```text
Notifications__Telegram__Enabled=true
Notifications__Telegram__BotToken=<SECRET>
Notifications__Telegram__DefaultChatId=<CHAT_ID>
Notifications__Telegram__DefaultTenantId=<TENANT_GUID>
Notifications__Telegram__DefaultBranchId=<OPTIONAL_BRANCH_GUID>
Notifications__Telegram__TimeoutSeconds=10
Notifications__Telegram__MaxRetries=3
Notifications__Telegram__BaseRetryDelayMilliseconds=500
```

`DefaultTenantId` is mandatory when enabled; without it the provider skips delivery to prevent cross-tenant routing. Real credentials must not be committed. HttpClient request logging is removed for this typed client because the Telegram Bot API token is part of the request URL. Application logs contain Alert/Tenant/Branch/status metadata only.

No real Telegram credential was used in local validation. Fake HTTP tests cover success, timeout, 5xx retryability and disabled mode.
