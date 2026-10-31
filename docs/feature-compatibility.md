# Ardalis.Specification 9.x: feature compatibility

**Scope:** the current RepositoryExample design built on EF Core 10. “Available” means the same **scenario** can be expressed through native LINQ, not that there is a method-for-method Ardalis API clone.

| Ardalis feature | RepositoryExample route | Status / warning |
| --- | --- | --- |
| Where, conditional Where | LINQ Where, if statements | Available |
| OrderBy, ThenBy | LINQ | Available; stable paging needs a tiebreaker |
| Skip, Take | LINQ | Available; count before paging |
| Select, SelectMany | LINQ, IQuery<TEntity,TDto> | Available |
| Include, ThenInclude | EF Core | Available |
| AsNoTracking, AsTracking | EF Core | Available |
| AsSplitQuery, AsNoTrackingWithIdentityResolution | EF Core | Available |
| IgnoreQueryFilters, IgnoreAutoIncludes, TagWith | EF Core | Available; check security assumptions |
| Search | LINQ predicates / EF.Functions.Like | Requires case/collation/escaping review |
| FirstOrDefault, SingleOrDefault | IQueryExecutor | Available; preserve failure semantics |
| CountAsync(spec), AnyAsync(spec) | IQueryExecutor | Available; count criteria before paging |
| Page and total count | IQueryExecutor | Available; two SQL statements |
| WithProjectionOf | Reusable IQueryable extensions | Alternative, not built-in spec merge |
| PostProcessingAction | Materialize then process in IQueryExecutor | Available; check page/count ordering |
| IsSatisfiedBy | Separate domain predicate | Not built into Query Object |
| In-memory specification evaluator | IQueryable.AsQueryable for pure LINQ | Partial; async and EF methods differ |
| EnableCache/cache key | External policy/decorator | Not built in |
| Custom evaluator pipeline | LINQ extensions / explicit policies | Different architecture |
| AsAsyncEnumerable streaming | None with managed context lifetime | Gap |
| Entity root queries | IQuery<TEntity> | Available |
| DTO queries | IQuery<TEntity,TDto> | Available |
| Context-wide joins | IContextQuery<TDto> | Available |
| Complex arbitrary async result | IQueryExecutor / IContextQueryExecutor | Available |
| ExecuteUpdateAsync / ExecuteDeleteAsync | IRepository bulk methods | Available |
| RepositoryBase<T> CRUD | IRepository CRUD | Explicit SaveChanges is required |
| EF6 | None | Not supported |

## What is actually covered by tests?

Repository/tests/Repository.IntegrationTests/ArdalisMigrationParityTests.cs compares Ardalis.Specification.EntityFrameworkCore **9.3.1** with RepositoryExample, using actual EF Core SQLite queries:

- Filtering, ordering, projection and presence of WHERE and ORDER BY in SQL
- Include + no-tracking navigation loading
- Paged result with criteria-only total count
- FirstOrDefault and Any
- Bulk update/delete against independently seeded databases
- A cross-entity IContextQuery example (not a direct Ardalis comparison)

**Not yet tested:** full Search parity, provider-specific collations, WithProjectionOf, streaming, post-processing, identity resolution, global-filter security, SQL Server/PostgreSQL SQL, caching or all transaction semantics.

These tests establish **scenario parity on SQLite**, not total feature or API parity. They must run in CI before parity claims are treated as verified.

## Non-goals

Do not introduce Query.Where / Query.OrderBy builders, stored specification state or a SpecificationEvaluator just to mimic Ardalis syntax. The intended design is a thin wrapper around **native LINQ**.

See [migration guide](ardalis-migration.md) and [testing](testing.md).