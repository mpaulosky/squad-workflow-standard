# Auth0 Management API Integration Pattern (MyBlog)

**Confidence:** high  
**Last validated:** 2026-04-19 (Sprint 2 mining)  
**MyBlog Status:** Production — UserManagementHandler actively implements this pattern

## Pattern

MyBlog calls the Auth0 Management API to read users and assign/revoke roles. The implementation is in `src/Web/Features/UserManagement/UserManagementHandler.cs` and follows a direct ManagementApiClient pattern with inline M2M token fetching.

### Architecture

```
Program.cs (config + Auth0 setup)
  ↓
RoleClaimsHelper (remaps role claims)
  ↓
UserManagementHandler (MediatR handlers)
  ├─ GetUsersWithRolesQuery → User list + roles (read-side)
  ├─ GetAvailableRolesQuery → Role definitions (read-side)
  ├─ AssignRoleCommand → Add user to role (write-side)
  └─ RemoveRoleCommand → Remove user from role (write-side)
  ↓
ManageRoles.razor (UI for admin)
```

### Implementation Details

#### Package & Instantiation

- **Package:** Auth0.ManagementApi 7.46.0 (in Directory.Packages.props)
- **Client creation:** ManagementApiClient is instantiated inline in `GetManagementClientAsync()` method
- **Token fetch:** Direct `httpClient.PostAsJsonAsync()` to `/oauth/token` endpoint with M2M credentials
- **Token lifespan:** Access token valid for 24 hours (not currently cached)

#### CQRS Handlers

All handlers in UserManagementHandler follow consistent patterns:

| Handler | Queries | Operations |
| --- | --- | --- |
| GetUsersWithRolesQuery | GetAllAsync, GetRolesAsync per user | Read-only |
| GetAvailableRolesQuery | Roles.GetAllAsync | Read-only |
| AssignRoleCommand | Users.AssignRolesAsync | Write |
| RemoveRoleCommand | Users.RemoveRolesAsync | Write |

#### PaginationInfo

- Imported from `Auth0.ManagementApi.Paging` namespace
- Used in GetAllAsync, GetRolesAsync, etc. — default pagination is usually sufficient for admin UI

#### Error Handling

- Catch-all try/catch returns `Result.Fail(ex.Message)` or `Result.Fail<T>(ex.Message)`
- Raw exception messages are logged but wrapped for API responses
- **TODO:** Should wrap in ResultErrorCode.ExternalService to match security pattern

#### Secrets Management

- **Local dev:** `dotnet user-secrets set "Auth0:ManagementApiDomain" "..."` (see docs/AUTH0_SETUP.md)
- **CI/CD:** GitHub Actions secrets (AUTH0_MANAGEMENT_CLIENT_ID, AUTH0_MANAGEMENT_CLIENT_SECRET)
- **Rule:** Secrets NEVER appear in source code; configuration only via User Secrets or env vars

### Authorization Boundary

- **Enforcement:** AdminPolicy guards ALL `/admin/users` routes (in Program.cs or route definition)
- **Check:** Verify `[Authorize("AdminPolicy")]` on all UserManagementHandler endpoint mappings
- **Violation:** Non-admin users cannot access user list, role assignment, or role queries

### Rate Limiting & Performance

**Current State:** No caching; every request hits Auth0 Management API.

- **Developer plan limit:** 1,000 requests/minute (sufficient for admin UI with ~10 admins)
- **Production:** Implement IMemoryCache (5-minute TTL) for user list and available roles
- **Audit:** All role modifications are currently logged to result objects; structured audit logging is planned

### Role Claim Transformation

- Handler: `RoleClaimsHelper.AddRoleClaims()` called during OpenID Connect token validation
- Maps multiple role claim types (auth0 namespaces, standard claim types) into `System.Security.Claims.ClaimTypes.Role`
- Allows authorization checks to use standard `[Authorize(Roles = "Admin")]`

### Testing Notes

- **ManagementApiClient:** Not unit-testable due to sealed class and no factory interface; use integration tests
- **UserManagementHandler:** Mock IConfiguration and IHttpClientFactory for unit tests
- **See also:** `.squad/skills/testing-patterns/` (Sprint 2 — under review)
