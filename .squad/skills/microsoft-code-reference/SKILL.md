---
name: microsoft-code-reference
description: DevOps-focused Microsoft API reference for NuGet, Aspire AppHost, GitHub Actions, and .NET SDK verification. Use to verify NuGet package versions, Aspire resource APIs, .NET SDK/target framework compatibility, GitHub Actions SDK calls, and troubleshoot integration issues in CI/CD and app configuration.
compatibility: Requires Microsoft Learn MCP Server (https://learn.microsoft.com/api/mcp)
owner: Boromir (DevOps)
scope: MyBlog CI/CD, Aspire AppHost, NuGet package management, GitHub Actions workflows
---

# Microsoft Code Reference — DevOps Focus

**Owner:** Boromir (DevOps)  
**Scope:** MyBlog CI/CD, Aspire AppHost, NuGet management, GitHub Actions workflows, .NET SDK integration  

Verify Microsoft APIs, NuGet packages, Aspire resource naming, and GitHub Actions SDK calls to prevent version mismatches, incorrect resource wiring, and CI/CD failures.

## Common DevOps Use Cases

| Scenario | Root Problem | Query | Expected Outcome |
| ---------- | -------------- | ------- | ------------------ |
| AppHost resource won't start | Wrong method name or parameter | `"Aspire.Hosting MongoDB AddMongoDB"` | Confirm `AddMongoDB(name)` signature |
| NuGet version conflict | Version mismatch or package targeting wrong framework | `"NuGet Aspire.Hosting.MongoDB version"` | Verify latest stable version and confirm package targets .NET 10 per global.json |
| GitHub Actions checkout fails | Outdated action or incorrect parameters | `"GitHub Actions checkout v4 fetch-depth"` | Confirm v4 supports `fetch-depth: 0` |
| .NET SDK/target framework incompatibility | .NET SDK mismatch (global.json) or package incompatibility | `".NET 10 Aspire 13 compatibility"` | Verify Aspire 13.x works with .NET 10 per global.json |
| AppHost resource naming mismatch | Inconsistent resource naming between AppHost and ServiceDefaults | `"Aspire resource naming conventions"` | Ensure resource names match across `AddMongoDB("mongodb")` and service wiring |

## Tools

| Need | Tool | Example |
| ------ | ------ | --------- |
| Aspire SDK method/resource lookup | `microsoft_docs_search` | `"Aspire.Hosting AddMongoDB resource name"` |
| NuGet package version & compatibility | `microsoft_code_sample_search` | `query: "Aspire.Hosting.MongoDB net10", language: "csharp"` |
| Full API reference (overloads, parameters) | `microsoft_docs_fetch` | Fetch URL from `microsoft_docs_search` |

## NuGet Package Verification

MyBlog specifies NuGet package versions in individual `.csproj` files with `<PackageReference>` and targets .NET 10 (specified in `global.json`). When adding/updating a package:

```
# Verify package name and latest version
microsoft_docs_search("Aspire.Hosting.MongoDB NuGet latest")

# Check compatibility with .NET 10 and Aspire 13
microsoft_code_sample_search(query: "Aspire.Hosting.MongoDB net10", language: "csharp")

# Look for breaking changes between versions
microsoft_docs_search("Aspire 13 breaking changes from 12")
```

**Key packages MyBlog uses:**

- `Aspire.AppHost.Sdk` (13.2.2) — AppHost runtime
- `Aspire.Hosting.MongoDB` (13.2.2) — MongoDB orchestration
- `Aspire.Hosting.Redis` (13.2.2) — Redis orchestration
- `Aspire.ServiceDefaults` — Common service configuration

Always check for version alignment across Aspire packages.

## Aspire AppHost Resource Verification

AppHost wiring uses resource names (e.g., `builder.AddMongoDB("mongodb")`) that must be consistent with service references. Verify:

```
# Confirm resource registration method exists
"Aspire.Hosting DistributedApplication AddMongoDB AddRedis"

# Check if resource can be referenced correctly
"Aspire resource WithReference documentation"

# Verify WaitFor behavior
"Aspire.Hosting WaitFor method"
```

**MyBlog Aspire pattern:**

```csharp
var mongo = builder.AddMongoDB("mongodb");        // Resource named "mongodb"
var mongoDb = mongo.AddDatabase("myblog");         // Database named "myblog"
var redis = builder.AddRedis("redis");             // Resource named "redis"

builder.AddProject<Projects.Web>("web")
    .WithReference(mongoDb)                        // Reference passes resource + database
    .WithReference(redis)
    .WaitFor(mongo)                                // Wait for orchestration
    .WaitFor(redis);
```

When updating AppHost, verify:

- Resource names are consistent across `AddMongoDB()` and client service wiring
- `AddDatabase()` returns correct database reference type
- `WithReference()` accepts the resource type
- `WaitFor()` works with both `AddMongoDB()` and `AddRedis()` return types

## GitHub Actions Workflow Verification

MyBlog CI runs on GitHub Actions with dotnet restore, build, and test stages. When updating workflow:

```
# Verify action version and parameters
"GitHub Actions setup-dotnet@v4 global-json-file"

# Check matrix build syntax for multi-target support
"GitHub Actions matrix strategy"

# Verify cache key format for .NET projects
"GitHub Actions cache .csproj Directory.Packages.props"
```

**MyBlog CI workflow uses:**

- `actions/setup-dotnet@v4` with `global-json-file: global.json`
- `actions/cache@v4` with key: `${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}`
- `gittools/actions/gitversion` for semantic versioning
- `dorny/test-reporter@v1` for test result publishing

When modifying the workflow, verify action parameters and cache hit rates against `.csproj` changes.

## .NET SDK / Target Framework Compatibility

MyBlog targets .NET 10 SDK (specified in `global.json` with version 10.0.100) and Aspire 13.2.2. Verify compatibility before upgrading:

```
# Check .NET 10 support for Aspire
"Aspire 13 .NET 10 compatibility"

# Verify NuGet package targets .NET 10
"[PackageName] .NET 10 target framework"

# Check for .NET API changes
".NET 10 breaking changes from 9"
```

Use `global.json` (currently `sdk.version: 10.0.100`) as source of truth for .NET SDK version. Ensure all packages and Aspire components support the target framework.

## Error Troubleshooting

| Error | Query |
| ------- | ------- |
| `Resource not found in distributed application` | `"Aspire.Hosting resource naming lifecycle"` |
| `Cannot convert resource to reference type` | `"Aspire WithReference type compatibility"` |
| NuGet restore fails with version conflict | `"NuGet [PackageName] conflicts v[X] vs v[Y]"` |
| AppHost won't start: method not found | `"Aspire.Hosting [MethodName] signature"` → fetch full page |
| GitHub Actions workflow syntax error | `"GitHub Actions [feature] [version]"` |
| Test project target mismatch | `".NET 10 xUnit test support"` |

## Validation Workflow for DevOps Changes

Before deploying CI/CD or Aspire changes:

1. **Verify package/method exists** — `microsoft_docs_search(query: "[PackageName/Resource] [Method]")`
2. **Check compatibility** — `microsoft_code_sample_search(query: "[feature] net10", language: "csharp")`
3. **Fetch full details** (if method has overloads) — `microsoft_docs_fetch(url: "...")`
4. **Test locally** — Run `dotnet build`, `dotnet test`, or AppHost startup before committing

For AppHost changes: verify resource names are consistent and test with `dotnet run` in AppHost project.  
For NuGet changes: verify package versions in `.csproj` files and run CI locally with `dotnet build MyBlog.slnx`.  
For GitHub Actions changes: test workflow syntax and cache keys with a dry-run commit to a test branch.
