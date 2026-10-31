# Running the Ardalis migration parity suite

Install **.NET SDK 10** and run, from the repository root:

~~~shell
dotnet restore Repository/Examples.slnx
dotnet test Repository/Examples.slnx --configuration Release --no-restore
~~~

The GitHub Actions workflow runs the same commands for pull requests and main.

## What tests prove

ArdalisMigrationParityTests uses:

- Ardalis.Specification.EntityFrameworkCore 9.3.1 (only in the test project)
- The existing IQuery/IQueryExecutor/IContextQuery and IRepository contracts
- EF Core SQLite with an open, in-memory SQLite connection
- The existing TestDbContext seed dataset on both sides

Bulk mutations use **two separately seeded databases**, so results don't depend on test execution order. SQL-shape checks assert key translated clauses, *not* identical aliases or parameter names.

## Explicit limitations

Passing the suite demonstrates **scenario parity**, not exact SQL, performance or universal EF provider parity. Additional testing is needed for Search and LIKE escaping, null/collation differences, global filters, identity resolution, streaming, caching, provider-specific translation, and transaction semantics.

The RepositoryExample CRUD operations do not save automatically. Unlike Ardalis RepositoryBase<T>, they require an explicit SaveChangesAsync.

## Adding a new parity case

1. Reproduce one documented Ardalis scenario, pinned to version 9.3.1.
2. Express the same scenario using regular LINQ inside IQuery or IQueryExecutor.
3. Execute both over identically seeded databases.
4. Assert result values/order, exceptional behavior and tracking state, where applicable.
5. For important SQL, inspect ToQueryString and verify plans on your production provider.
6. Update [feature compatibility](feature-compatibility.md) after CI is green.
