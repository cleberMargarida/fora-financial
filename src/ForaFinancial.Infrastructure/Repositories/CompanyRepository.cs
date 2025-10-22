using Microsoft.EntityFrameworkCore;
using ForaFinancial.Domain.Entities;
using ForaFinancial.Domain.Repositories;
using ForaFinancial.Infrastructure.Data;

namespace ForaFinancial.Infrastructure.Repositories;

public class CompanyRepository(CompaniesContext context) : ICompanyRepository
{
    public async Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Companies
            .Include(c => c.Incomes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Company?> GetByCikAsync(int cik, CancellationToken cancellationToken = default)
    {
        return await context.Companies
            .Include(c => c.Incomes)
            .FirstOrDefaultAsync(c => c.Cik == cik, cancellationToken);
    }

    public async Task<List<Company>> GetAllAsync(string? nameStartsWith = null, CancellationToken cancellationToken = default)
    {
        var query = context.Companies.Include(c => c.Incomes).AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameStartsWith))
        {
            query = query.Where(c => c.Name.StartsWith(nameStartsWith));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Company> AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);
        return company;
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        context.Companies.Update(company);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Company company, CancellationToken cancellationToken = default)
    {
        context.Companies.Remove(company);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int cik, CancellationToken cancellationToken = default)
    {
        return await context.Companies.AnyAsync(c => c.Cik == cik, cancellationToken);
    }
}
