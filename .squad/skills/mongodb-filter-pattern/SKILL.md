---
name: mongodb-filter-pattern
confidence: medium
description: >
  MyBlog-specific pattern for extending read-side Mongo queries through
  GetBlogPostsQuery, GetBlogPostsHandler, IBlogPostRepository, and
  MongoDbBlogPostRepository using MongoDB.EntityFrameworkCore conventions.
---

## MongoDB Filter Pattern (MyBlog)

### Why this exists

The imported filter skill assumed raw Mongo driver filters, paginated API
endpoints, and repository methods that return `Result<T>`. MyBlog's current read
path is different: `GetBlogPostsQuery` calls `GetBlogPostsHandler`, which reads
through `IBlogPostRepository` / `MongoDbBlogPostRepository`, then caches DTOs in
memory and Redis.

This skill defines the filter pattern that actually fits MyBlog.

### Current list-query path

| Layer | Canonical file | Owner | Current behavior |
| --- | --- | --- | --- |
| Query contract | `src/Web/Features/BlogPosts/List/GetBlogPostsQuery.cs` | Sam | Query has no filter properties yet. |
| Handler | `src/Web/Features/BlogPosts/List/GetBlogPostsHandler.cs` | Sam | Uses a fixed cache key `blog:all`; maps domain entities to DTOs. |
| Repository contract | `src/Domain/Interfaces/IBlogPostRepository.cs` | Sam | `GetAllAsync(CancellationToken)` returns domain entities directly. |
| Repository implementation | `src/Web/Data/MongoDbBlogPostRepository.cs` | Sam | Uses EF Core LINQ over `BlogDbContext`, ordered by `CreatedAt` descending. |
| Unit tests | `tests/Unit.Tests/Handlers/GetBlogPostsHandlerTests.cs` | Gimli | Verifies cache hit/miss behavior and repository calls. |
| Integration tests | `tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs` | Gimli | Verifies ordering, persistence, delete, and concurrency behavior against real Mongo. |

### Use this skill when

- adding search, author, publish-state, or date filters to blog post lists
- extending future Mongo-backed read queries in the same repo style
- updating cache-key composition for list queries
- reviewing whether a new filter needs an index or integration coverage

### MyBlog conventions

1. **Query contract first.**
   - Add nullable filter properties to the MediatR query record first.
   - Keep property names aligned across query, handler, repository signature, and
     tests.

2. **Repositories return domain entities, not `Result<T>`.**
   - Keep `Result` wrapping in handlers.
   - Do not change repository return types just to match the old imported skill.

3. **Use EF Core LINQ, not `Builders<T>.Filter`, for normal MyBlog queries.**
   - `MongoDbBlogPostRepository` is built on `BlogDbContext`.
   - Start with `AsNoTracking()`, then add conditional `Where(...)` clauses.

4. **Cache keys must include filter state.**
   - The current fixed key `blog:all` only fits the unfiltered list.
   - When filters are added, build a normalized cache key from every parameter
     that changes the result set.

5. **Handler + repository + tests move together.**
   - Sam updates query/handler/repository files.
   - Gimli updates unit/integration tests.
   - If UI query inputs are added, hand off to Legolas after the backend
     contract is stable.

### Recommended implementation shape

#### 1. Extend the MediatR query

```csharp
public sealed record GetBlogPostsQuery(
    string? SearchTerm = null,
    string? Author = null,
    bool? IsPublished = null) : IRequest<Result<IReadOnlyList<BlogPostDto>>>;
```

Rules:

- Keep filter properties nullable for optional behavior.
- Normalize trimming in the handler before building cache keys or repository
  arguments.

#### 2. Extend the repository contract

```csharp
Task<IReadOnlyList<BlogPost>> GetAllAsync(
    string? searchTerm = null,
    string? author = null,
    bool? isPublished = null,
    CancellationToken ct = default);
```

Rules:

- Add optional parameters to the interface before touching the implementation.
- Keep the contract in `src/Domain/Interfaces` and the implementation in
  `src/Web/Data`.

#### 3. Apply conditional LINQ in the repository

```csharp
await using var ctx = await contextFactory.CreateDbContextAsync(ct);
var query = ctx.BlogPosts.AsNoTracking();

if (!string.IsNullOrWhiteSpace(searchTerm))
{
    var term = searchTerm.Trim();
    query = query.Where(p =>
        p.Title.Contains(term) ||
        p.Content.Contains(term) ||
        p.Author.Contains(term));
}

if (!string.IsNullOrWhiteSpace(author))
{
    var normalizedAuthor = author.Trim();
    query = query.Where(p => p.Author == normalizedAuthor);
}

if (isPublished is not null)
{
    query = query.Where(p => p.IsPublished == isPublished.Value);
}

return await query
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync(ct);
```

Rules:

- Preserve `AsNoTracking()` for list reads.
- Keep the sort explicit and stable.
- If case-insensitive text behavior is required, verify provider translation with
  an integration test before merging.
- Do not drop to raw regex/driver code unless EF translation proves inadequate.

#### 4. Expand the cache key in the handler

```csharp
private static string BuildCacheKey(GetBlogPostsQuery request)
{
    var search = request.SearchTerm?.Trim() ?? string.Empty;
    var author = request.Author?.Trim() ?? string.Empty;
    var published = request.IsPublished?.ToString() ?? "all";
    return $"blog:list:{search}:{author}:{published}";
}
```

Rules:

- Use the same normalized values for cache key generation and repository calls.
- Review cache invalidation in create/edit/delete handlers when introducing new
  list variants.

### Testing rules

#### Unit tests (Gimli)

Update `GetBlogPostsHandlerTests` to cover:

- filtered cache key generation
- cache miss path with repository arguments
- cache hit path per filtered key

#### Integration tests (Gimli)

Add/extend `MongoDbBlogPostRepositoryTests` to prove:

- filter translation against real Mongo
- ordering still works with filters applied
- edge cases such as empty filters and combined filters

### Validation guidance

Current MyBlog state:

- `Web.csproj` does **not** currently reference FluentValidation.
- Blog post validation today is mainly domain guard clauses plus handler error
  wrapping.

Rule:

- Do not invent generic validator files just because the imported skill used
  them.
- If FluentValidation is introduced later, colocate validators with the feature
  and keep the property names identical to the MediatR query.

### Explicit non-fit items for later deletion review

The imported skill included several patterns that are **not current MyBlog
conventions**:

- **`Builders<T>.Filter` + `BsonRegularExpression` examples** — MyBlog uses the EF
  Core adapter first.
- **Minimal API endpoint examples** — current blog list flow is Blazor + MediatR,
  not `IEndpointRouteBuilder` list endpoints.
- **HTTP client query-string builders** — MyBlog pages currently call handlers
  directly rather than a separate REST client.
- **Repository methods returning `Result<T>`** — current repositories return
  domain entities and leave result wrapping to handlers.
- **Pagination contract examples** — current blog list query is unpaged; if
  paging is introduced later, document it as a separate repo convention.

If MyBlog later exposes REST endpoints for blog lists, revisit this skill and
promote only the pieces that map to the new code path.

### Files that usually change together

1. `src/Web/Features/BlogPosts/List/GetBlogPostsQuery.cs`
2. `src/Web/Features/BlogPosts/List/GetBlogPostsHandler.cs`
3. `src/Domain/Interfaces/IBlogPostRepository.cs`
4. `src/Web/Data/MongoDbBlogPostRepository.cs`
5. `tests/Unit.Tests/Handlers/GetBlogPostsHandlerTests.cs`
6. `tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs`

### References

- `src/Web/Features/BlogPosts/List/GetBlogPostsQuery.cs`
- `src/Web/Features/BlogPosts/List/GetBlogPostsHandler.cs`
- `src/Domain/Interfaces/IBlogPostRepository.cs`
- `src/Web/Data/MongoDbBlogPostRepository.cs`
- `tests/Unit.Tests/Handlers/GetBlogPostsHandlerTests.cs`
- `tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs`
- [MongoDB EF Core Provider](https://www.mongodb.com/docs/entity-framework/current/)
