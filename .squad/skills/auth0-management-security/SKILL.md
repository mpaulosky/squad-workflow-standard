# Auth0 Management API Security Patterns (MyBlog)

**Confidence:** high  
**Last validated:** 2026-04-19 (Sprint 2 mining)  
**MyBlog Status:** Partially implemented — secrets managed correctly; audit logging planned

## Secrets Management (ENFORCED)

**CRITICAL: Auth0 Management API secrets must NEVER appear in source code or committed config files.**

### Local Development

```bash
# After creating M2M application in Auth0 Dashboard, set each secret:
dotnet user-secrets set "Auth0:ManagementApiDomain"       "your-tenant.us.auth0.com"
dotnet user-secrets set "Auth0:ManagementApiClientId"     "YOUR_M2M_CLIENT_ID"
dotnet user-secrets set "Auth0:ManagementApiClientSecret" "YOUR_M2M_CLIENT_SECRET"
```

Secrets are stored locally at `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` (Linux/macOS) and never committed to git.

### CI/CD (GitHub Actions)

- GitHub Actions secrets: `AUTH0_MANAGEMENT_CLIENT_ID`, `AUTH0_MANAGEMENT_CLIENT_SECRET` (set via repo settings)
- Never `Console.Write`, log, or echo secrets — even in debug paths
- Pass secrets to workflow steps via `${{ secrets.AUTH0_MANAGEMENT_CLIENT_ID }}`

### Configuration in Code

- **Safe to commit:** `appsettings.json` with non-secret Auth0 settings (Domain, ClientId only)
- **Never commit:** ClientSecret, M2M credentials
- See `docs/AUTH0_SETUP.md` section "Local Development Configuration"

---

## Principle of Least Privilege

Request only the Management API scopes needed for MyBlog operations:

| Scope | Purpose | Used By |
| --- | --- | --- |
| `read:users` | Read user profiles (email, name, metadata) | GetUsersWithRolesQuery |
| `read:roles` | List available role definitions | GetAvailableRolesQuery |
| `create:role_members` | Assign a user to a role | AssignRoleCommand |
| `delete:role_members` | Remove a user from a role | RemoveRoleCommand |

**Do NOT request** `create:users`, `delete:users`, or `create:roles` unless needed. The M2M app in `docs/AUTH0_SETUP.md` is already configured with only the above scopes.

---

## Authorization Boundary (ENFORCED)

**All routes that call UserManagementHandler MUST be guarded by AdminPolicy.**

Current implementations in MyBlog:

| Endpoint | Handler | Guard |
| --- | --- | --- |
| `/admin/users` (list) | GetUsersWithRolesQuery | Checked in ManageRoles.razor |
| `/admin/users/{id}/roles/assign` | AssignRoleCommand | Checked in ManageRoles.razor |
| `/admin/users/{id}/roles/remove` | RemoveRoleCommand | Checked in ManageRoles.razor |

**Never allow user-role operations from non-admin endpoints.** If you add new routes:

```csharp
app.MapGet("/admin/users", ...).RequireAuthorization("AdminPolicy");
```

---

## Error Handling

Auth0 Management API errors should be wrapped and logged without exposing internals to end users.

Current MyBlog pattern:

```csharp
catch (Exception ex)
{
    // Log internally (if logging added)
    return Result.Fail(ex.Message);
}
```

**Future improvement** (not yet implemented):

- Wrap specific Auth0 errors in `ResultErrorCode.ExternalService`
- Never expose raw Auth0 error codes, status codes, or SDK stack traces

**Structured logging (not yet implemented):**

- Use structured logging for all admin role operations (userId, actorId, action, timestamp, roleId)
- Avoid string interpolation to prevent log injection attacks

---

## Rate Limiting

Auth0 Management API rate limits:

- **Developer plan:** 1,000 requests/minute (current — adequate for admin UI)
- **Production plan:** 5,000 requests/minute

**Current MyBlog:** No caching; every role query or assignment hits the API.

**Recommended for production:**

- Cache user list in IMemoryCache with 5-minute TTL
- Cache available roles with 5-minute TTL
- **Status:** Planned but not implemented; add to backlog if scaling admin user count

---

## Testing

### Unit Testing

- `ManagementApiClient` is a sealed class — not unit-testable without a factory wrapper
- Mock `IConfiguration` and `IHttpClientFactory` for UserManagementHandler tests
- See `.squad/skills/testing-patterns/` (Sprint 2 review) for test fixture recommendations

### Integration Testing

- Use test Auth0 tenant or mock HTTP responses
- Verify token fetch, role assignment, role removal, and error cases
- Not yet implemented in MyBlog test suite; add to Sprint 2 test review

---

## Audit Logging (NOT YET IMPLEMENTED)

**Planned:** All admin role operations should log:

- **userId** — User being modified
- **actorId** — Admin making the change
- **action** — 'assign', 'revoke', or other operation
- **timestamp** — When the change occurred
- **roleId** — Role being assigned or revoked

**Current state:** No audit logging in UserManagementHandler; consider adding structured audit table or event stream.

---

## Related Documentation

- **docs/AUTH0_SETUP.md** — Complete Auth0 configuration for new developers
- **docs/SECURITY.md** — Organization-wide security policy (Auth0 secrets policy now enforced)
- **src/Web/Features/UserManagement/UserManagementHandler.cs** — CQRS handler implementation
- **CONTRIBUTING.md** — Contributor workflow (reference this skill)
