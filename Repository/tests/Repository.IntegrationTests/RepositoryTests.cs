using Repository.IntegrationTests.Infrastructure;
using Repository.IntegrationTests.Model;
using Repository.IntegrationTests.ViewModel;

namespace Repository.IntegrationTests
{
    public class RepositoryTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
    {
        private readonly ProductRepository _repository = new ProductRepository(fixture.Context);

        [Fact]
        public void SpecificationWithRepository()
        {
            // Arrange
            IQuerySpecification<Product> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesOrderedByPrice();

            // Act
            var bananasOrApples = _repository.GetList(bananasOrApplesOrderedByPriceSpec);

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
        public void SpecificationWithRepositoryProjected()
        {
            // Arrange
            IQuerySpecification<Product, ProductProjection> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPrice();

            // Act
            var bananasOrApples = _repository.GetList(bananasOrApplesOrderedByPriceSpec);

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
        public void SpecificationWithRepositoryProjectedResult()
        {
            // this is the example for something with pagination like devexpress LoadResult
            // Arrange
            IFinalQuerySpecification<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceResult();

            // Act
            var bananasOrApples = _repository.Get(bananasOrApplesOrderedByPriceSpec);

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
        public void BananasOrApplesProjectedOrderedByPriceFirstOrDefaultResult()
        {
            // Arrange
            IFinalQuerySpecification<Product, ProductProjection?> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceFirstOrDefault();

            // Act
            var bananaOrAppleFirst = _repository.Get(bananasOrApplesOrderedByPriceSpec);

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

    public interface IQuerySpecification<T> : IQuerySpecification<T, T>;

    public interface IQuerySpecification<in TSource, out TDest>
    {
        IQueryable<TDest> Apply(IQueryable<TSource> query);
    }

    public interface IFinalQuerySpecification<in TSource, out TDest>
    {
        TDest Apply(IQueryable<TSource> query);
    }


    public class ProductRepository(TestDbContext context)
    {
        //public List<Product> GetList(IQuerySpecification<Product> specification)
        //{
        //    return specification.Apply(context.Products).ToList();
        //}

        public List<TProjection> GetList<TProjection>(IQuerySpecification<Product, TProjection> specification)
        {
            return specification.Apply(context.Products).ToList();
        }

        public TResult Get<TResult>(IFinalQuerySpecification<Product, TResult> specification)
        {
            return specification.Apply(context.Products);
        }
    }
