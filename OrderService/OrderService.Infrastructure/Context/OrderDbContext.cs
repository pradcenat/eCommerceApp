using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entity;

namespace OrderService.Infrastructure.Context
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.UserId).IsRequired();
                entity.Property(o => o.UserEmail).IsRequired().HasMaxLength(100);
                entity.Property(o => o.Status).IsRequired()
                    .HasConversion<string>();
                entity.Property(o => o.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                entity.Property(o => o.ShippingAddress)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.HasIndex(o => o.UserId);
                entity.HasIndex(o => o.Status);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.ProductId).IsRequired();
                entity.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
                entity.Property(i => i.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");
                entity.Property(i => i.Quantity).IsRequired();
                entity.Ignore(i => i.TotalPrice);

                entity.HasOne(i => i.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
