using Microsoft.EntityFrameworkCore;
using QuerySpecification.IntegrationTests.Infrastructure;
using QuerySpecification.IntegrationTests.Model;
using QuerySpecification.IntegrationTests.ViewModel;

namespace QuerySpecification.IntegrationTests;

public class QuerySpecificationTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
{
    [Fact]
    public void QuerySpecification()
    {
        // Arrange
        IQuerySpecification<Product> bananasOrApplesOrderedByPrice = new BananasOrApplesOrderedByPrice();

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
    public void QuerySpecificationProjected()
    {
        // Arrange
        IQuerySpecification<Product, ProductProjection> bananasOrApplesOrderedByPrice =
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
    public void FinalQuerySpecificationWithProjectedResult()
    {
        // this is the example for something with pagination like devexpress LoadResult
        // Arrange
        IFinalQuerySpecification<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceFinalQuery =
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
    public void FinalQuerySpecificationWithProjectedResultExtensionMethod()
    {
        // this is the example for something with pagination like devexpress LoadResult
        // Arrange
        IFinalQuerySpecification<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceFinalQuery =
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
    public void BananasOrApplesProjectedOrderedByPriceFirstOrDefaultFinal()
    {
        // Arrange
        IFinalQuerySpecification<Product, ProductProjection?> bananasOrApplesOrderedByPriceFinalQuery =
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
    public async Task BananasOrApplesProjectedOrderedByPriceFirstOrDefaultFinalAsync()
    {
        // Arrange
        IFinalQuerySpecificationAsync<Product, ProductProjection?> bananasOrApplesOrderedByPriceFinalQuery =
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

public class BananasOrApplesOrderedByPrice : IQuerySpecification<Product>
{
    public IQueryable<Product> Apply(IQueryable<Product> query)
    {
        return query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price);
    }
}

public class BananasOrApplesProjectedOrderedByPrice : IQuerySpecification<Product, ProductProjection>
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

public class BananasOrApplesProjectedOrderedByPriceResult : IFinalQuerySpecification<Product, List<ProductProjection>>
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

public class BananasOrApplesProjectedOrderedByPriceFirstOrDefault : IFinalQuerySpecification<Product, ProductProjection?>
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

public class BananasOrApplesProjectedOrderedByPriceFirstOrDefaultAsync : IFinalQuerySpecificationAsync<Product, ProductProjection?>
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

public interface IQuerySpecification<T> : IQuerySpecification<T, T>;

public interface IQuerySpecification<in TSource, out TDest>
{
    IQueryable<TDest> Apply(IQueryable<TSource> query);
}

public interface IFinalQuerySpecification<in TSource, out TDest>
{
    TDest Apply(IQueryable<TSource> query);
}

public interface IFinalQuerySpecificationAsync<in TSource, TDest>
{
    Task<TDest> ApplyAsync(IQueryable<TSource> query, CancellationToken cancellationToken = default);
}

public static class QuerySpecificationExtensions
{
    public static IQueryable<T> ApplyQuery<T>(this IQueryable<T> source, IQuerySpecification<T> query)
    {
        return query.Apply(source);
    }
    public static IQueryable<TDest> ApplyQuery<TSource, TDest>(this IQueryable<TSource> source, IQuerySpecification<TSource, TDest> query)
    {
        return query.Apply(source);
    }
    public static TDest ApplyQuery<TSource, TDest>(this IQueryable<TSource> source, IFinalQuerySpecification<TSource, TDest> query)
    {
        return query.Apply(source);
    }
}