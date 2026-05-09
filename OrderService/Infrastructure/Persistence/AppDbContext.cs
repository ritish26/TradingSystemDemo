using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.ClientId)
                .HasMaxLength(50);
            entity.Property(e => e.InstrumentId)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(OrderStatus.Pending);
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");
            entity.Property(e => e.xmin)
                .HasColumnName("xmin")
                .IsRowVersion();
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Orders_Status");
        });

        // Configure OutboxMessage entity
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.Payload)
                .IsRequired();
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(OrderStatus.Pending);
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_OutboxMessages_Status");
        });
    }
}