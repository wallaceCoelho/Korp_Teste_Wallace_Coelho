using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Persistence;

public static class InventoryDataSeeder
{
    public static async Task SeedInventoryDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<InventoryDbContext>>();

        try
        {
            Console.WriteLine("[InventoryDataSeeder] Verificando semente de dados de categorias e produtos...");

            var categoryNames = new[]
            {
                "Eletrônicos",
                "Informática",
                "Periféricos & Acessórios",
                "Smartphones & Telefonia",
                "Áudio & Vídeo",
                "Eletrodomésticos",
                "Móveis & Decoração",
                "Papelaria & Escritório",
                "Ferramentas & Construção",
                "Games & Consoles",
                "Redes & Conectividade",
                "Iluminação & Elétrica",
                "Segurança & CFTV",
                "Wearables & Smartwatches",
                "Cabos & Adaptadores"
            };

            var existingCategories = await context.Categories.ToListAsync();
            var categories = new List<Category>(existingCategories);

            foreach (var catName in categoryNames)
            {
                var existing = categories.FirstOrDefault(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    var catResult = Category.Create(catName);
                    if (catResult.IsSuccess)
                    {
                        var newCat = catResult.Value!;
                        await context.Categories.AddAsync(newCat);
                        categories.Add(newCat);
                        Console.WriteLine($"[InventoryDataSeeder] Categoria criada: {catName}");
                    }
                }
            }

            await context.SaveChangesAsync();

            var existingProductCount = await context.Products.CountAsync();
            Console.WriteLine($"[InventoryDataSeeder] Total de produtos existentes no banco: {existingProductCount}");

            if (existingProductCount >= 100)
            {
                Console.WriteLine("[InventoryDataSeeder] Base já contém 100 ou mais produtos. Seeding finalizado.");
                return;
            }

            Randomizer.Seed = new Random(8675309);
            var faker = new Faker("pt_BR");

            var newProducts = new List<Product>();
            var targetCount = 100;

            for (int i = 1; i <= targetCount; i++)
            {
                var code = $"PRD-{i:D3}";
                var alreadyExists = await context.Products.AnyAsync(p => p.Code == code);
                if (alreadyExists) continue;

                var category = faker.PickRandom(categories);
                var rawName = $"{faker.Commerce.ProductAdjective()} {faker.Commerce.ProductName()}";
                var name = rawName.Length > 140 ? rawName[..140] : rawName;

                var rawDesc = faker.Commerce.ProductDescription();
                var description = rawDesc.Length > 450 ? rawDesc[..450] : rawDesc;

                var initialStock = faker.Random.Number(10, 150);
                var minStock = faker.Random.Number(3, 15);
                var unitPrice = Math.Round(faker.Random.Decimal(19.90m, 3500.00m), 2);

                var productResult = Product.Create(
                    code: code,
                    name: name,
                    initialStock: initialStock,
                    unitPrice: unitPrice,
                    description: description,
                    minStock: minStock
                );

                if (productResult.IsSuccess)
                {
                    var product = productResult.Value!;
                    product.ChangeCategory(category.Id);
                    newProducts.Add(product);
                }
                else
                {
                    Console.WriteLine($"[InventoryDataSeeder] Falha ao criar produto {code}: {productResult.Error}");
                }
            }

            if (newProducts.Count > 0)
            {
                await context.Products.AddRangeAsync(newProducts);
                await context.SaveChangesAsync();
                Console.WriteLine($"[InventoryDataSeeder] Sucesso: {newProducts.Count} produtos inseridos com sucesso!");
            }
            else
            {
                Console.WriteLine("[InventoryDataSeeder] Nenhum novo produto precisou ser inserido.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryDataSeeder] ERRO durante a execução do seeder: {ex.Message}\n{ex.StackTrace}");
            logger?.LogError(ex, "Erro ao executar o Seeder de dados do inventário com Bogus.");
            throw;
        }
    }
}
