using ForaFinancial.Application.DTOs;

namespace ForaFinancial.Application.Interfaces;

public interface IEdgarApiService
{
    Task<EdgarCompanyInfo?> GetCompanyFactsAsync(int cik, CancellationToken cancellationToken = default);
}
