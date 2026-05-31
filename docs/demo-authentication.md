# Demo Authentication

ServiceFlow uses demo JWT authentication for local development and portfolio walkthroughs.
It is not production identity management and does not include passwords, refresh tokens,
external providers, or a user database.

## Roles

- `Admin`: read everything; create, update, and archive clients; create, update, change status, and close service requests.
- `Agent`: read clients and service requests; create, update, change status, and close service requests.
- `Viewer`: read clients and service requests only.

## Get a Demo Token

```bash
curl -X POST http://localhost:8080/api/auth/demo-token \
  -H "Content-Type: application/json" \
  -d '{"userId":"demo-admin","displayName":"Demo Admin","role":"Admin"}'
```

The response contains a bearer token:

```json
{
  "accessToken": "...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-05-31T18:00:00Z"
}
```

Use it with API requests:

```bash
curl http://localhost:8080/api/clients \
  -H "Authorization: Bearer <accessToken>"
```

## Use Token In Swagger

Start the API locally, open Swagger, click **Authorize**, and paste the access token.
Swagger is configured for Bearer tokens, so the token can be pasted directly.

Public endpoints:

- `GET /health`
- `GET /swagger`
- `GET /swagger/v1/swagger.json`
- `POST /api/auth/demo-token`

All client and service request endpoints require a valid demo JWT.

For write actions that persist a user identifier, such as adding service request
comments, use a GUID value in `userId` because the domain model stores user IDs
as GUIDs.
