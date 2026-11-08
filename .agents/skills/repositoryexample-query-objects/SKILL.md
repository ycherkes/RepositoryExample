---
name: repositoryexample-query-objects
description: Create, extend, or review EF Core Query Object classes using ycherkes/RepositoryExample (IQuery, IQueryExecutor, IContextQuery and IRepository). Use for new reusable filters, DTO projections, sorting, paging, counts, aggregates, cross-entity joins, and bulk operations. For replacing Ardalis.Specification, use migrate-ardalis-to-repositoryexample instead.
---

# Build RepositoryExample query objects

Use the **actual contracts in the target project**, not a generic Specification abstraction. RepositoryExample is a small sample of a thin EF Core repository, not an ORM-neutral framework or a packaged query DSL.

When inside this repository, inspect:
- `Repository/tests/Repository.IntegrationTests/Queries.cs` for the contracts.
- `Repository/tests/Repository.IntegrationTests/IRepository.cs` and `Repository.cs` for execution and mutations.
- `Repository/tests/Repository.IntegrationTests/RepositoryTests.cs` for concrete query classes.
- `docs/architecture.md` and `docs/testing.md` for boundaries and tests.

When working in a *consumer* project, first locate its copied/adapted interfaces and namespaces; use those over examples. The canonical reference is https://github.com/ycherkes/RepositoryExample. Do not claim a NuGet dependency exists.

## Choose the smallest appropriate contract

| Required result | Contract | Implementation |
| --- | --- | --- |
| List of entities | `IQuery<TEntity>` | `IQueryable<TEntity> Apply(IQueryable<TEntity>)` |
| List of projected rows | `IQuery<TEntity, TItem>` | `IQueryable<TItem> Apply(IQueryable<TEntity>)` |
| One item, optional item, bool, count, aggregate, page with total, or any arbitrary result | `IQueryExecutor<TEntity, TResult>` | `Task<TResult> ApplyAsync(IQueryable<TEntity>, CancellationToken)` |
| List involving the full DbContext / multiple roots | `IContextQuery<TItem>` | `IQueryable<TItem> Apply(DbContext)` |
| Arbitrary async result involving the full DbContext | `IContextQueryExecutor<TResult>` | `Task<TResult> ApplyAsync(DbContext, CancellationToken)` |

`IQueryExecutor<TEntity>` is only an alias for `IQueryExecutor<TEntity, TEntity>`, **not** a way to return a list. The repository materializes `IQuery` queries with `ToListAsync`; an executor owns its own terminal operations.

## Procedure

1. Identify the entity root, required output shape, selection criteria, ordering, page/count semantics, and whether tracking is needed.
2. Inspect existing query objects and reusable `IQueryable` extensions; reuse business predicates only if EF Core can translate them.
3. Implement a small named, parameterized query object with the contract selected above. Apply filtering **before** projection; do not use `ToList`, `AsEnumerable`, `Compile`, or client-side transformations inside `IQuery.Apply`.
4. In an executor, use EF Core async terminal operations with the supplied cancellation token. Materialize only intentionally, not as a shortcut to make an untranslatable predicate work.
5. Pass the query into the appropriate `IRepository.GetAsync` overload. Do not return an unexecuted `IQueryable` to the application caller.
6. Add SQLite integration tests for results and behavior, plus provider-specific tests where the production provider matters. Run the repository test suite.

## Patterns that compile against the example contracts

### List of projected rows

```csharp
public sealed record ProductSummary(int Id, string Name, float Price);

public sealed class ProductsByCategoryQuery(string category)
    : IQuery<Product, ProductSummary>
{
    public IQueryable<ProductSummary> Apply(IQueryable<Product> products) =>
        products.Where(p => p.Category.Name == category)
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Select(p => new ProductSummary(p.Id, p.Name, p.Price));
}

List<ProductSummary> items = await repository.GetAsync(
    new ProductsByCategoryQuery("Fruit"), cancellationToken);
```

Projection of navigation properties does not normally require `Include`. Prefer returning only needed columns; use `Include`/`ThenInclude` for entity graph loading when warranted.

### First matching item (not a one-element list)

```csharp
public sealed class ProductByIdQuery(int id)
    : IQueryExecutor<Product, Product?>
{
    public Task<Product?> ApplyAsync(
        IQueryable<Product> products, CancellationToken ct) =>
        products.FirstOrDefaultAsync(p => p.Id == id, ct);
}

Product? product = await repository.GetAsync(
    new ProductByIdQuery(42), cancellationToken);
```

Use `SingleOrDefaultAsync` only when duplicate matches must throw. For existence checks use `AnyAsync`, not `ToListAsync().Any()`.

### Page and *unpaginated* total count

```csharp
public sealed record ProductPage(IReadOnlyList<ProductSummary> Items, int TotalCount);

public sealed class ProductPageQuery(string category, int skip, int take)
    : IQueryExecutor<Product, ProductPage>
{
    public async Task<ProductPage> ApplyAsync(
        IQueryable<Product> products, CancellationToken ct)
    {
        if (skip < 0 || take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take));

        var filtered = products.Where(p => p.Category.Name == category);
        var count = await filtered.CountAsync(ct);
        var items = await filtered.OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip(skip).Take(take)
            .Select(p => new ProductSummary(p.Id, p.Name, p.Price))
            .ToListAsync(ct);
        return new ProductPage(items, count);
    }
}
```

Count on `filtered` **before** `Skip`/`Take`. Two separate SQL queries can observe different snapshots; explicitly choose a transaction/isolation policy if strict consistency is required.

### Cross-entity query

```csharp
public sealed class ProductsWithCategoryQuery : IContextQuery<ProductSummary>
{
    public IQueryable<ProductSummary> Apply(DbContext context) =>
        from product in context.Set<Product>()
        join category in context.Set<Category>()
            on product.CategoryId equals category.Id
        orderby product.Name, product.Id
        select new ProductSummary(product.Id, product.Name, product.Price);
}
```

Use `IContextQueryExecutor<TResult>` when a context-wide query needs a scalar, paging+count, or multiple async commands.

### Bulk operations

```csharp
IQuery<Product> selected = new ProductsAbovePriceQuery(100f);
int deleted = await repository.ExecuteDeleteAsync(selected, cancellationToken);
int updated = await repository.ExecuteUpdateAsync(
    selected,
    setters => setters.SetProperty(p => p.Price, p => p.Price + 1f),
    cancellationToken);
```

These bypass change tracking. Inspect the existing `IRepository` signature rather than assuming standard EF Core `ExecuteUpdateAsync` overloads. Be aware that executing the example above in sequence updates only rows still present after deletion.

## Invariants and review checks

- Prefer `Where`, `OrderBy`/`ThenBy`, `Select`, `Include`, `AsNoTracking`, etc. from **native LINQ / EF Core**; never introduce `Query.Where` or a `SpecificationEvaluator`.
- Stable pagination needs deterministic ordering with a unique tie-breaker; validate page bounds.
- Decide tracking deliberately. Watch global query filters, tenant isolation, N+1 loading, SQL translation, index use, and client evaluation.
- Keep reusable query fragments as `IQueryable<T>` extensions rather than introducing a second query grammar.
- Use the example's explicit `SaveChangesAsync` for tracked writes; `Add`, `Update`, and `Remove` only change the context state.
- Inspect `ToQueryString()` and actual results on SQLite; check collation, null semantics, performance, and provider-only features on the deployment database.
- Deliver: query code, repository usage, relevant tests, and a short note about any unresolved behavior/translation assumptions.
