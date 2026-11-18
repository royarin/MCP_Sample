# Demo 4 – what to narrate if the network fails

**Anonymous call** → `HTTP 401` with body `{"statusCode":401,"message":"A valid Entra ID access token for api://opsdesk-mcp is required."}`

**Reader raising an incident** → tool result relayed by the model:
> Policy: raise_incident requires the Ops.Operator role. This agent only has read access – ask an operator to perform the action.

**Operator** → `Raised INC-00001 (critical) for inventory: "Replication lag over threshold". Owner notified at supply-chain@contoso.com …` and the incident appears on the dashboard.

**Trace** → one operation id: `agent.question` (opsbot-mcp) → `POST /opsdesk-secure/mcp` (APIM) → `tools/call raise_incident` (opsdesk-tasks) → `POST /Incident/inventory` (opsdesk-api).
