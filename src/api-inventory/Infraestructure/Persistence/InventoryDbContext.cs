using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistence;

internal sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<InboxDeadLetter> InboxDeadLetters => Set<InboxDeadLetter>();
    public DbSet<OutboxDeadLetter> OutboxDeadLetters => Set<OutboxDeadLetter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
