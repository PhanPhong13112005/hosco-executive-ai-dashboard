# GD4 Evidence Index

CHATBOT DOES NOT ACCESS DATABASE DIRECTLY.

Current LLM extension evidence (2026-10-07):

- `logs/llm-final-build.txt`: solution build PASS, existing NU1900 warning.
- `logs/llm-full-regression.txt`: final Unit 131/131 + Integration 128/128 = 259/259 PASS.
- `logs/llm-frontend-build.txt`: unchanged frontend build PASS.
- `logs/llm-validation-status.txt`: boolean-only configuration check; **LIVE_LLM_PROVIDER=NOT_CONFIGURED**, mock HTTP provider validation and real local Chat/Reporting runtime.
- `logs/llm-intermediate-assertion.txt`: intermediate failed follow-up test assertion, corrected by observing actual Reporting date parameters; not final evidence.

Pre-LLM baseline evidence (2026-10-06), preserved without replacement:

- `logs/final-build.txt`: final solution build.
- `logs/full-regression.txt`: final 80 unit + 80 integration tests.
- `logs/final-frontend-build.txt`: final TypeScript/Vite production build.
- `logs/npm-install.txt`: requested install result; `logs/npm-audit.txt`: open transitive advisory.
- `logs/chat-api-runtime.txt`: actual local Chat API/mapping/401/403/tenant/branch/injection and GD3 exports.
- `Verify-ChatRuntime.ps1`: local-only repeatable runtime check; never prints bearer tokens.
- `UI_VERIFICATION.md` + `screenshots/*.jpg`: observed real Chat UI.

Other logs record earlier implementation validation and sandbox startup attempts. They are retained as history, not used to override the final counts or conceal failures. Trailing spaces in historical Vite stderr were trimmed for Git whitespace checks; error content is unchanged. No screenshot has been synthesized and no unexecuted check is labeled PASS.
