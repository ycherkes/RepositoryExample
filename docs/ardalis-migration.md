# Migrate from Ardalis.Specification to RepositoryExample

This guide targets **Ardalis.Specification 9.x** and **Ardalis.Specification.EntityFrameworkCore 9.x**. It is **not a drop-in replacement or API-compatible fork**. It replaces specification query state with ordinary LINQ query objects, keeping EF Core responsible for query translation.

See [feature compatibility](feature-compatibility.md), [architecture](architecture.md) and the executable comparison tests in Repository/tests/Repository.IntegrationTests/ArdalisMigrationParityTests.cs.

## Choose the right contract

| Ardalis use case | RepositoryExample contract |
| --- | --- |
| ListAsync(spec) returning entities | IQuery<TEntity> |
| ListAsync(spec) returning DTOs | IQuery<TEntity, TResultItem> |
| FirstOrDefault, Single, Count, Any | IQueryExecutor<TEntity, TResult> |
| Page + total count / aggregation / arbitrary response | IQueryExecutor<TEntity, TResult> |
| Multi-entity or context-wide query | IContextQuery<TResultItem> / IContextQueryExecutor<TResult> |
| In-memory IsSatisfiedBy domain rule | Keep a domain predicate, not a query object |
| Streaming | No safe built-in equivalent; plan a context lifetime policy |

A regular IQuery returns an **unexecuted IQueryable**; the repository calls ToListAsync. IQueryExecutor owns its terminal LINQ operations and returns any result type.

## 1. Filtering, ordering, projections

**Before — Ardalis:**

~~~csharp
public sealed class ProductsByCategorySpec : Specification<Product, ProductProjection>
{
    public ProductsByCategorySpec(string category)
    {
        Query.Where(p => p.Category.Name == category)
             .OrderBy(p => p.Price)
             .Select(p => new ProductProjection
             {
                 Name = p.Name,
                 Price = p.Price,
                 CategoryName = p.Category.Name
             });
    }
}

var items = await ardalisRepository.ListAsync(
    new ProductsByCategorySpec("Fruit"), cancellationToken);
~~~

**After — RepositoryExample:**

~~~csharp
public sealed class ProductsByCategoryQuery(string category)
    : IQuery<Product, ProductProjection>
{
    public IQueryable<ProductProjection> Apply(IQueryable<Product> products) =>
        products.Where(p => p.Category.Name == category)
            .OrderBy(p => p.Price)
            .Select(p => new ProductProjection
            {
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category.Name
            });
}

var items = await repository.GetAsync(
    new ProductsByCategoryQuery("Fruit"), cancellationToken);
~~~

The query is still executed by EF Core, not by a new evaluator.

## 2. Include and EF-specific features

Translate Ardalis methods to **native EF Core extensions** inside Apply:

~~~csharp
public sealed class ProductWithCategoryQuery(int id) : IQuery<Product>
{
    public IQueryable<Product> Apply(IQueryable<Product> products) =>
        products.Where(p => p.Id == id)
            .Include(p => p.Category)
            .AsNoTracking()
            .TagWith("ProductWithCategoryQuery");
}
~~~

Also available through EF Core: ThenInclude, AsTracking, AsSplitQuery, AsNoTrackingWithIdentityResolution, IgnoreQueryFilters, IgnoreAutoIncludes. Prefer projecting needed columns to eager-loading entire graphs if possible.

## 3. CountAsync(spec) + paging: critical behavior difference

Ardalis CountAsync(spec) evaluates **criteria only**, ignoring pagination. Counting *after* Skip/Take during migration is a correctness bug.

~~~csharp
public sealed record ProductPage(IReadOnlyList<ProductProjection> Items, int TotalCount);

public sealed class ProductPageQuery(int skip, int take)
    : IQueryExecutor<Product, ProductPage>
{
    public async Task<ProductPage> ApplyAsync(
        IQueryable<Product> products, CancellationToken ct)
    {
        var filtered = products.Where(p => p.Category.Name == "Fruit");
        var total = await filtered.CountAsync(ct); // before paging
        var items = await filtered
            .OrderBy(p => p.Id) // stable, unique tie-breaker
            .Skip(skip).Take(take)
            .Select(p => new ProductProjection
            {
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category.Name
            }).ToListAsync(ct);

        return new ProductPage(items, total);
    }
}
~~~

Validate skip/take bounds. Two SQL statements can observe different committed data; use suitable transaction isolation if page and count must share one snapshot.

## 4. Single values and aggregates

Use IQueryExecutor, which returns Task<TResult> instead of an IQueryable list:

~~~csharp
public sealed class ProductByNameQuery(string name)
    : IQueryExecutor<Product, Product?>
{
    public Task<Product?> ApplyAsync(
        IQueryable<Product> products, CancellationToken ct) =>
        products.FirstOrDefaultAsync(p => p.Name == name, ct);
}

Product? product = await repository.GetAsync(
    new ProductByNameQuery("Apple"), cancellationToken);
~~~

For SingleOrDefault, retain the original exception semantics. Any and Count should operate on the intended filtered source, not silently count only a page.

## 5. Reuse query fragments instead of WithProjectionOf

Use ordinary IQueryable extension methods for common predicates/shapes:

~~~csharp
public static class ProductQueryParts
{
    public static IQueryable<Product> InCategory(
        this IQueryable<Product> products, string category) =>
        products.Where(p => p.Category.Name == category);
}

// Inside Apply: products.InCategory(category).Select(...)
~~~

This reuses native LINQ operations without merging two potentially incompatible query plans. It is an **alternative**, not a feature-identical implementation of WithProjectionOf.

## 6. Search and PostProcessingAction

- Search: translate to Where, OR/AND predicates, and optionally EF.Functions.Like. Verify grouped criteria, escaping, case/collation and wildcard rules; there is **no guaranteed universal 1:1 rewrite**.
- PostProcessingAction: materialize in IQueryExecutor, then perform explicitly requested in-memory work. Be precise about whether count/paging applies before or after processing.
- EnableCache: Ardalis stores cache metadata; it does not provide a cache store. Prefer an optional application-level cache policy/decorator instead of adding a query evaluator.
- Custom evaluators: use regular LINQ/EF extensions or cross-cutting repository policies; do not recreate a second query DSL.

## 7. CRUD and SaveChanges: important difference

Ardalis RepositoryBase<T>.AddAsync / UpdateAsync / DeleteAsync **automatically save changes**. RepositoryExample Add / Update / Remove do **not** save.

~~~csharp
repository.Add(product);
await repository.SaveChangesAsync(cancellationToken);
~~~

Review transaction boundaries, error timing and unit-of-work assumptions. Current IRepository exposes EF Core types such as EntityEntry and IDbContextTransaction. It is a **thin EF Core abstraction**, not an ORM-neutral repository.

## 8. Migration verification

1. Run the [comparison suite](testing.md) using .NET SDK 10 and SQLite.
2. Verify output ordering, projection, null handling, tracking and exception behavior.
3. Inspect ToQueryString() for important SQL shape; do not assume byte-identical SQL.
4. Validate collation, Search, global filters, query translation, transaction isolation and performance on your **production provider**.
5. Migrate writes separately and explicitly test SaveChanges behavior.

RepositoryExample intentionally does **not** support the Ardalis builder API; it supports equivalent query scenarios using standard LINQ.
