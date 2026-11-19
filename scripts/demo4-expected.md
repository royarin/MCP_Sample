# Demo 4 fallback narration

| Step | Expected result |
|---|---|
| `--no-key` | HTTP 401: APIM subscription key missing |
| `--no-token` | HTTP 401: a valid Entra ID access token for api://opsdesk-mcp is required |
| `--identity norole` | HTTP 403: authenticated, not authorised: role Ops.Agent required |
| Normal startup | Endpoint, key present, client identity, Ops.Agent role, expiry, five read-only tools |
| Inventory briefing | Red replication lag; Supply Chain Systems / #inventory-oncall; Inventory DB Failover Runbook; current incidents |
| Write request | No write tool / read-only connection; no incident created and dashboard unchanged |
| All-systems fan-out | Successful reads, then red `Rate limit exceeded: 6 tool calls per minute for this agent. Retry after <n> s.`; partial answer explains the limit |
| `--open` | The uncurated API-generated twin exposes eleven tools, including writes |
| `--discover opsdesk-apim-secure` | Runtime endpoint resolved from API Center, then the same secure connection |
| Observability | Printed W3C question trace joins agent, APIM, and API telemetry across the two App Insights resources |

API Center shows version 1.0, environment demo, owner Platform Ops, internal classification, read-only tools, and six tool calls/minute/agent. Its endpoint is a catalog entry, not a bypass around gateway policy.

The gate is authoritative: the model cannot create an incident with a tool it never receives. All routes reuse the existing product/key and App Insights setup, but only the dedicated Demo 4 endpoint has its Entra checks and tool budget. The burst script is a rehearsal fallback, not the primary live rate-limit demonstration.
