# Repository Pattern example with Query Abstractions for EF Core

This repository provides a **thin abstraction** over Entity Framework Core's `DbContext` to:

- Separate EF Core logic from the rest of your application.
- Make application code **easier to unit test** without depending directly on EF Core.
- Allow **reusable and composable queries** using interfaces like `IQuery` and `IQueryExecutor`.

## Origin and intent

This example grew out of discussions about repositories that return `IQueryable` directly. Once a repository returns an `IQueryable`, the caller is effectively building a real database query outside the repository. Rather than hiding that fact behind a generic return type, this approach gives the query a small, explicit holder: an `IQuery` implementation.

The holder contains the filtering, ordering, includes, and/or projection. The repository supplies the source query and remains responsible for executing it. This keeps query logic reusable and discoverable while retaining a deliberately small abstraction.

This is a lightweight take on the same general problem addressed by query specifications. [Ardalis.Specification](https://github.com/ardalis/Specification) is a related library that provides a specification evaluator and repository base implementation. Its approach is distinct from [Fowler's Specification pattern](https://martinfowler.com/apsupp/spec.pdf): it is designed to describe and evaluate data-access queries, rather than to model a reusable business-rule predicate.

## Why Use This?

### Problems with directly using `DbContext`
- **Tight coupling:** Application logic directly depends on EF Core.
- **Hard to test:** Unit tests require a real database or in-memory DbContext.
- **Duplicate LINQ logic:** Queries get repeated across services.

### Benefits of this abstraction
- **Decouples DbContext:** Your services depend on an `IRepository` interface.
- **Reusable Queries:** Query definitions are separate classes and can be shared across services.
- **Better testability:** Queries can be tested independently and repository methods mocked.
