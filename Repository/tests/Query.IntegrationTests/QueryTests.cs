using Query.IntegrationTests.Infrastructure;
using Query.IntegrationTests.Model;
using Query.IntegrationTests.ViewModel;

namespace Query.IntegrationTests;

public class QueryTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
{
    [Fact]
    public void SpecificationTest()
    {
        // Arrange
        IQuerySpecification<Product> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesOrderedByPrice();

        // Act
        var bananasOrApples = bananasOrApplesOrderedByPriceSpec.Apply(fixture.Context.Products).ToList();

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
    public void SpecificationProjectedTest()
    {
        // Arrange
        IQuerySpecification<Product, ProductProjection> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPrice();

        // Act
        var bananasOrApples = bananasOrApplesOrderedByPriceSpec.Apply(fixture.Context.Products).ToList();

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
    public void SpecificationWithProjectedResultTest()
    {
        // this is the example for something with pagination like devexpress LoadResult
        // Arrange
        IResultQuerySpecification<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceResult();

        // Act
        var bananasOrApples = bananasOrApplesOrderedByPriceSpec.Apply(fixture.Context.Products);

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
    public void BananasOrApplesProjectedOrderedByPriceFirstOrDefaultResultTest()
    {
        // Arrange
        IResultQuerySpecification<Product, ProductProjection?> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceFirstOrDefault();

        // Act
        var bananaOrAppleFirst = bananasOrApplesOrderedByPriceSpec.Apply(fixture.Context.Products);

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

public class BananasOrApplesProjectedOrderedByPriceResult : IResultQuerySpecification<Product, List<ProductProjection>>
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

public class BananasOrApplesProjectedOrderedByPriceFirstOrDefault : IResultQuerySpecification<Product, ProductProjection?>
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

public interface IQuerySpecification<T> : IQuerySpecification<T, T>;

public interface IQuerySpecification<in TSource, out TDest>
{
    IQueryable<TDest> Apply(IQueryable<TSource> query);
}

public interface IResultQuerySpecification<in TSource, out TDest>
{
    TDest Apply(IQueryable<TSource> query);
}