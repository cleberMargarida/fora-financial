using FluentAssertions;
using ForaFinancial.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ForaFinancial.IntegrationTests;

/// <summary>
/// Integration tests for company funding calculation scenarios
/// Tests based on real SEC EDGAR data that was seeded during application startup
/// </summary>
[Collection(nameof(IntegrationTest))]
public class CompanyFundingCalculationTests(WebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetCompanies_LumenTechnologies_ReturnsZeroFunding_WhenNegativeIncomeIn2022()
    {
        // Arrange - Data already seeded from SEC EDGAR API during startup
        // Lumen Technologies (CIK: 18926) has:
        // 2018: -$1,733,000,000
        // 2019: -$5,269,000,000
        // 2020: -$1,232,000,000
        // 2021: +$2,033,000,000 (positive)
        // 2022: -$1,548,000,000 (negative - fails eligibility)

        // Act
        var response = await _client.GetAsync("/api/companies?startsWith=L", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyFundingResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        companies.Should().NotBeNull();
        
        var lumen = companies!.FirstOrDefault(c => c.Name == "LUMEN TECHNOLOGIES, INC.");
        lumen.Should().NotBeNull();
        lumen!.Id.Should().BeGreaterThan(0);
        lumen.Name.Should().Be("LUMEN TECHNOLOGIES, INC.");
        
        // Company is NOT eligible because 2022 income is negative
        lumen.StandardFundableAmount.Should().Be(0m, 
            "because 2022 income is negative, failing the eligibility requirement");
        lumen.SpecialFundableAmount.Should().Be(0m,
            "because standard fundable amount is 0");
    }

    [Fact]
    public async Task GetCompanies_ChartIndustries_CalculatesFundingCorrectly_WithIncomeDecline()
    {
        // Arrange - Data already seeded from SEC EDGAR API during startup
        // Chart Industries Inc (CIK: 892553) has:
        // 2018: $88,000,000
        // 2019: $46,400,000
        // 2020: $308,100,000 (highest income)
        // 2021: $59,100,000
        // 2022: $24,000,000 (lower than 2021 - triggers 25% penalty)

        // Act
        var response = await _client.GetAsync("/api/companies?startsWith=C", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyFundingResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        companies.Should().NotBeNull();
        
        var chart = companies!.FirstOrDefault(c => c.Name == "CHART INDUSTRIES INC");
        chart.Should().NotBeNull();
        chart!.Name.Should().Be("CHART INDUSTRIES INC");
        
        // Expected calculations:
        // Highest income: $308,100,000 (2020)
        // Standard: $308,100,000 × 21.51% = $66,272,310
        chart.StandardFundableAmount.Should().Be(66_272_310m,
            "because highest income ($308.1M) × 21.51% = $66,272,310");
        
        // Special: $66,272,310 - (25% penalty) = $49,704,232.50
        // No vowel bonus (starts with 'C')
        // Has income decline (2022 < 2021)
        chart.SpecialFundableAmount.Should().Be(49_704_232.5m,
            "because standard ($66,272,310) minus 25% penalty ($16,568,077.50) = $49,704,232.50");
    }

    [Fact]
    public async Task GetCompanies_WithFilter_ReturnsFilteredCompanies()
    {
        // Act
        var response = await _client.GetAsync("/api/companies?startsWith=C", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyFundingResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        companies.Should().NotBeNull();
        companies!.Should().OnlyContain(c => c.Name.StartsWith('C'),
            "because filter should only return companies starting with 'C'");
    }

    [Fact]
    public async Task GetCompanies_NoFilter_ReturnsAllCompanies()
    {
        // Act
        var response = await _client.GetAsync("/api/companies", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyFundingResponse>>(cancellationToken: TestContext.Current.CancellationToken);
        companies.Should().NotBeNull();
        companies.Should().NotBeEmpty("because database should have seeded companies");
    }
}
