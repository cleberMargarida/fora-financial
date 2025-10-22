using ForaFinancial.Application.DTOs;
using ForaFinancial.Application.Interfaces;
using ForaFinancial.Application.Options;
using ForaFinancial.Domain.Entities;
using ForaFinancial.Domain.Repositories;
using ForaFinancial.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForaFinancial.Application.Services;

/// <summary>
/// Background service that imports company data from SEC EDGAR API at application startup.
/// </summary>
public class ImportBackgroundService(
    IServiceProvider serviceProvider,
    IEdgarApiService edgarApiService,
    IOptions<EdgarImportOptions> options,
    ILogger<ImportBackgroundService> logger) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<ImportBackgroundService> _logger = logger;
    private readonly IEdgarApiService _edgarApiService = edgarApiService;
    private readonly EdgarImportOptions _options = options.Value;
    private ICompanyRepository _companyRepository = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting background data import");

            // Create a scope to resolve scoped services
            await using var scope = _serviceProvider.CreateAsyncScope();

            _companyRepository = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();

            await ImportAllCompaniesAsync(cancellationToken);

            _logger.LogInformation("Background data import completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during background data import");
        }
    }

    private async Task ImportAllCompaniesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting import of {Count} companies", _options.CompanyCiks.Length);

        foreach (var cik in _options.CompanyCiks)
        {
            try
            {
                await ImportCompanyAsync(cik, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing company with CIK {Cik}", cik);
            }
        }

        _logger.LogInformation("Import completed");
    }

    private async Task ImportCompanyAsync(int cik, CancellationToken cancellationToken)
    {
        var existingCompany = await _companyRepository.GetByCikAsync(cik, cancellationToken);

        if (existingCompany != null)
        {
            _logger.LogInformation("Company {Name} (CIK: {Cik}) already exists in database, skipping API call",
                existingCompany.Name, cik);
            return;
        }

        var companyInfo = await _edgarApiService.GetCompanyFactsAsync(cik, cancellationToken);

        if (companyInfo == null)
        {
            _logger.LogWarning("No data received for CIK {Cik}", cik);
            return;
        }

        var company = new Company(cik, companyInfo.EntityName);

        var incomeData = ExtractIncomeData(companyInfo);

        foreach (var data in incomeData)
        {
            company.AddIncome(data);
        }

        await _companyRepository.AddAsync(company, cancellationToken);

        _logger.LogInformation("Imported {Name} (CIK: {Cik}) with {Count} income records", companyInfo.EntityName, cik, company.Incomes.Count);
    }

    private static IEnumerable<Income> ExtractIncomeData(EdgarCompanyInfo companyInfo)
    {
        var result = new List<Income>();

        var usdData = companyInfo.Facts?.UsGaap?.NetIncomeLoss?.Units?.Usd;

        if (usdData == null)
        {
            return result;
        }

        if (usdData.Length == 0)
        {
            return result;
        }

        return usdData
            .Where(d => d.Form == "10-K" && d.Frame is { Length: 6 } && d.Frame.StartsWith("CY"))
            .GroupBy(d => int.Parse(d.Frame[2..]))
            .Select(g => new Income(companyInfo.Cik, g.Key, Money.FromDollar(g.MaxBy(d => Math.Abs(d.Val))!.Val)));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
