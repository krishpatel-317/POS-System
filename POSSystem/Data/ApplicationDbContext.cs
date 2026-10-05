using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using POSSystem.Models;
using System.Reflection.Emit;

namespace POSSystem.Data
{
    // IdentityDbContext adds all the ASP.NET Identity tables automatically
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Domain tables
        public DbSet<Category> Categories { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Sale> Sales { get; set; }

        public DbSet<SaleItem> SaleItems { get; set; }

        public DbSet<Payment> Payments { get; set; }


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // Category 1 -> Many Products

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);


            // Customer 1 -> Many Sales

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId);


            // User 1 -> Many Sales (AspNetUsers IdentityUser)

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId);


            // Sale 1 -> Many SaleItems

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId);


            // Product 1 -> Many SaleItems

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Product)
                .WithMany(p => p.SaleItems)
                .HasForeignKey(si => si.ProductId);


            // Sale 1 -> 1 Payment

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Sale)
                .WithOne(s => s.Payment)
                .HasForeignKey<Payment>(p => p.SaleId);

            // Precision for all currency decimals
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SaleItem>().Property(si => si.TotalPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(18,2)");
        }
    }
}
