using ForaFinancial.Application.DTOs;

namespace ForaFinancial.Application.Interfaces;

public interface IFundingCalculationService
{
    Task<List<CompanyFundingResponse>> GetCompanyFundingAsync(string? startsWith = null, CancellationToken cancellationToken = default);
}
