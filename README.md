# Repository Pattern example with Query Abstractions for EF Core

This repository provides a **thin abstraction** over Entity Framework Core's `DbContext` to:

- Separate EF Core logic from the rest of your application.
- Make application code **easier to unit test** without depending directly on EF Core.
- Allow **reusable and composable queries** using interfaces like `IQuery` and `IQueryExecutor`.

## Why Use This?

### Problems with directly using `DbContext`
- **Tight coupling:** Application logic directly depends on EF Core.
- **Hard to test:** Unit tests require a real database or in-memory DbContext.
- **Duplicate LINQ logic:** Queries get repeated across services.

### Benefits of this abstraction
- **Decouples DbContext:** Your services depend on an `IRepository` interface.
- **Reusable Queries:** Query definitions are separate classes and can be shared across services.
- **Better testability:** Queries can be tested independently and repository methods mocked.
