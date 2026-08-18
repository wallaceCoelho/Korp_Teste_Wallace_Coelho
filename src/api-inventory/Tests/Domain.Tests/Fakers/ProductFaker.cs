using Bogus;
using Domain.Entities;

namespace Domain.Tests.Fakers;

public static class ProductFaker
{
    public static Faker<Product> CreateFaker(
        string? code = null,
        string? name = null,
        int? initialStock = null,
        decimal? unitPrice = null,
        string? description = null,
        int? minStock = null)
    {
        return new Faker<Product>()
            .CustomInstantiator(f =>
            {
                var prodCode = code ?? f.Commerce.Ean8();
                var prodName = name ?? f.Commerce.ProductName();
                var prodDesc = description ?? f.Commerce.ProductDescription();
                var stock = initialStock ?? f.Random.Number(10, 100);
                var price = unitPrice ?? Math.Round(f.Random.Decimal(10m, 1000m), 2);
                var min = minStock ?? f.Random.Number(1, 10);

                var result = Product.Create(prodCode, prodName, stock, price, prodDesc, min);
                return result.Value!;
            });
    }

    public static Product GenerateValid() => CreateFaker().Generate();

    public static List<Product> GenerateValidList(int count = 3) => CreateFaker().Generate(count);
}
