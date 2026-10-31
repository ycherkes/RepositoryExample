using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Repository.IntegrationTests.Infrastructure;
using Repository.IntegrationTests.Model;
using Repository.IntegrationTests.ViewModel;

namespace Repository.IntegrationTests;

/// <summary>
/// Behavioral parity samples for Ardalis.Specification 9.3.1 and the existing IQuery model.
/// Both sides execute against SQLite via the same EF Core provider; no mocks or EF InMemory provider.
/// These are migration examples, not an assertion that the two libraries have identical APIs.
/// </summary>
public sealed class ArdalisMigrationParityTests
{
    [Fact]
    public async Task Filtering_sorting_and_projection_return_identical_results_and_query_shapes()
    {
        using var database = new DatabaseScope();

        var ardalisQuery = SpecificationEvaluator.Default.GetQuery(
            database.Context.Set<Product>(), new ProductProjectionSpec(minPrice: 9f));
        var queryObject = new ProductProjectionQuery(minPrice: 9f);
        var nativeQuery = queryObject.Apply(database.Context.Set<Product>());

        // Inspect actual translated SQL shape rather than comparing provider-generated aliases.
        foreach (var sql in new[] { ardalisQuery.ToQueryString(), nativeQuery.ToQueryString() })
        {
            Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        }

        var expected = await ardalisQuery.ToListAsync();
        var actual = await database.Repository.GetAsync(queryObject);

        Assert.Equal(
            expected.Select(p => (p.Name, p.Price, p.CategoryName)).ToArray(),
            actual.Select(p => (p.Name, p.Price, p.CategoryName)).ToArray());
        Assert.Equal(new[] { "Apple", "Banana" }, actual.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task Paginated_results_and_unpaginated_total_count_match()
    {
        using var database = new DatabaseScope();

        var spec = new ProductPageSpec(skip: 1, take: 1);
        var ardalisItems = await SpecificationEvaluator.Default
            .GetQuery(database.Context.Set<Product>(), spec)
            .Select(p => p.Name)
            .ToListAsync();

        // Ardalis CountAsync(spec) evaluates criteria only; it must not count a paged query.
        var ardalisCount = await SpecificationEvaluator.Default
            .GetQuery(database.Context.Set<Product>(), spec, evaluateCriteriaOnly: true)
            .CountAsync();

        var actual = await database.Repository.GetAsync(
            new ProductPageQuery(skip: 1, take: 1));

        Assert.Equal(3, ardalisCount);
        Assert.Equal(ardalisCount, actual.TotalCount);
        Assert.Equal(ardalisItems, actual.Names);
        Assert.Equal(new[] { "Apple" }, actual.Names);
    }

    [Fact]
    public async Task Include_and_no_tracking_have_the_same_observable_behavior()
    {
        using var database = new DatabaseScope();

        var ardalisItems = await SpecificationEvaluator.Default
            .GetQuery(database.Context.Set<Product>(), new IncludedProductSpec())
            .ToListAsync();

        Assert.Empty(database.Context.ChangeTracker.Entries());

        var queryItems = await database.Repository.GetAsync(new IncludedProductQuery());
        Assert.Empty(database.Context.ChangeTracker.Entries());

        Assert.Single(ardalisItems);
        Assert.Single(queryItems);
        Assert.Equal(ardalisItems[0].Category.Name, queryItems[0].Category.Name);
        Assert.Equal("Fruit", queryItems[0].Category.Name);
    }

    [Fact]
    public async Task First_or_default_and_any_can_use_a_materializing_query_executor()
    {
        using var database = new DatabaseScope();

        var spec = new ProductsAbovePriceSpec(9f);
        var expectedFirst = await SpecificationEvaluator.Default
            .GetQuery(database.Context.Set<Product>(), spec)
            .FirstOrDefaultAsync();
        var actualFirst = await database.Repository.GetAsync(new FirstProductAbovePriceQuery(9f));

        var expectedAny = await SpecificationEvaluator.Default
            .GetQuery(database.Context.Set<Product>(), new ProductsAbovePriceSpec(100f))
            .AnyAsync();
        var actualAny = await database.Repository.GetAsync(new AnyProductAbovePriceQuery(100f));

        Assert.Equal(expectedFirst?.Id, actualFirst?.Id);
        Assert.Equal(expectedAny, actualAny);
        Assert.False(actualAny);
    }

    [Fact]
    public async Task Query_object_can_execute_a_cross_entity_join()
    {
        using var database = new DatabaseScope();

        // IContextQuery operates on the full DbContext when no single entity root fits.
        var results = await database.Repository.GetAsync(new ProductsWithCategoryJoinQuery());

        Assert.Equal(new[] { "Apple", "Banana", "Cherry" },
            results.Select(x => x.Name).ToArray());
        Assert.All(results, result => Assert.Equal("Fruit", result.CategoryName));
    }

    [Fact]
    public async Task Bulk_update_produces_the_same_database_state()
    {
        using var ardalisDatabase = new DatabaseScope();
        using var queryDatabase = new DatabaseScope();

        var spec = new ProductsAbovePriceSpec(9f);
        var affectedByArdalis = await SpecificationEvaluator.Default
            .GetQuery(ardalisDatabase.Context.Set<Product>(), spec)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Price, p => p.Price + 1f));

        var affectedByQuery = await queryDatabase.Repository.ExecuteUpdateAsync(
            new ProductsAbovePriceQuery(9f),
            setters => setters.SetProperty(p => p.Price, p => p.Price + 1f));

        var expected = await ardalisDatabase.Context.Products.AsNoTracking()
            .OrderBy(p => p.Id).Select(p => p.Price).ToListAsync();
        var actual = await queryDatabase.Context.Products.AsNoTracking()
            .OrderBy(p => p.Id).Select(p => p.Price).ToListAsync();

        Assert.Equal(2, affectedByArdalis);
        Assert.Equal(affectedByArdalis, affectedByQuery);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Bulk_delete_produces_the_same_database_state()
    {
        using var ardalisDatabase = new DatabaseScope();
        using var queryDatabase = new DatabaseScope();

        var spec = new ProductsAbovePriceSpec(14f);
        var affectedByArdalis = await SpecificationEvaluator.Default
            .GetQuery(ardalisDatabase.Context.Set<Product>(), spec)
            .ExecuteDeleteAsync();

        var affectedByQuery = await queryDatabase.Repository.ExecuteDeleteAsync(
            new ProductsAbovePriceQuery(14f));

        var expected = await ardalisDatabase.Context.Products.AsNoTracking()
            .OrderBy(p => p.Id).Select(p => p.Name).ToListAsync();
        var actual = await queryDatabase.Context.Products.AsNoTracking()
            .OrderBy(p => p.Id).Select(p => p.Name).ToListAsync();

        Assert.Equal(1, affectedByArdalis);
        Assert.Equal(affectedByArdalis, affectedByQuery);
        Assert.Equal(expected, actual);
    }

    private sealed class ProductProjectionSpec : Specification<Product, ProductProjection>
    {
        public ProductProjectionSpec(float minPrice)
        {
            Query.Where(p => p.Price >= minPrice)
                .OrderBy(p => p.Price)
                .Select(p => new ProductProjection
                {
                    Name = p.Name,
                    Price = p.Price,
                    CategoryName = p.Category.Name
                });
        }
    }

    private sealed class ProductProjectionQuery(float minPrice)
        : IQuery<Product, ProductProjection>
    {
        public IQueryable<ProductProjection> Apply(IQueryable<Product> products) =>
            products.Where(p => p.Price >= minPrice)
                .OrderBy(p => p.Price)
                .Select(p => new ProductProjection
                {
                    Name = p.Name,
                    Price = p.Price,
                    CategoryName = p.Category.Name
                });
    }

    private sealed class ProductPageSpec : Specification<Product>
    {
        public ProductPageSpec(int skip, int take)
        {
            Query.Where(p => p.Category.Name == "Fruit")
                .OrderBy(p => p.Price)
                .Skip(skip)
                .Take(take);
        }
    }

    private sealed record ProductPage(IReadOnlyList<string> Names, int TotalCount);

    private sealed class ProductPageQuery(int skip, int take)
        : IQueryExecutor<Product, ProductPage>
    {
        public async Task<ProductPage> ApplyAsync(
            IQueryable<Product> products, CancellationToken cancellationToken)
        {
            var filtered = products.Where(p => p.Category.Name == "Fruit");
            var total = await filtered.CountAsync(cancellationToken);
            var names = await filtered.OrderBy(p => p.Price)
                .Skip(skip).Take(take).Select(p => p.Name)
                .ToListAsync(cancellationToken);
            return new ProductPage(names, total);
        }
    }

    private sealed class IncludedProductSpec : Specification<Product>
    {
        public IncludedProductSpec()
        {
            Query.Where(p => p.Name == "Apple")
                .Include(p => p.Category)
                .AsNoTracking();
        }
    }

    private sealed class IncludedProductQuery : IQuery<Product>
    {
        public IQueryable<Product> Apply(IQueryable<Product> products) =>
            products.Where(p => p.Name == "Apple")
                .Include(p => p.Category)
                .AsNoTracking();
    }

    private sealed class ProductsAbovePriceSpec : Specification<Product>
    {
        public ProductsAbovePriceSpec(float minimumPrice) =>
            Query.Where(p => p.Price > minimumPrice).OrderBy(p => p.Price);
    }

    private sealed class ProductsAbovePriceQuery(float minimumPrice) : IQuery<Product>
    {
        public IQueryable<Product> Apply(IQueryable<Product> products) =>
            products.Where(p => p.Price > minimumPrice).OrderBy(p => p.Price);
    }

    private sealed class FirstProductAbovePriceQuery(float minimumPrice)
        : IQueryExecutor<Product, Product?>
    {
        public Task<Product?> ApplyAsync(
            IQueryable<Product> products, CancellationToken cancellationToken) =>
            products.Where(p => p.Price > minimumPrice)
                .OrderBy(p => p.Price).FirstOrDefaultAsync(cancellationToken);
    }

    private sealed class AnyProductAbovePriceQuery(float minimumPrice)
        : IQueryExecutor<Product, bool>
    {
        public Task<bool> ApplyAsync(
            IQueryable<Product> products, CancellationToken cancellationToken) =>
            products.AnyAsync(p => p.Price > minimumPrice, cancellationToken);
    }

    private sealed class ProductsWithCategoryJoinQuery : IContextQuery<ProductProjection>
    {
        public IQueryable<ProductProjection> Apply(DbContext context) =>
            from product in context.Set<Product>()
            join category in context.Set<Category>() on product.CategoryId equals category.Id
            orderby product.Name
            select new ProductProjection
            {
                Name = product.Name,
                Price = product.Price,
                CategoryName = category.Name
            };
    }

    private sealed class DatabaseScope : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public TestDbContext Context { get; }
        public IRepository Repository { get; }

        public DatabaseScope()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;
            Context = new TestDbContext(options);
            Context.Database.EnsureCreated();
            Repository = new Repository(Context);
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
