# Architecture: Query Object, not a domain Specification

This example permits a caller to pass a **named, reusable database query** to a repository without returning an unexecuted IQueryable outside repository execution.

~~~text
Application -> IQuery<TEntity,TResult> ------> Repository.GetAsync(query)
               Apply(IQueryable<TEntity>)         |
               returns IQueryable<TResult>        +-- context.Set<TEntity>()
                                                   +-- query.Apply(source)
                                                   +-- ToListAsync()
                                                   +-- returns List<TResult>

Application -> IQueryExecutor<TEntity,TResult> -> Repository.GetAsync(executor)
               ApplyAsync(source, ct)                  |
               returns Task<TResult>                  +-- context.Set<TEntity>()
                                                      +-- executor.ApplyAsync(source, ct)
                                                      +-- returns TResult
~~~

IContextQuery and IContextQueryExecutor accept the entire DbContext for cross-entity queries.

A Query Object can define Where, Include, OrderBy, Select, paging and provider-specific flags. It is not identical to Fowler/Evans's domain Specification, which models a reusable criterion.

## Architectural boundary

- Caller never receives an unexecuted IQueryable from IRepository.GetAsync.
- Plain IQuery contracts can be implemented using only IQueryable, but query implementations may reference EF Core.
- IContextQuery and much of IRepository explicitly depend on EF Core.
- No separate LINQ-like builder, evaluator or custom interpreter exists.
- IQueryExecutor can issue multiple SQL commands, perform aggregation or post-processing.
- IRepository Add/Update/Remove change tracked state. They do not automatically call SaveChangesAsync.

## Trade-offs

**Benefits:** native LINQ, named queries, reuse, projection, arbitrary results, and clear materialization boundaries.

**Costs:** IQueryExecutor is more powerful than a pure descriptor; implementations can accidentally materialize whole tables or execute excess queries. IContextQuery deliberately uses DbContext. SQL and performance still depend on the EF provider.

**Testing:** SQLite integration tests validate SQL translation and query behavior better than mock-only tests. Repeat sensitive queries on the production provider to verify collations, transaction isolation and plans.

See [feature compatibility](feature-compatibility.md) and [migration guide](ardalis-migration.md).