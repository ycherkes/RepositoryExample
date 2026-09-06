using Microsoft.EntityFrameworkCore;
using Repository.IntegrationTests;
using Repository.IntegrationTests.Infrastructure;
using Repository.IntegrationTests.Model;
using Repository.IntegrationTests.ViewModel;

namespace Repository.IntegrationTests
{
    public class RepositoryTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
    {
        private readonly IRepository _repository = new Repository(fixture.Context);

        [Fact]
        public async Task QueryWithRepository()
        {
            // Arrange
            IQuery<Product> bananasOrApplesOrderedByPriceQuery = new BananasOrApplesOrderedByPriceQuery();

            // Act
            var bananasOrApples = await _repository.GetAsync(bananasOrApplesOrderedByPriceQuery, CancellationToken.None);

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
        public async Task QueryWithRepositoryProjected()
        {
            // Arrange
            IQuery<Product, ProductProjection> bananasOrApplesOrderedByPriceQuery = new BananasOrApplesProjectedOrderedByPriceQuery();

            // Act
            var bananasOrApples = await _repository.GetAsync(bananasOrApplesOrderedByPriceQuery, CancellationToken.None);

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
        public async Task QueryExecutorWithRepositoryProjectedResult()
        {
            // this is the example for something with pagination like devexpress LoadResult
            // Arrange
            IQueryExecutor<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceQuery = new BananasOrApplesProjectedOrderedByPriceResultQuery();

            // Act
            var bananasOrApples = await _repository.GetAsync(bananasOrApplesOrderedByPriceQuery, CancellationToken.None);

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
        public async Task BananasOrApplesProjectedOrderedByPriceFirstOrDefaultResult()
        {
            // Arrange
            var bananasOrApplesOrderedByPriceQuery = new BananasOrApplesProjectedOrderedByPriceFirstOrDefaultQuery();

            // Act
            var bananaOrAppleFirst = await _repository.GetAsync(bananasOrApplesOrderedByPriceQuery, CancellationToken.None);

            // Assert
            Assert.Equivalent(new
            {
                Name = "Apple",
                Price = 10F,
                CategoryName = "Fruit"
            }, bananaOrAppleFirst);
        }
    }

}

internal sealed class BananasOrApplesOrderedByPriceQuery : IQuery<Product>
{
    public IQueryable<Product> Apply(IQueryable<Product> products)
    {
        return products
            .Where(static x => new List<string> { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price);
    }
}

internal sealed class BananasOrApplesProjectedOrderedByPriceQuery : IQuery<Product, ProductProjection>
{
    public IQueryable<ProductProjection> Apply(IQueryable<Product> products)
    {
        return products
            .Where(static x => new List<string> { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            });
    }
}

internal sealed class BananasOrApplesProjectedOrderedByPriceResultQuery : IQueryExecutor<Product, List<ProductProjection>>
{
    public async Task<List<ProductProjection>> ApplyAsync(IQueryable<Product> products, CancellationToken cancellationToken)
    {
        return await products
            .Where(static x => new List<string> { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            }).ToListAsync(cancellationToken: cancellationToken);
    }
}

internal sealed class BananasOrApplesProjectedOrderedByPriceFirstOrDefaultQuery : IQueryExecutor<Product, ProductProjection?>
{
    public async Task<ProductProjection?> ApplyAsync(IQueryable<Product> products, CancellationToken cancellationToken)
    {
        return await products
            .Where(static x => new List<string> { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}


