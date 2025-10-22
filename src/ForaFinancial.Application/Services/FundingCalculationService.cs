using ForaFinancial.Application.DTOs;
using ForaFinancial.Application.Interfaces;
using ForaFinancial.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ForaFinancial.Application.Services;

public class FundingCalculationService(ICompanyRepository companyRepository, ILogger<FundingCalculationService> logger) : IFundingCalculationService
{
    public async Task<List<CompanyFundingResponse>> GetCompanyFundingAsync(string? startsWith = null, CancellationToken cancellationToken = default)
    {
        var companies = await companyRepository.GetAllAsync(startsWith, cancellationToken);
        var results = new List<CompanyFundingResponse>();

        foreach (var company in companies)
        {
            if (!company.IsEligibleForFunding())
                logger.LogDebug("Company {Name} is not eligible for funding", company.Name);

            var funding = company.CalculateFunding();

            results.Add(new CompanyFundingResponse
            {
                Id = funding.CompanyId,
                Name = funding.CompanyName,
                StandardFundableAmount = funding.StandardFundableAmount,
                SpecialFundableAmount = funding.SpecialFundableAmount
            });
        }

        return [.. results.OrderBy(r => r.Id)];
    }
}
