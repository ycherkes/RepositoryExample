using Microsoft.EntityFrameworkCore;
using QueryHolder.IntegrationTests.Infrastructure;
using QueryHolder.IntegrationTests.Model;
using QueryHolder.IntegrationTests.ViewModel;

namespace QueryHolder.IntegrationTests;

public class QueryHolderTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
{
    [Fact]
    public void Query()
    {
        // Arrange
        IQueryHolder<Product> bananasOrApplesOrderedByPrice = new BananasOrApplesOrderedByPrice();

        // Act
        var bananasOrApples = bananasOrApplesOrderedByPrice.Apply(fixture.Context.Products).ToList();

        // Assert
        Assert.Equivalent(new[]
        {
            new Product
            {
                Id = 1,
                Name = "Apple",
                Price = 10F,
                CategoryId = 1
            },
            new Product
            {
                Id = 2,
                Name = "Banana",
                Price = 15F,
                CategoryId = 1
            }
        }, bananasOrApples);
    }

    [Fact]
    public void QueryProjected()
    {
        // Arrange
        IQueryHolder<Product, ProductProjection> bananasOrApplesOrderedByPrice =
            new BananasOrApplesProjectedOrderedByPrice();

        // Act
        var bananasOrApples = bananasOrApplesOrderedByPrice.Apply(fixture.Context.Products).ToList();

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                Name = "Apple",
                Price = 10F,
                CategoryName = "Fruit"
            },
            new
            {
                Name = "Banana",
                Price = 15F,
                CategoryName = "Fruit"
            }
        }, bananasOrApples);
    }

    [Fact]
    public void FinalQueryWithProjectedResult()
    {
        // this is the example for something with pagination like devexpress LoadResult
        // Arrange
        IFinalQueryHolder<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceFinalQuery =
            new BananasOrApplesProjectedOrderedByPriceResult();

        // Act
        var bananasOrApples = bananasOrApplesOrderedByPriceFinalQuery.Apply(fixture.Context.Products);

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                Name = "Apple",
                Price = 10F,
                CategoryName = "Fruit"
            },
            new
            {
                Name = "Banana",
                Price = 15F,
                CategoryName = "Fruit"
            }
        }, bananasOrApples);
    }

    [Fact]
    public void FinalQueryWithProjectedResultExtensionMethod()
    {
        // this is the example for something with pagination like devexpress LoadResult
        // Arrange
        IFinalQueryHolder<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceFinalQuery =
            new BananasOrApplesProjectedOrderedByPriceResult();

        // Act
        var bananasOrApples = fixture.Context.Products.ApplyQuery(bananasOrApplesOrderedByPriceFinalQuery);

        // Assert
        Assert.Equivalent(new[]
        {
            new
            {
                Name = "Apple",
                Price = 10F,
                CategoryName = "Fruit"
            },
            new
            {
                Name = "Banana",
                Price = 15F,
                CategoryName = "Fruit"
            }
        }, bananasOrApples);
    }

    [Fact]
    public void FinalBananasOrApplesProjectedOrderedByPriceFirstOrDefault()
    {
        // Arrange
        IFinalQueryHolder<Product, ProductProjection?> bananasOrApplesOrderedByPriceFinalQuery =
            new BananasOrApplesProjectedOrderedByPriceFirstOrDefault();

        // Act
        var bananaOrAppleFirst = bananasOrApplesOrderedByPriceFinalQuery.Apply(fixture.Context.Products);

        // Assert
        Assert.Equivalent(new
        {
            Name = "Apple",
            Price = 10F,
            CategoryName = "Fruit"
        }, bananaOrAppleFirst);
    }

    [Fact]
    public async Task FinalAsyncBananasOrApplesProjectedOrderedByPriceFirstOrDefault()
    {
        // Arrange
        IAsyncFinalQueryHolder<Product, ProductProjection?> bananasOrApplesOrderedByPriceFinalQuery =
            new BananasOrApplesProjectedOrderedByPriceFirstOrDefaultAsync();

        // Act
        var bananaOrAppleFirst = await bananasOrApplesOrderedByPriceFinalQuery.ApplyAsync(fixture.Context.Products);

        // Assert
        Assert.Equivalent(new
        {
            Name = "Apple",
            Price = 10F,
            CategoryName = "Fruit"
        }, bananaOrAppleFirst);
    }
}

public class BananasOrApplesOrderedByPrice : IQueryHolder<Product>
{
    public IQueryable<Product> Apply(IQueryable<Product> query)
    {
        return query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price);
    }
}

public class BananasOrApplesProjectedOrderedByPrice : IQueryHolder<Product, ProductProjection>
{
    public IQueryable<ProductProjection> Apply(IQueryable<Product> query)
    {
        return query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            });
    }
}

public class BananasOrApplesProjectedOrderedByPriceResult : IFinalQueryHolder<Product, List<ProductProjection>>
{
    public List<ProductProjection> Apply(IQueryable<Product> query)
    {
        return query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            }).ToList();
    }
}

public class BananasOrApplesProjectedOrderedByPriceFirstOrDefault : IFinalQueryHolder<Product, ProductProjection?>
{
    public ProductProjection? Apply(IQueryable<Product> query)
    {
        return query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            }).FirstOrDefault();
    }
}

public class BananasOrApplesProjectedOrderedByPriceFirstOrDefaultAsync : IAsyncFinalQueryHolder<Product, ProductProjection?>
{
    public async Task<ProductProjection?> ApplyAsync(IQueryable<Product> query, CancellationToken cancellationToken = default)
    {
        return await query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}

public interface IQueryHolder<T> : IQueryHolder<T, T>;

public interface IQueryHolder<in TSource, out TDest>
{
    IQueryable<TDest> Apply(IQueryable<TSource> query);
}

public interface IFinalQueryHolder<in TSource, out TDest>
{
    TDest Apply(IQueryable<TSource> query);
}

public interface IAsyncFinalQueryHolder<in TSource, TDest>
{
    Task<TDest> ApplyAsync(IQueryable<TSource> query, CancellationToken cancellationToken = default);
}

public static class QueryHolderExtensions
{
    public static IQueryable<T> ApplyQuery<T>(this IQueryable<T> source, IQueryHolder<T> query)
    {
        return query.Apply(source);
    }
    public static IQueryable<TDest> ApplyQuery<TSource, TDest>(this IQueryable<TSource> source, IQueryHolder<TSource, TDest> query)
    {
        return query.Apply(source);
    }
    public static TDest ApplyQuery<TSource, TDest>(this IQueryable<TSource> source, IFinalQueryHolder<TSource, TDest> query)
    {
        return query.Apply(source);
    }
    public static Task<TDest> ApplyQueryAsync<TSource, TDest>(this IQueryable<TSource> source, IAsyncFinalQueryHolder<TSource, TDest> query, CancellationToken cancellationToken = default)
    {
        return query.ApplyAsync(source, cancellationToken);
    }
}