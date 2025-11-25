using Microsoft.EntityFrameworkCore;
using ConfectioneryApi.Models;

namespace ConfectioneryApi.Data
{
    public class ConfectioneryDbContext : DbContext
    {
        public ConfectioneryDbContext(DbContextOptions<ConfectioneryDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Pastry> Pastries { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфігурація для зв'язків
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order) 
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1. Гарантуємо, що номер телефону клієнта є унікальним
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            // 2. Гарантуємо, що назва кондитерського виробу є унікальною
            modelBuilder.Entity<Pastry>()
                .HasIndex(p => p.Name)
                .IsUnique();
        }
    }
}