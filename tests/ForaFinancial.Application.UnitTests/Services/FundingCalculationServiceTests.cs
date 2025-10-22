using FluentAssertions;
using ForaFinancial.Application.DTOs;
using ForaFinancial.Application.Services;
using ForaFinancial.Domain.Entities;
using ForaFinancial.Domain.Repositories;
using ForaFinancial.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ForaFinancial.Application.UnitTests.Services;

public class FundingCalculationServiceTests
{
    private readonly Mock<ICompanyRepository> _mockRepository;
    private readonly Mock<ILogger<FundingCalculationService>> _mockLogger;
    private readonly FundingCalculationService _service;

    public FundingCalculationServiceTests()
    {
        _mockRepository = new Mock<ICompanyRepository>();
        _mockLogger = new Mock<ILogger<FundingCalculationService>>();
        _service = new FundingCalculationService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldReturnEmptyList_WhenNoCompanies()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldReturnFundingForAllCompanies()
    {
        // Arrange
        var company1 = CreateEligibleCompany(1, "Apple Inc.", 5_000_000_000m);
        var company2 = CreateEligibleCompany(2, "Microsoft Corporation", 6_000_000_000m);

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([company1, company2]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Apple Inc.");
        result[1].Id.Should().Be(2);
        result[1].Name.Should().Be("Microsoft Corporation");
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldIncludeIneligibleCompanies_WithZeroFunding()
    {
        // Arrange
        var ineligibleCompany = new Company(12345, "Ineligible Company");
        // No income data, so not eligible

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ineligibleCompany]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].StandardFundableAmount.Should().Be(0m);
        result[0].SpecialFundableAmount.Should().Be(0m);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldCalculateStandardAndSpecialFunding()
    {
        // Arrange
        var company = CreateEligibleCompany(1, "Test Company", 5_000_000_000m);

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([company]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        var funding = company.CalculateFunding();
        result[0].StandardFundableAmount.Should().Be(funding.StandardFundableAmount);
        result[0].SpecialFundableAmount.Should().Be(funding.SpecialFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldPassStartsWithFilter_ToRepository()
    {
        // Arrange
        var startsWith = "A";
        _mockRepository
            .Setup(r => r.GetAllAsync(startsWith, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _service.GetCompanyFundingAsync(startsWith);

        // Assert
        _mockRepository.Verify(
            r => r.GetAllAsync(startsWith, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldReturnResultsOrderedById()
    {
        // Arrange
        var company3 = CreateEligibleCompany(3, "Company C", 1_000_000_000m);
        var company1 = CreateEligibleCompany(1, "Company A", 1_000_000_000m);
        var company2 = CreateEligibleCompany(2, "Company B", 1_000_000_000m);

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([company3, company1, company2]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(3);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldPassCancellationToken_ToRepository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _mockRepository
            .Setup(r => r.GetAllAsync(null, cancellationToken))
            .ReturnsAsync([]);

        // Act
        await _service.GetCompanyFundingAsync(cancellationToken: cancellationToken);

        // Assert
        _mockRepository.Verify(
            r => r.GetAllAsync(null, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldReturnCorrectDTOStructure()
    {
        // Arrange
        var company = CreateEligibleCompany(42, "Test Company Inc.", 5_000_000_000m);

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([company]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        result[0].Should().BeOfType<CompanyFundingResponse>();
        result[0].Id.Should().Be(42);
        result[0].Name.Should().Be("Test Company Inc.");
        result[0].StandardFundableAmount.Should().BeGreaterThan(0);
        result[0].SpecialFundableAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldLogDebug_WhenCompanyNotEligible()
    {
        // Arrange
        var ineligibleCompany = new Company(12345, "Ineligible Company");
        
        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ineligibleCompany]);

        // Act
        await _service.GetCompanyFundingAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not eligible for funding")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldHandleCompanyWithVowelName()
    {
        // Arrange
        var company = CreateEligibleCompany(1, "Apple Inc.", 5_000_000_000m);

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([company]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        result[0].SpecialFundableAmount.Should().BeGreaterThan(result[0].StandardFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFundingAsync_ShouldHandleCompanyWithIncomeDecline()
    {
        // Arrange
        var company = new Company(12345, "Test Company");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(5_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(5_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(5_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(6_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(4_000_000_000m))); // Decline

        _mockRepository
            .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([company]);

        // Act
        var result = await _service.GetCompanyFundingAsync();

        // Assert
        result[0].SpecialFundableAmount.Should().BeLessThan(result[0].StandardFundableAmount);
    }

    private static Company CreateEligibleCompany(int cik, string name, decimal maxIncome)
    {
        var company = new Company(cik, name);
        
        // Use reflection to set the Id property for testing purposes
        typeof(Company).GetProperty(nameof(Company.Id))!.SetValue(company, cik);
        
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(maxIncome * 0.8m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(maxIncome * 0.85m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(maxIncome * 0.9m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(maxIncome * 0.95m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(maxIncome)));
        return company;
    }
}
