using Bogus;
using Domain.Entities;

namespace Domain.Tests.Fakers;

public static class CategoryFaker
{
    public static Faker<Category> CreateFaker(string? name = null)
    {
        return new Faker<Category>()
            .CustomInstantiator(f =>
            {
                var categoryName = name ?? f.Commerce.Categories(1)[0] + " " + f.Random.AlphaNumeric(4);
                var result = Category.Create(categoryName);
                return result.Value!;
            });
    }

    public static Category GenerateValid() => CreateFaker().Generate();

    public static List<Category> GenerateValidList(int count = 3) => CreateFaker().Generate(count);
}
