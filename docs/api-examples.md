# API Examples

These examples assume the full Docker Compose setup is running at `http://localhost:8080`.

## Get An Admin Token

Use a GUID `userId` for write actions that persist user identifiers, such as comments.

```bash
ADMIN_USER_ID="11111111-1111-1111-1111-111111111111"

ADMIN_TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/demo-token \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"$ADMIN_USER_ID\",\"displayName\":\"Demo Admin\",\"role\":\"Admin\"}" \
  | jq -r ".accessToken")
```

## Call A Protected Endpoint

```bash
curl http://localhost:8080/api/clients \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

## Create A Client

```bash
CLIENT_ID=$(curl -s -X POST http://localhost:8080/api/clients \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Maya Chen","email":"maya.chen@example.com","companyName":"Acme Logistics"}' \
  | jq -r ".id")
```

## Create A Service Request

```bash
SERVICE_REQUEST_ID=$(curl -s -X POST http://localhost:8080/api/service-requests \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"clientId\":\"$CLIENT_ID\",
    \"title\":\"Cannot export monthly report\",
    \"description\":\"The export fails during the final packaging step.\",
    \"priority\":\"High\",
    \"dueDateUtc\":\"2030-01-15T12:00:00Z\"
  }" \
  | jq -r ".id")
```

## Add An Internal Comment

```bash
curl -X POST "http://localhost:8080/api/service-requests/$SERVICE_REQUEST_ID/comments" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"body":"Checked the customer import logs.","visibility":"Internal"}'
```

## View Audit Log

```bash
curl "http://localhost:8080/api/service-requests/$SERVICE_REQUEST_ID/audit-log" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```
