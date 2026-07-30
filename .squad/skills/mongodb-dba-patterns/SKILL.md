---
name: mongodb-dba-patterns
confidence: medium
description: >
  MyBlog-specific MongoDB operations guidance for Aspire-managed local runtime,
  MongoDB.EntityFrameworkCore mappings, repository ownership, and shared-environment
  DBA work that still needs explicit review before production use.
---

## MongoDB DBA Patterns (MyBlog)

### Why this exists

MyBlog runs MongoDB through .NET Aspire and `MongoDB.EntityFrameworkCore`, not
through hand-written driver repositories. This skill narrows MongoDB guidance to
real MyBlog code paths so Sam, Gimli, Boromir, and Frodo know which changes are
live conventions versus future-only operator notes.

### Current MyBlog MongoDB map

| Area | Canonical files | Owner | Current rule |
| --- | --- | --- | --- |
| Local runtime wiring | `src/AppHost/AppHost.cs`, `src/Web/Program.cs` | Boromir + Sam | AppHost defines `mongodb` and database `myblog`; Web consumes it through `AddMongoDBClient("myblog")`. |
| EF Core mapping | `src/Web/Data/BlogDbContext.cs` | Sam | `BlogPost` maps to collection `blogposts`; `Version` is the optimistic concurrency token. |
| Repository layer | `src/Domain/Interfaces/IBlogPostRepository.cs`, `src/Web/Data/MongoDbBlogPostRepository.cs` | Sam | Repositories return domain entities; handlers wrap results in `Result` / `Result<T>`. |
| Read-side caching | `src/Web/Features/BlogPosts/List/GetBlogPostsHandler.cs`, `src/Web/Features/BlogPosts/Edit/EditBlogPostHandler.cs` | Sam | Mongo-backed reads are cached at handler level, not in the repository. |
| Integration proof | `tests/Integration.Tests/Infrastructure/MongoDbFixture.cs`, `tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs` | Gimli | Testcontainers-backed Mongo is the canonical verification path for persistence behavior. |
| Secrets / external hardening | runtime config + deployment pipeline | Frodo + Boromir | User Secrets / secret stores own credentials; never commit connection strings with secrets. |

### Use this skill when

- changing Mongo wiring in Aspire AppHost or `Program.cs`
- changing collection mapping, concurrency tokens, or document shape
- diagnosing slow repository reads or missing index support
- planning backups, restores, or Mongo version upgrades for a shared environment
- tightening Mongo authentication, TLS, or least-privilege access
- investigating Mongo-backed integration test failures

### MyBlog operational rules

1. **Use Aspire for local Mongo first.**
   - Start from `src/AppHost` with `dotnet run`.
   - Do not hard-code a local Mongo connection string in `appsettings.json` just
     to bypass Aspire.

2. **Change schema shape in code, not in ad hoc shell sessions.**
   - Collection name, concurrency, and entity shape live in
     `BlogDbContext` + `BlogPost`.
   - If a Mongo admin step is required, document it beside the code change and
     hand it to Boromir for environment rollout.

3. **Repository work stays EF Core-first.**
   - `MongoDbBlogPostRepository` uses short-lived contexts from
     `IDbContextFactory<BlogDbContext>`.
   - Do not introduce raw `IMongoCollection<T>` usage unless the EF provider
     cannot express the query and Sam explicitly signs off.

4. **Handler caching remains outside the repository.**
   - Query caches belong in MediatR handlers.
   - Any Mongo change that affects list/detail reads must also review cache keys
     and invalidation in the handler layer.

5. **Integration tests are the contract check.**
   - Mongo persistence, ordering, and concurrency changes require matching
     updates in `Integration.Tests`.
   - Gimli owns the test side; Sam owns runtime implementation.

6. **Sync the main repo before starting Aspire (running environment check).**
   - The Aspire AppHost DLL that is currently running determines which MongoDB
     image and volume are in use. A stale local `dev` branch will silently run
     old infra code even after a fix has been merged.
   - Before starting (or restarting) Aspire, confirm the main repo is current
     by running `git pull origin dev` then
     `dotnet build src/AppHost/AppHost.csproj -c Debug`.
   - If Aspire is already running and MongoDB is crashing, identify the active
     build with `ps aux | grep AppHost.dll` — the DLL path shows which repo
     root is in use.
   - If the path points to a repo other than your current worktree, rebuild that
     repo's AppHost or restart Aspire from the correct directory.

### Local development and inspection

Preferred tooling:

- **MongoDB for VS Code** or **MongoDB Compass** for collection inspection,
  indexes, and explain plans
- **Aspire dashboard** for connection/runtime visibility
- **`mongosh`** only when GUI tooling cannot answer the question

Example inspection flow for MyBlog:

```bash
cd src/AppHost
dotnet run
```

Then connect with the connection string surfaced by Aspire and inspect the live
`myblog.blogposts` collection. For test failures, use the connection string from
`MongoDbFixture` and the per-test database name created in the test.

### Collection and mapping conventions

Current canonical mapping:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var entity = modelBuilder.Entity<BlogPost>();
    entity.ToCollection("blogposts");
    entity.HasKey(p => p.Id);
    entity.Property(p => p.Version).IsConcurrencyToken();
}
```

Rules:

- Keep collection names lowercase and stable once data exists.
- Treat `Version` concurrency behavior as part of the storage contract.
- If document shape changes, update the entity, mapping, repository usage, and
  integration tests in the same change.

### Backup and restore guidance

MyBlog has **no automated backup workflow in-repo today**. Local Aspire and
Testcontainers databases are disposable; backup guidance matters only for a
shared or production Mongo environment.

If a shared environment is introduced, Boromir owns automation and Sam verifies
collection-level restore assumptions.

Example commands for a future shared environment:

```bash
mongodump --uri="<shared-mongo-connection-string>" \
  --db=myblog \
  --out=./backups/mongo/2026-04-19

mongorestore --drop \
  --uri="<staging-mongo-connection-string>" \
  --db=myblog \
  ./backups/mongo/2026-04-19/myblog
```

Rules:

- Never treat disposable local/Testcontainers data as a backup source.
- Always rehearse restore on a non-production target before calling a backup
  strategy complete.
- Keep backup credentials out of scripts committed to the repo.

### Performance and index review

Current read hotspot:

- `MongoDbBlogPostRepository.GetAllAsync()` sorts by `CreatedAt` descending.
- `GetBlogPostsHandler` caches the DTO list in memory + Redis.

MyBlog rules:

- Any new filter or alternate sort added to the repository triggers an index
  review for `blogposts`.
- Sam proposes index shape; Gimli verifies the query path with integration tests;
  Boromir applies/shared-environment rollout when needed.
- Use Compass / VS Code explain plans before adding driver-only code.

Recommended current review candidate if the list grows materially:

```javascript
db.blogposts.createIndex({ CreatedAt: -1 }, { name: "idx_blogposts_created_desc" })
```

This is a **candidate**, not a declared current production index.

### Security and secret handling

- Local credentials belong in User Secrets or Aspire-managed secrets, never in
  committed `appsettings*.json` values with real secrets.
- Shared-environment Mongo should use SCRAM auth and TLS.
- Frodo reviews least-privilege requirements before any non-local Mongo rollout.
- Do not log raw connection strings, passwords, or certificates.

### Package and upgrade guidance

MyBlog pins Mongo packages directly in `src/Web/Web.csproj` today:

- `Aspire.MongoDB.Driver`
- `MongoDB.EntityFrameworkCore`

Upgrade rules:

1. Upgrade one major MongoDB server version at a time.
2. Check the EF Core provider release notes before bumping package versions.
3. Re-run repository integration tests, especially ordering and concurrency.
4. Treat provider upgrades as Sam-owned code work plus Boromir-owned environment
   coordination.

### Explicit non-fit items for later deletion review

The imported skill carried guidance that is not part of normal MyBlog flow yet:

- **Manual replica-set initiation steps** — local MyBlog uses Aspire and
  Testcontainers; we do not manually bootstrap replica sets in normal work.
- **IssueManager collection examples** — not part of this repo and removed from
  the retained guidance.
- **Atlas-only cluster administration detail** — useful only if MyBlog adopts a
  managed shared Mongo deployment later.
- **Always-on profiling guidance** — premature for the current small training
  app; only use profiling during a targeted investigation.

If MyBlog stays Aspire-local and test-container-only, revisit whether the
shared-environment backup/upgrade sections should be trimmed further in
Milestone 3.

### References

- `src/AppHost/AppHost.cs`
- `src/Web/Program.cs`
- `src/Web/Data/BlogDbContext.cs`
- `src/Web/Data/MongoDbBlogPostRepository.cs`
- `tests/Integration.Tests/Infrastructure/MongoDbFixture.cs`
- [MongoDB EF Core Provider](https://www.mongodb.com/docs/entity-framework/current/)
- [MongoDB for VS Code](https://www.mongodb.com/products/tools/vs-code)
- [mongodump / mongorestore](https://www.mongodb.com/docs/database-tools/mongodump/)
