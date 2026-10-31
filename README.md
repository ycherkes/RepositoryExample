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

## In 30 seconds

Define a query object that contains the filtering, ordering, and projection:

```csharp
public sealed class ProductsByCategoryQuery(string category)
    : IQuery<Product, ProductSummary>
{
    public IQueryable<ProductSummary> Apply(IQueryable<Product> products) =>
        products
            .Where(product => product.Category.Name == category)
            .OrderBy(product => product.Name)
            .Select(product => new ProductSummary(
                product.Id,
                product.Name,
                product.Price));
}

public sealed record ProductSummary(int Id, string Name, decimal Price);
```

Execute it through the repository; the caller never receives an unexecuted
`IQueryable`:

```csharp
var products = await repository.GetAsync(
    new ProductsByCategoryQuery("Fruit"),
    cancellationToken);
```

The query can be reused wherever that read is needed, while the repository
remains responsible for supplying the entity set and materializing the result.


## Migrating from Ardalis.Specification 9.x

This repository demonstrates a **minimal Query Object alternative** to
[Ardalis.Specification](https://github.com/ardalis/Specification). It is **not an
API-compatible fork**: use native LINQ in query objects rather than recreating
the specification builder and evaluator.

- [Ardalis migration guide](docs/ardalis-migration.md) — before/after examples,
  paging and counting, CRUD/SaveChanges differences, search and post-processing.
- [Feature compatibility matrix](docs/feature-compatibility.md) — native LINQ
  replacements, intentional omissions, and verified vs unverified scenarios.
- [Architecture and trade-offs](docs/architecture.md) — IQuery, IQueryExecutor,
  IContextQuery and their execution boundaries.
- [Run the SQLite parity tests](docs/testing.md) — .NET 10 commands, what
  comparisons prove, and limits of provider-specific testing.

The parity suite in
[ArdalisMigrationParityTests.cs](Repository/tests/Repository.IntegrationTests/ArdalisMigrationParityTests.cs)
uses the MIT-licensed **Ardalis.Specification.EntityFrameworkCore 9.3.1** in
**tests only**. The existing query contracts and repository implementation
remain dependency-free from Ardalis. GitHub Actions runs the comparison suite.

**Important:** this is a thin abstraction **for EF Core**, not an ORM-neutral
repository. Several interfaces intentionally expose EF types. Unlike Ardalis's
built-in RepositoryBase CRUD methods, the example's Add/Update/Remove methods
require an explicit SaveChangesAsync call.
