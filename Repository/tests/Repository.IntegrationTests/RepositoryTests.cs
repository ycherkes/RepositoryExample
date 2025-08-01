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
            IQuery<Product> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesOrderedByPrice();

            // Act
            var bananasOrApples = await _repository.GetAsync(bananasOrApplesOrderedByPriceSpec, CancellationToken.None);

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
            IQuery<Product, ProductProjection> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPrice();

            // Act
            var bananasOrApples = await _repository.GetAsync(bananasOrApplesOrderedByPriceSpec, CancellationToken.None);

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
            IQueryExecutor<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceResult();

            // Act
            var bananasOrApples = await _repository.GetAsync(bananasOrApplesOrderedByPriceSpec, CancellationToken.None);

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
            var bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceFirstOrDefault();

            // Act
            var bananaOrAppleFirst = await _repository.GetAsync(bananasOrApplesOrderedByPriceSpec, CancellationToken.None);

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

public class BananasOrApplesOrderedByPrice : IQuery<Product>
{
    public IQueryable<Product> Invoke(IQueryable<Product> queryable)
    {
        return queryable.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price);
    }
}

public class BananasOrApplesProjectedOrderedByPrice : IQuery<Product, ProductProjection>
{
    public IQueryable<ProductProjection> Invoke(IQueryable<Product> queryable)
    {
        return queryable.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            });
    }
}

public class BananasOrApplesProjectedOrderedByPriceResult : IQueryExecutor<Product, List<ProductProjection>>
{
    public async Task<List<ProductProjection>> InvokeAsync(IQueryable<Product> queryable, CancellationToken cancellationToken)
    {
        return await queryable.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
            .OrderBy(p => p.Price)
            .Select(x => new ProductProjection
            {
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name
            }).ToListAsync(cancellationToken: cancellationToken);
    }
}

public class BananasOrApplesProjectedOrderedByPriceFirstOrDefault : IQueryExecutor<Product, ProductProjection?>
{
    public async Task<ProductProjection?> InvokeAsync(IQueryable<Product> queryable, CancellationToken cancellationToken)
    {
        return await queryable.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
        .OrderBy(p => p.Price)
        .Select(x => new ProductProjection
        {
            Name = x.Name,
            Price = x.Price,
            CategoryName = x.Category.Name
        }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}


