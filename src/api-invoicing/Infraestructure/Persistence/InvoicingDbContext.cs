using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistence;

internal sealed class InvoicingDbContext(DbContextOptions<InvoicingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<InboxDeadLetter> InboxDeadLetters => Set<InboxDeadLetter>();
    public DbSet<OutboxDeadLetter> OutboxDeadLetters => Set<OutboxDeadLetter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<long>("InvoiceNumberSequence")
                .StartsAt(1)
                .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicingDbContext).Assembly);
    }
}
