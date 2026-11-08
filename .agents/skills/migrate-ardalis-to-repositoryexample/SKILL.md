---
name: migrate-ardalis-to-repositoryexample
description: Migrate existing C# and EF Core code from Ardalis.Specification or Ardalis.Specification.EntityFrameworkCore 9.x to ycherkes/RepositoryExample Query Objects. Use for Specification<T>, Specification<T,TResult>, RepositoryBase CRUD, ListAsync(spec), GetBySpecAsync, FirstOrDefaultAsync, CountAsync(spec), paging, Search, or WithProjectionOf rewrites. Do not use for brand-new queries with no Ardalis dependency.
---

# Migrate Ardalis.Specification to RepositoryExample

**Objective:** preserve application behavior while replacing Ardalis's query-builder/evaluator with the minimal **native LINQ** Query Object interfaces. This is a *migration*, not a drop-in API replacement. Do not generate Ardalis-style fluent builders, specification state, or an evaluator in the target code.

Read the repository's `docs/ardalis-migration.md`, `docs/feature-compatibility.md`, `docs/architecture.md`, and `docs/testing.md`. Inspect `Repository/tests/Repository.IntegrationTests/ArdalisMigrationParityTests.cs` for executable examples. For consumer repositories, use the versions of `IQuery`, `IRepository` and EF Core present there, not assumed names. Canonical source: https://github.com/ycherkes/RepositoryExample.

## Migration workflow

1. **Inventory** the current `Specification<TEntity>`, `Specification<TEntity,TResult>`, custom evaluators, `ISpecification` uses, repository interfaces/implementations, and `ListAsync`/`FirstOrDefaultAsync`/`SingleOrDefaultAsync`/`CountAsync`/`AnyAsync` callers. Record all extensions (`Include`, `Search`, `WithProjectionOf`, `PostProcessingAction`, caching, tracking, global filters) and SaveChanges responsibilities.
2. **Describe original behavior** before editing: result cardinality and type, filters, sort order, page and total count, null semantics, duplicate detection, tracking, exceptions, and whether post-processing affects results.
3. **Choose the new contract** using the table below. Keep application query parameters as constructor arguments; use IQueryable operations in `Apply`. Do not materialize early.
4. **Translate** each specification explicitly to native LINQ / EF Core. Replace repository calls with `repository.GetAsync(new NamedQuery(...), ct)`; adjust signatures and DI interfaces only where the actual consuming project needs it.
5. **Migrate writes separately**: Ardalis's EF `RepositoryBase<T>` typically saves in its CRUD methods; this sample's `IRepository.Add/Update/Remove` do not. Add `await repository.SaveChangesAsync(ct)` at the correct unit-of-work boundary, not blindly after every line.
6. **Verify semantics**, using side-by-side integration tests before removing Ardalis packages. Change dependencies only after all callers and adapters have been migrated; do not remove Ardalis if other usages remain.
7. **Report** matched behaviors, intentionally changed behaviors, unsupported cases, tests executed, and remaining production-provider risks.

## Contract and call mapping

| Ardalis use | RepositoryExample |
| --- | --- |
| `Specification<TEntity>` used with `ListAsync(spec)` | `IQuery<TEntity>` + `GetAsync(query, ct)` => `List<TEntity>` |
| `Specification<TEntity,TDto>` / projected `ListAsync` | `IQuery<TEntity,TDto>` + `GetAsync(query, ct)` => `List<TDto>` |
| `FirstOrDefaultAsync` / `SingleOrDefaultAsync` | `IQueryExecutor<TEntity,TEntity?>` using corresponding EF Core terminal operator |
| `CountAsync(spec)`, `AnyAsync(spec)`, aggregate | `IQueryExecutor<TEntity,int/bool/...>` |
| Results **plus** total count | `IQueryExecutor<TEntity,TPage>` with two explicit queries |
| Multiple entity sets | `IContextQuery<TDto>` or `IContextQueryExecutor<TResult>` |
| `Query.Where/OrderBy/Skip/Take/Select` | Native `Where/OrderBy/Skip/Take/Select` |
| `Query.Include/ThenInclude`, tracking flags, split query | Native EF Core extension methods |
| `AddAsync/UpdateAsync/DeleteAsync` | `Add/Update/Remove` **and explicitly `SaveChangesAsync`** |
| In-memory `IsSatisfiedBy` business rule | Keep a separate domain predicate; **not** a database query object |

For `IQuery<TEntity,TDto>`, the implementation returns `IQueryable<TDto>`; the repository owns `ToListAsync`. For `IQueryExecutor<TEntity,TResult>`, the implementation returns `Task<TResult>` and owns terminal async operations. `IContextQuery` accepts a `DbContext`, so these contracts are EF-specific, not ORM-neutral.

## Worked rewrite: filtering and projection

**Before (Ardalis):**

```csharp
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

var results = await ardalisRepository.ListAsync(
    new ProductsByCategorySpec("Fruit"), cancellationToken);
```

**After (RepositoryExample):**

```csharp
public sealed class ProductsByCategoryQuery(string category)
    : IQuery<Product, ProductProjection>
{
    public IQueryable<ProductProjection> Apply(IQueryable<Product> products) =>
        products.Where(p => p.Category.Name == category)
            .OrderBy(p => p.Price).ThenBy(p => p.Id)
            .Select(p => new ProductProjection
            {
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category.Name
            });
}

var results = await repository.GetAsync(
    new ProductsByCategoryQuery("Fruit"), cancellationToken);
```

The unique tie-breaker strengthens deterministic ordering, especially for later pagination. Confirm whether adding it changes observable expectations.

## High-risk compatibility cases

### Count with paging

Ardalis `CountAsync(spec)` evaluates **criteria only**, ignoring `Skip/Take`. The equivalent executor must count the filtered source *before* pagination:

```csharp
public sealed record ProductPage(IReadOnlyList<ProductProjection> Items, int TotalCount);

public sealed class ProductPageQuery(int skip, int take)
    : IQueryExecutor<Product, ProductPage>
{
    public async Task<ProductPage> ApplyAsync(
        IQueryable<Product> products, CancellationToken ct)
    {
        if (skip < 0 || take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take));

        var filtered = products.Where(p => p.Category.Name == "Fruit");
        int count = await filtered.CountAsync(ct);
        var items = await filtered.OrderBy(p => p.Price).ThenBy(p => p.Id)
            .Skip(skip).Take(take)
            .Select(p => new ProductProjection
            {
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category.Name
            })
            .ToListAsync(ct);
        return new ProductPage(items, count);
    }
}
```

Two statements may see different data under ordinary isolation; assess transaction/snapshot requirements.

### Terminal semantics

Keep `FirstOrDefault` vs `SingleOrDefault` behavior (including the duplicate-match exception), ordering, default/null results and `AnyAsync` meaning. Do not implement single-result calls by `GetAsync(IQuery)` and then `FirstOrDefault` in memory.

### Search, projections, and post-processing

- `Search` has no universal text-search rewrite. Define whether terms are OR/AND grouped; verify wildcards, `EF.Functions.Like` escaping, collation and case sensitivity on the **real provider**.
- `WithProjectionOf` has no direct API equivalent. Reuse explicit `IQueryable<T>` extension fragments and project once; validate joins, includes and ordering.
- `PostProcessingAction` must be made explicit *after* materialization in an `IQueryExecutor`. Carefully preserve whether filtering, sorting and pagination occurred in the DB or in memory. Don't move all work client-side accidentally.
- `EnableCache` specifies metadata, not a cache store: implement a separate policy/decorator only if the existing application needs it.
- Custom evaluator logic must be inspected, not discarded automatically. Re-express in LINQ or a well-defined repository policy where feasible.

### Tracking, writes, and missing features

- Preserve `AsNoTracking`, identity resolution, `AsSplitQuery`, `IgnoreQueryFilters`, `IgnoreAutoIncludes` and tagging as relevant. Review tenant/global-filter and authorization consequences.
- For tracked entity writes, persist explicitly with `SaveChangesAsync`; ensure transaction, exception timing, concurrency, and unit-of-work behavior are tested.
- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` execute directly and bypass tracking; use only for eligible filtered entity queries with intentional side effects.
- Streaming, `IsSatisfiedBy`, in-memory EF evaluator parity, and arbitrary evaluator plugins are **not** built-in replacements. Document the gap; do not promise exact compatibility.

## Verification and acceptance

- Migrate **one representative vertical slice** first: query class, application call, and tests. Continue in small batches.
- Test old and new implementations with identical seeded data using SQLite, as in `ArdalisMigrationParityTests.cs`. Assert returned values **and order**, nullable results, duplicate exceptions, tracking, unpaged counts, and post-processing where used.
- Inspect `ToQueryString()` for key SQL clauses without requiring byte-identical SQL.
- Run `dotnet restore Repository/Examples.slnx` and `dotnet test Repository/Examples.slnx --configuration Release --no-restore` **when in the sample repository**. In a consuming app, run its own test commands.
- Check translation, search/collation, null comparisons, filters, performance, and transaction semantics on the deployment provider (SQLite results alone cannot certify SQL Server or PostgreSQL parity).
- Do **not** claim feature/API parity beyond scenarios actually tested. Keep `docs/feature-compatibility.md` honest if new parity cases are added.
