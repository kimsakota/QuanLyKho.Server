using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKho.API.Models
{
    public class AppDbContext : DbContext

    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Import> Imports => Set<Import>();
        public DbSet<ImportDetail> ImportDetails => Set<ImportDetail>();
        public DbSet<Export> Exports => Set<Export>();
        public DbSet<ExportDetail> ExportDetails => Set<ExportDetail>();
        public DbSet<InventoryCheck> InventoryChecks => Set<InventoryCheck>();
        public DbSet<InventoryCheckDetail> InventoryCheckDetails => Set<InventoryCheckDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. BỎ QUA CÁC THUỘC TÍNH TÍNH TOÁN (QUAN TRỌNG ĐỂ SỬA LỖI "No backing field")
            modelBuilder.Entity<ExportDetail>().Ignore(e => e.TotalPrice);
            modelBuilder.Entity<ImportDetail>().Ignore(i => i.TotalPrice);
            modelBuilder.Entity<InventoryCheckDetail>().Ignore(c => c.Diff);

            // 2. Cấu hình các Index và Relationship
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductCode)
                .IsUnique(false);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique(true);

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.TaxCode)
                .IsUnique();

            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Imports)
                .WithOne(i => i.Supplier)
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Exports)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Import>()
                .HasMany(i => i.ImportDetails)
                .WithOne(d => d.Import)
                .HasForeignKey(d => d.ImportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Export>()
                .HasMany(e => e.ExportDetails)
                .WithOne(d => d.Export)
                .HasForeignKey(d => d.ExportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.ImportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.ExportDetails)
                .WithOne(d => d.Product)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryCheck>()
                .HasMany(c => c.Details)
                .WithOne(d => d.InventoryCheck)
                .HasForeignKey(d => d.InventoryCheckId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. XỬ LÝ KIỂU DECIMAL CHO SQLITE (Nếu dùng SQLite thì convert sang double)
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(decimal));

                    foreach (var property in properties)
                    {
                        modelBuilder.Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion<double>();
                    }
                }
            }
            // Nếu dùng SQL Server thì giữ nguyên cấu hình cũ (nếu muốn dùng song song)
            else
            {
                modelBuilder.Entity<Product>().Property(p => p.SalePrice).HasColumnType("decimal(18,2)");
                modelBuilder.Entity<ImportDetail>().Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
                modelBuilder.Entity<ExportDetail>().Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
            }
        }
    }
}