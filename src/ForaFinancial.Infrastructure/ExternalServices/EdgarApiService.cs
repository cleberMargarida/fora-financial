using System.Text.Json;
using ForaFinancial.Application.DTOs;
using ForaFinancial.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ForaFinancial.Infrastructure.ExternalServices;

/// <summary>
/// Service for fetching company data from the SEC EDGAR API with built-in resilience policies.
/// </summary>
public class EdgarApiService(HttpClient httpClient, ILogger<EdgarApiService> logger) : IEdgarApiService
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public async Task<EdgarCompanyInfo?> GetCompanyFactsAsync(int cik, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"CIK{cik:D10}.json", cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonSerializer.Deserialize<EdgarCompanyInfo>(content, _options);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Company data not found for CIK {Cik}. This company may not have XBRL data available in EDGAR.", cik);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error fetching data for CIK {Cik}. This may be due to network issues or API unavailability.", cik);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, "Timeout fetching data for CIK {Cik}. The request exceeded the configured timeout.", cik);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request cancelled for CIK {Cik}", cik);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching data for CIK {Cik}", cik);
            throw;
        }
    }
}
