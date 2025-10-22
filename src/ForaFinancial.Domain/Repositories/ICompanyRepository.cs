using ForaFinancial.Domain.Entities;

namespace ForaFinancial.Domain.Repositories;

/// <summary>
/// Repository interface for Company aggregate
/// </summary>
public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Company?> GetByCikAsync(int cik, CancellationToken cancellationToken = default);
    Task<List<Company>> GetAllAsync(string? nameStartsWith = null, CancellationToken cancellationToken = default);
    Task<Company> AddAsync(Company company, CancellationToken cancellationToken = default);
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);
    Task DeleteAsync(Company company, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int cik, CancellationToken cancellationToken = default);
}
