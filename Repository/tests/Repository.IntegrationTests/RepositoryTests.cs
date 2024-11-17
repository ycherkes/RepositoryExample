using Repository.IntegrationTests.Infrastructure;
using Repository.IntegrationTests.Model;
using Repository.IntegrationTests.ViewModel;

namespace Repository.IntegrationTests
{
    public class RepositoryTests(ProductFilterFixture fixture) : IClassFixture<ProductFilterFixture>
    {
        private readonly ProductRepository _repository = new ProductRepository(fixture.Context);

        [Fact]
        public void SpecificationWithRepositoryTest()
        {
            // Arrange
            ICollectionQuerySpecification<Product> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesOrderedByPrice();

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
        public void SpecificationWithRepositoryProjectedTest()
        {
            // Arrange
            ICollectionQuerySpecification<Product, ProductProjection> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPrice();

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
        public void SpecificationWithRepositoryProjectedResultTest()
        {
            // this is the example for something with pagination like devexpress LoadResult
            // Arrange
            IResultQuerySpecification<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceResult();

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
        public void BananasOrApplesProjectedOrderedByPriceFirstOrDefaultResultTest()
        {
            // Arrange
            IResultQuerySpecification<Product, ProductProjection?> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceFirstOrDefault();

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

    public class BananasOrApplesOrderedByPrice : ICollectionQuerySpecification<Product>
    {
        public IQueryable<Product> Apply(IQueryable<Product> query)
        {
            return query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
                .OrderBy(p => p.Price);
        }
    }

    public class BananasOrApplesProjectedOrderedByPrice : ICollectionQuerySpecification<Product, ProductProjection>
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

    public interface ICollectionQuerySpecification<T> : ICollectionQuerySpecification<T, T>;

    public interface ICollectionQuerySpecification<in TSource, out TDest>
    {
        IQueryable<TDest> Apply(IQueryable<TSource> query);
    }

    public interface IResultQuerySpecification<in TSource, out TDest>
    {
        TDest Apply(IQueryable<TSource> query);
    }


    public class ProductRepository(TestDbContext context)
    {
        //public List<Product> GetList(IQuerySpecification<Product> specification)
        //{
        //    return specification.Apply(context.Products).ToList();
        //}

        public List<TProjection> GetList<TProjection>(ICollectionQuerySpecification<Product, TProjection> specification)
        {
            return specification.Apply(context.Products).ToList();
        }

        public TResult Get<TResult>(IResultQuerySpecification<Product, TResult> specification)
        {
            return specification.Apply(context.Products);
        }
    }
