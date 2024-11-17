namespace Query.IntegrationTests.Model;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public float Price { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Product> Products { get; set; }
}
