# Comments And Audit Log

Service request comments and audit logs are available through nested API endpoints.
All endpoints require a demo JWT bearer token.

## Comments

```http
GET /api/service-requests/{id}/comments
POST /api/service-requests/{id}/comments
```

Add a comment:

```bash
curl -X POST http://localhost:8080/api/service-requests/{id}/comments \
  -H "Authorization: Bearer <accessToken>" \
  -H "Content-Type: application/json" \
  -d '{"body":"Checked the customer import logs.","visibility":"Internal"}'
```

Comment visibility:

- `Public`: visible to Admin, Agent, and Viewer.
- `Internal`: visible only to Admin and Agent.

Viewer remains read-only in this MVP:

- Admin and Agent can add `Internal` and `Public` comments.
- Viewer can list comments, but only receives `Public` comments.
- Viewer receives `403 Forbidden` when trying to add a comment.

The comment author is taken from the authenticated JWT `sub` claim. Because the
domain currently stores user IDs as GUIDs, use a GUID value for demo tokens when
testing write actions that create comments.

## Audit Log

```http
GET /api/service-requests/{id}/audit-log
```

Audit logs are read-only from the API and are ordered by `CreatedAtUtc` ascending.
Admin and Agent can view audit logs. Viewer receives `403 Forbidden`.

Status and priority changes create audit entries through the existing service request workflow.
