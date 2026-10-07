# Actual Chat UI Verification

Date: 2026-10-06, local browser `http://127.0.0.1:5176/`, existing small/mobile viewport. Demo Owner account, local SQLite API. Performed through computer-use/browser actions, not synthetic screenshots.

Observed:

1. Existing expired token was cleared on API 401 and UI returned to login; demo login succeeded. Dashboard loaded real KPIs/stock/ranking.
2. Enabled Trợ lý AI sidebar opened welcome, suggestions, input and disabled-empty Send.
3. Top 5 suggestion returned scoped seed quantities (206,206,205,205,123) and correlation metadata.
4. “Tồn kho nào đang nguy hiểm?” returned 2 rows, each available quantity 1.
5. “Doanh thu thế nào?” requested a business period. “Còn tuần trước?” reused Revenue and truthfully returned no data.
6. “Thời tiết hôm nay?” stayed Unknown despite Revenue context.
7. Owner requesting A-HN produced visible “Bạn không có quyền xem dữ liệu này.” and retry. Retry did not duplicate the user message.
8. Clear conversation reset messages/context to welcome.
9. API validation process was deliberately stopped for rebuild. Inventory question showed safe unavailable message and retry. After restarting the same API, retry showed loading/disabled buttons then the actual inventory answer, no duplicate user message.

Screenshots are raw browser JPG captures:

- [Top SKU](screenshots/chat-top-products.jpg)
- [Scope denied](screenshots/chat-scope-denied.jpg)

The normal small viewport is retained, including scrollable message/suggestion areas; screenshots do not prove every desktop breakpoint. Runtime HTTP/security verification is independently recorded in `logs/chat-api-runtime.txt`, and fault/timeout paths in the full regression log. No JWT/secret is displayed in these captures.
