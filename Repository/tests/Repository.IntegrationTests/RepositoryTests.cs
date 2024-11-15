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
            ISpecification<Product> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesOrderedByPrice();

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
            ISpecification<Product, ProductProjection> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPrice();

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
        public void SpecificationWithRepositoryProjectedSealedTest()
        {
            // this is the example for something with pagination like devexpress LoadResult
            // Arrange
            ISealedSpecification<Product, List<ProductProjection>> bananasOrApplesOrderedByPriceSpec = new BananasOrApplesProjectedOrderedByPriceSealed();

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
    }

    public class BananasOrApplesOrderedByPrice : ISpecification<Product>
    {
        public IQueryable<Product> Apply(IQueryable<Product> query)
        {
            return query.Where(static x => new[] { "Banana", "Apple" }.Contains(x.Name))
                .OrderBy(p => p.Price);
        }
    }

    public class BananasOrApplesProjectedOrderedByPrice : ISpecification<Product, ProductProjection>
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

    public class BananasOrApplesProjectedOrderedByPriceSealed : ISealedSpecification<Product, List<ProductProjection>>
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

    public interface ISpecification<T> : ISpecification<T, T>;

    public interface ISpecification<in TSource, out TDest>
    {
        IQueryable<TDest> Apply(IQueryable<TSource> query);
    }

    public interface ISealedSpecification<in TSource, out TDest>
    {
        TDest Apply(IQueryable<TSource> query);
    }


    public class ProductRepository(TestDbContext context)
    {
        //public List<Product> GetList(ISpecification<Product> specification)
        //{
        //    return specification.Apply(context.Products).ToList();
        //}

        public List<TProjection> GetList<TProjection>(ISpecification<Product, TProjection> specification)
        {
            return specification.Apply(context.Products).ToList();
        }

        public TResult Get<TResult>(ISealedSpecification<Product, TResult> specification)
        {
            return specification.Apply(context.Products);
        }
    }
}
