---
name: testcontainers-shared-fixture
confidence: high
description: >
  MyBlog integration-test convention for sharing one MongoDbFixture per xUnit
  domain collection in tests/Integration.Tests while keeping per-test database
  isolation.
---

## MyBlog Testcontainers Shared Fixture

### Current repo fit

- `tests/Integration.Tests/Infrastructure/MongoDbFixture.cs` ✅ Already implements
  `IAsyncLifetime` for container lifecycle management.
- `tests/Integration.Tests/Infrastructure/BlogPostIntegrationCollection.cs` ✅
  Defines the xUnit collection definition.
- `tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs` ✅ Demonstrates
  live MongoDB-backed integration suite (9 tests, all passing).
- Container startup time: ~2–3s per shared fixture (acceptable for MVP).
- Per-test isolation: Ensures each test writes to a unique database within the shared container.

### MyBlog Tested Patterns (Established)

1. **One shared Mongo fixture per xUnit domain collection**
   - Define collection definitions once:

     ```csharp
     [CollectionDefinition("BlogPostIntegration")]
     public sealed class BlogPostIntegrationCollection
         : ICollectionFixture<MongoDbFixture> { }
     ```

   - Apply the collection attribute to each test class:

     ```csharp
     [Collection("BlogPostIntegration")]
     public sealed class MongoDbBlogPostRepositoryTests(MongoDbFixture fixture) { }
     ```

   - Collection names are domain-scoped: `BlogPostIntegration`, `AuthorIntegration`,
     `CommentIntegration` (for future).
   - Do NOT reuse generic collection names like `"MongoDb"` or `"Integration"`.

2. **Per-test database isolation using unique database names**
   - Every test must write to its own isolated database.
   - Generate unique names with: `$"T{Guid.NewGuid():N}"` (produces `T` prefix + 32-char GUID).
   - Pattern in `CreateRepo()` helper:

     ```csharp
     private MongoDbBlogPostRepository CreateRepo(string? dbName = null) =>
         new(fixture.CreateFactory(dbName ?? $"T{Guid.NewGuid():N}"));
     ```

   - xUnit creates a fresh test-class instance per test method, so constructor or
     method-level database creation ensures isolation.

3. **Fixture responsibility isolation**
   - `MongoDbFixture` **ONLY**:
     - Starts the MongoDB container in `InitializeAsync()`.
     - Exposes `ConnectionString` (read-only property).
     - Disposes the container in `DisposeAsync()`.
     - Provides `CreateFactory(dbName)` to yield `IDbContextFactory<BlogDbContext>`.
   - `MongoDbFixture` does **NOT**:
     - Manage test data or seeding.
     - Clear databases between tests (xUnit isolation + unique names handle this).
     - Define any test-specific logic.

4. **xUnit configuration for collection-level parallelism**
   - File: `tests/Integration.Tests/xunit.runner.json`
   - Current configuration (✅ correct):

     ```json
     {
       "parallelizeAssembly": false,
       "parallelizeTestCollections": true
     }
     ```

   - Rationale: No tests run in parallel within a single collection (one fixture per
     collection). Different collections CAN run in parallel (different containers).
   - Future scale: If 3–5 domain collections exist, parallelization will become
     a visible performance win.

### Real MyBlog Examples

**Collection Definition** (`tests/Integration.Tests/Infrastructure/BlogPostIntegrationCollection.cs`):

```csharp
[CollectionDefinition("BlogPostIntegration")]
public sealed class BlogPostIntegrationCollection
    : ICollectionFixture<MongoDbFixture> { }
```

**Test Class** (`tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs`):

```csharp
[Collection("BlogPostIntegration")]
public sealed class MongoDbBlogPostRepositoryTests(MongoDbFixture fixture)
{
    private MongoDbBlogPostRepository CreateRepo(string? dbName = null) =>
        new(fixture.CreateFactory(dbName ?? $"T{Guid.NewGuid():N}"));

    [Fact]
    public async Task AddAsync_persists_post_to_MongoDB()
    {
        // Arrange
        var repo = CreateRepo();
        var post = BlogPost.Create("Hello World", "Some content", "Author A");

        // Act
        await repo.AddAsync(post);

        // Assert
        var all = await repo.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].Title.Should().Be("Hello World");
    }
}
```

**Fixture Implementation** (`tests/Integration.Tests/Infrastructure/MongoDbFixture.cs`):

```csharp
public sealed class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder().Build();
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public IDbContextFactory<BlogDbContext> CreateFactory(string dbName) =>
        new TestContextFactory(ConnectionString, dbName);

    private sealed class TestContextFactory(string connectionString, string dbName)
        : IDbContextFactory<BlogDbContext>
    {
        public BlogDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BlogDbContext>()
                .UseMongoDB(connectionString, dbName)
                .Options;
            return new BlogDbContext(options);
        }
        public Task<BlogDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(CreateDbContext());
    }
}
```

### Next-step guidance for new domains

1. Create a new collection definition file:

   ```csharp
   [CollectionDefinition("{Entity}Integration")]
   public sealed class {Entity}IntegrationCollection
       : ICollectionFixture<MongoDbFixture> { }
   ```

   Example: `AuthorIntegrationCollection` for Author domain tests.

2. Create a new test class directory:

   ```
   tests/Integration.Tests/{Entity}/Mongo{Entity}RepositoryTests.cs
   ```

3. Apply the collection attribute and follow the same fixture pattern.

4. Each xUnit collection gets its **own** `MongoDbFixture` instance. xUnit
   handles collection-level isolation automatically, so different collections
   can run in parallel with separate fixtures and containers.

5. Verify `xunit.runner.json` still has `parallelizeTestCollections: true`.

### Current Test Coverage

- **BlogPostIntegration**: 9 tests (✅ passing)
  - Add/Get/Update/Delete operations
  - Ordering (newest first)
  - Concurrency conflict handling
  - Empty repository behavior

### Explicit Rejections

- **Rejected:** Imported performance claims from source repo ("46 seconds → 2
  seconds"). MyBlog's current measurement is ~2–3s per fixture startup, which is
  acceptable for the MVP scope.
- **Rejected:** Imported domain collections (`CategoryIntegration`,
  `IssueIntegration`, `CommentIntegration`, `StatusIntegration`) that don't exist
  in MyBlog yet. Create them only when the corresponding repository/handler tests
  are written.
- **Rejected:** Putting `IAsyncLifetime` on integration test classes. Only
  `MongoDbFixture` implements it. Test classes do not.
- **Rejected:** Seeding shared test data in the fixture. Each test is responsible
  for arranging its own data within its isolated database.
