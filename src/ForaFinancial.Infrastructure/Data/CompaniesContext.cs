using Microsoft.EntityFrameworkCore;
using ForaFinancial.Domain.Entities;

namespace ForaFinancial.Infrastructure.Data;

public class CompaniesContext(DbContextOptions<CompaniesContext> options) : DbContext(options)
{
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Income> IncomeData { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Cik).IsUnique();
            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.HasMany(c => c.Incomes)
                  .WithOne(i => i.Company)
                  .HasForeignKey(i => i.CompanyId)
                  .IsRequired();

            entity.Navigation(c => c.Incomes)
                  .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Income>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CompanyId, e.Year }).IsUnique();

            entity.ComplexProperty(e => e.Amount, value =>
            {
                value.Property(m => m.Amount)
                     .HasColumnName("Value")
                     .HasPrecision(18, 2)
                     .IsRequired();

                value.Property(m => m.Currency)
                     .HasColumnName("Currency")
                     .IsRequired();
            });
        });
    }
}
