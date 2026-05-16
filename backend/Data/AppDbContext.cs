using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationLineItem> QuotationLineItems => Set<QuotationLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Email).HasMaxLength(200);
            entity.Property(c => c.Phone).HasMaxLength(20);
            entity.Property(c => c.Notes).HasColumnType("text");

            entity.HasIndex(c => c.Name);
            entity.HasIndex(c => c.Email);
            entity.HasIndex(c => c.Phone);
        });

        // Quotation configuration
        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.ClientId).IsRequired(false);

            entity.Property(q => q.QuotationNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(q => q.QuotationNumber)
                .IsUnique();

            entity.Property(q => q.ClientName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(q => q.ClientEmail).HasMaxLength(200);
            entity.Property(q => q.ClientPhone).HasMaxLength(20);

            entity.Property(q => q.SubTotal).HasPrecision(18, 2);
            entity.Property(q => q.GstPercentage).HasPrecision(5, 2);
            entity.Property(q => q.GstAmount).HasPrecision(18, 2);
            entity.Property(q => q.TotalAmount).HasPrecision(18, 2);

            entity.Property(q => q.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasOne(q => q.Client)
                .WithMany(c => c.Quotations)
                .HasForeignKey(q => q.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(q => q.LineItems)
                .WithOne(li => li.Quotation)
                .HasForeignKey(li => li.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Line item configuration
        modelBuilder.Entity<QuotationLineItem>(entity =>
        {
            entity.HasKey(li => li.Id);

            entity.Property(li => li.Description)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(li => li.UnitPrice).HasPrecision(18, 2);
            entity.Property(li => li.Amount).HasPrecision(18, 2);
        });
    }
}
