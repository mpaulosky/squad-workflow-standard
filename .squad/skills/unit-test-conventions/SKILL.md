---
name: unit-test-conventions
confidence: high
description: >
  MyBlog unit test authoring conventions for domain entities, handlers, helpers,
  and components. Covers file headers, AAA pattern, mocking patterns,
  FluentAssertions, and bUnit component testing.
---

## MyBlog Unit Test Conventions

### Scope

This skill covers unit tests in `tests/Unit.Tests/`, NOT integration tests
(which have their own fixture patterns in `tests/Integration.Tests/`).

- **Domain entity tests**: DTOs, value objects, aggregates (e.g., `BlogPostTests.cs`)
- **Handler unit tests**: Query/Command handlers with mocked dependencies (e.g., `GetBlogPostsHandlerTests.cs`)
- **Component tests**: Blazor components via bUnit (e.g., `NavMenuTests.cs`, `ProfileTests.cs`)
- **Helper/utility tests**: Any non-domain, non-handler logic

### File Header (Charter Convention)

Every `.cs` test file should have a 7-line copyright block at the top:

```csharp
//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     {FileName}.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================
```

**Notes:**

- Year: Current year
- Project Name: Always `Unit.Tests` for test files in `tests/Unit.Tests/`
- Format: Single-line comments (`//`), no blank lines within block
- Placement: Line 1 of file, before any usings or namespaces
- **Current repo state:** Test files created recently have headers; older test files may lack them.
  Add headers when creating new files or significantly refactoring existing tests.

**Example from repo** (MongoDbBlogPostRepositoryTests.cs):

```csharp
//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbBlogPostRepositoryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Integration.Tests
//=======================================================
```

### Test Namespace Pattern

```
MyBlog.Unit.Tests.{Folder}
```

Examples:

- `MyBlog.Unit.Tests.Handlers` — `GetBlogPostsHandlerTests.cs`
- `MyBlog.Unit.Tests.Components.Layout` — `NavMenuTests.cs`
- `MyBlog.Unit.Tests.Features.UserManagement` — `ProfileTests.cs`
- `MyBlog.Unit.Tests` — `BlogPostTests.cs` (domain entity tests live at root)

### AAA Pattern with Comments (Preferred Convention)

The recommended pattern for test methods is Arrange / Act / Assert with explicit comments:

```csharp
[Fact]
public void Create_WithValidArgs_ReturnsBlogPost()
{
    // Arrange
    var title = "Test Title";
    var content = "Test Content";
    var author = "Test Author";

    // Act
    var post = BlogPost.Create(title, content, author);

    // Assert
    post.Title.Should().Be("Test Title");
    post.Content.Should().Be("Test Content");
    post.Author.Should().Be("Test Author");
}
```

**Current repo state:** Some existing tests (e.g., `BlogPostTests.cs`) do not yet use AAA comments.
When writing new tests or modifying existing ones, adopt AAA comments to improve clarity.

**Why comments matter:**

- Forces deliberate test design (Arrange step often catches unclear test intent)
- Makes test expectations obvious at a glance
- Establishes a consistent pattern across the test suite

**Async variant:**

```csharp
[Fact]
public async Task Handle_Success_CreatesPost()
{
    // Arrange
    var command = new CreateBlogPostCommand("Title", "Content", "Author");
    var repo = Substitute.For<IBlogPostRepository>();

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Success.Should().BeTrue();
}
```

### Assertions: FluentAssertions `.Should()` (Critical Rule: Use everywhere)

MyBlog uses **FluentAssertions** exclusively. All assertions must use the
`.Should()` fluent chain.

**✅ Correct:**

```csharp
post.Title.Should().Be("Expected Title");
result.Success.Should().BeTrue();
list.Should().HaveCount(3);
```

**❌ Wrong:**

```csharp
Assert.Equal("Expected Title", post.Title);
post.Title == "Expected Title" || throw...
```

**Common assertions for MyBlog:**

```csharp
// Collections
list.Should().BeEmpty();
list.Should().HaveCount(2);
list.Should().Contain(item);
list.Should().OnlyContain(x => x.IsPublished);

// Strings
title.Should().Be("Expected");
title.Should().BeNullOrEmpty();
title.Should().Contain("substring");

// Exceptions
act.Should().Throw<ArgumentException>();
await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

// Objects
result.Should().NotBeNull();
result.Should().BeOfType<BlogPostDto>();
result.Value!.Id.Should().NotBeEmpty();

// Booleans
result.Success.Should().BeTrue();
result.Failure.Should().BeFalse();
```

### Mocking with NSubstitute

MyBlog uses **NSubstitute** for all mocking. Test classes typically create
substitutes in the constructor or as fields.

**Pattern for handler tests:**

```csharp
public class GetBlogPostsHandlerTests
{
    private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
    private readonly IMemoryCache _cache = Substitute.For<IMemoryCache>();
    private readonly GetBlogPostsHandler _handler;

    public GetBlogPostsHandlerTests()
    {
        _handler = new GetBlogPostsHandler(_repo, _cache);
    }

    [Fact]
    public async Task Handle_CacheMiss_CallsRepo()
    {
        // Arrange
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<BlogPost> { BlogPost.Create("T", "C", "A") });

        // Act
        var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
```

**IMemoryCache mocking gotcha:**

- `IMemoryCache.Set<T>` is an **extension method** — NSubstitute cannot intercept it.
- `Set<T>` calls `CreateEntry()` internally — **mock `CreateEntry()` instead**:

  ```csharp
  var cacheEntry = Substitute.For<ICacheEntry>();
  _cache.CreateEntry(Arg.Any<object>()).Returns(cacheEntry);
  // Later, verify with:
  _cache.Received(1).CreateEntry(Arg.Any<object>());
  ```

**IMemoryCache.TryGetValue out-param pattern:**

```csharp
object? outVal = null;
_cache.TryGetValue(Arg.Any<object>(), out outVal)
    .Returns(x => { x[1] = (object)cachedValue; return true; });
```

### Domain Entity Tests (BlogPostTests Pattern)

Domain entities like `BlogPost` are tested directly without mocking. Test the `Create()` factory and
lifecycle methods (`Update()`, `Publish()`, `Unpublish()`).

**Real example from repo** (`tests/Unit.Tests/BlogPostTests.cs`):

```csharp
public class BlogPostTests
{
    [Fact]
    public void Create_WithValidArgs_ReturnsBlogPost()
    {
        var post = BlogPost.Create("Test Title", "Test Content", "Test Author");

        post.Id.Should().NotBeEmpty();
        post.Title.Should().Be("Test Title");
        post.Content.Should().Be("Test Content");
        post.Author.Should().Be("Test Author");
        post.IsPublished.Should().BeFalse();
        post.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", "content", "author")]
    [InlineData("title", "", "author")]
    [InlineData("title", "content", "")]
    public void Create_WithBlankArgs_ThrowsArgumentException(
        string title, string content, string author)
    {
        var act = () => BlogPost.Create(title, content, author);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesTitle_AndContent()
    {
        var post = BlogPost.Create("Old Title", "Old Content", "Author");
        post.Update("New Title", "New Content");

        post.Title.Should().Be("New Title");
        post.Content.Should().Be("New Content");
        post.UpdatedAt.Should().NotBeNull();
    }
}
```

Note: These tests do not follow AAA comments (current repo state). When adding new entity tests,
consider adopting the AAA pattern above.

**Guidelines:**

- Use `[Fact]` for single-case tests
- Use `[Theory]` + `[InlineData]` for parameterized tests
- Test happy path + all failure paths
- No mocking (entities are pure logic)

### Handler Tests (Vertical Slice Pattern)

Handler tests mock ALL external dependencies (repo, cache, etc.) and verify
handler logic in isolation.

**Pattern for query handlers:**

```csharp
public class EditBlogPostHandlerTests
{
    private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
    private readonly IMemoryCache _cache = Substitute.For<IMemoryCache>();
    private readonly EditBlogPostHandler _handler;

    public EditBlogPostHandlerTests()
    {
        _cache.CreateEntry(Arg.Any<object>()).Returns(Substitute.For<ICacheEntry>());
        _handler = new EditBlogPostHandler(_repo, _cache);
    }

    [Fact]
    public async Task HandleEdit_Success_UpdatesPostAndInvalidatesCaches()
    {
        // Arrange
        var post = BlogPost.Create("Old Title", "Old Content", "Author");
        var command = new EditBlogPostCommand(post.Id, "New Title", "New Content");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _repo.Received(1).UpdateAsync(post, Arg.Any<CancellationToken>());
        _cache.Received(1).Remove("blog:all");
        post.Title.Should().Be("New Title");
        post.Content.Should().Be("New Content");
    }

    [Fact]
    public async Task HandleEdit_NotFound_ReturnsFailResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var command = new EditBlogPostCommand(id, "T", "C");
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Failure.Should().BeTrue();
        result.Error.Should().Contain(id.ToString());
    }

    [Fact]
    public async Task HandleEdit_ConcurrentUpdate_ReturnsConcurrencyErrorCode()
    {
        // Arrange
        var post = BlogPost.Create("Title", "Content", "Author");
        var command = new EditBlogPostCommand(post.Id, "New Title", "New Content");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        _repo.UpdateAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("conflict", new Exception()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Failure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCode.Concurrency);
    }
}
```

### bUnit Component Tests

Component tests in MyBlog use bUnit's `TestContext` base class. Tests render components with auth context
by calling a `RenderForUser(ClaimsPrincipal)` method and assert rendered markup.

**Real example from repo** (`tests/Unit.Tests/Features/UserManagement/ProfileTests.cs`):

```csharp
public class ProfileTests : BunitContext
{
    [Fact]
    public void Profile_RendersIdentityDetailsRolesPictureAndClaims()
    {
        // Arrange
        var principal = CreatePrincipal(
            name: "Admin User",
            email: "admin@example.com",
            userId: "auth0|123",
            pictureUrl: "https://example.com/avatar.png",
            rolesJson: "[\"Admin\",\"Author\"]",
            extraClaims: [new Claim("department", "Engineering")]);

        // Act
        var cut = RenderForUser(principal);

        // Assert
        cut.Markup.Should().Contain("Admin User");
        cut.Markup.Should().Contain("auth0|123");
    }
}
```

**How it works:**

- Inherit from `BunitContext` (bUnit's `TestContext` base; made available via `global using Bunit;`)
- Call `RenderForUser(principal)` to render the component with authenticated context
- Use `cut.Markup.Should()` to assert the rendered HTML output
- Each test class defines its own `RenderForUser()` and `CreatePrincipal()` helper methods

### Critical Gotchas

**❌ NEVER compare two `{Entity}Dto.Empty` calls**

```csharp
// WRONG:
var dto1 = BlogPostDto.Empty;
var dto2 = BlogPostDto.Empty;
dto1.Should().Be(dto2); // FAILS — Empty calls DateTime.UtcNow each time
```

**✅ CORRECT — Assert individual fields:**

```csharp
var dto = BlogPostDto.Empty;
dto.Id.Should().BeEmpty();
dto.Title.Should().Be("");
dto.Content.Should().Be("");
```

**`GenerateSlug` trailing underscore is correct**

```csharp
"C# Is Great!".GenerateSlug().Should().Be("c_is_great_");
// Trailing underscore is EXPECTED, not a bug
```

### Namespace and File Organization

```
tests/Unit.Tests/
├── BlogPostTests.cs                        # Domain entity tests
├── ResultTests.cs                          # Result<T> utility tests
├── Handlers/
│   ├── GetBlogPostsHandlerTests.cs        # Query handler tests
│   ├── CreateBlogPostHandlerTests.cs
│   ├── EditBlogPostHandlerTests.cs
│   └── DeleteBlogPostHandlerTests.cs
├── Components/
│   ├── Layout/
│   │   └── NavMenuTests.cs                # Component tests
│   └── RazorSmokeTests.cs
├── Features/
│   └── UserManagement/
│       └── ProfileTests.cs
├── Security/
│   └── RoleClaimsHelperTests.cs
└── Testing/
    └── TestAuthorizationService.cs        # Auth mocking helper for bUnit tests
```

### Running Tests Locally

Before pushing, run the full unit test suite:

```bash
dotnet test tests/Unit.Tests --logger "console;verbosity=detailed"
```

Verify all tests pass (zero failures required per Gimli's charter rule #1).

### Test Coverage Goals

- **Domain entities**: ≥80% path coverage
- **Handlers**: ≥85% (all success paths + major error cases)
- **Components**: ≥75% (render paths + auth state variants)
- **Overall repo**: ≥85% line coverage (current: 91.64%)
