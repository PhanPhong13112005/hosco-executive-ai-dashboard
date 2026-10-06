# GD4 Evidence Index

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

Authoritative final evidence (2026-10-06):

- `logs/final-build.txt`: final solution build.
- `logs/full-regression.txt`: final 80 unit + 80 integration tests.
- `logs/final-frontend-build.txt`: final TypeScript/Vite production build.
- `logs/npm-install.txt`: requested install result; `logs/npm-audit.txt`: open transitive advisory.
- `logs/chat-api-runtime.txt`: actual local Chat API/mapping/401/403/tenant/branch/injection and GD3 exports.
- `Verify-ChatRuntime.ps1`: local-only repeatable runtime check; never prints bearer tokens.
- `UI_VERIFICATION.md` + `screenshots/*.jpg`: observed real Chat UI.

Other logs record earlier implementation validation and sandbox startup attempts. They are retained as history, not used to override the final counts or conceal failures. Trailing spaces in historical Vite stderr were trimmed for Git whitespace checks; error content is unchanged. No screenshot has been synthesized and no unexecuted check is labeled PASS.
