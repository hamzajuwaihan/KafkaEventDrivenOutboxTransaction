
using Microsoft.EntityFrameworkCore;
using OrdersSystem.Domain.Entities;

namespace OrdersSystem.Infra.DB;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OutboxMessage> OutboxMessages{ get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>();

        base.OnModelCreating(modelBuilder);
    }

}
