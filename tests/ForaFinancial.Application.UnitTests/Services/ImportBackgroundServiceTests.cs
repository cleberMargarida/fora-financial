using FluentAssertions;
using ForaFinancial.Application.DTOs;
using ForaFinancial.Application.Interfaces;
using ForaFinancial.Application.Options;
using ForaFinancial.Application.Services;
using ForaFinancial.Domain.Entities;
using ForaFinancial.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ForaFinancial.Application.UnitTests.Services;

public class ImportBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IEdgarApiService> _mockEdgarApiService;
    private readonly Mock<ICompanyRepository> _mockRepository;
    private readonly Mock<ILogger<ImportBackgroundService>> _mockLogger;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly EdgarImportOptions _options;

    public ImportBackgroundServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockEdgarApiService = new Mock<IEdgarApiService>();
        _mockRepository = new Mock<ICompanyRepository>();
        _mockLogger = new Mock<ILogger<ImportBackgroundService>>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _options = new EdgarImportOptions
        {
            CompanyCiks = [320193, 789019, 1318605]
        };

        // Setup service provider to return scoped services
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ICompanyRepository)))
            .Returns(_mockRepository.Object);
        
        _mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(_mockScope.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldImportAllConfiguredCompanies()
    {
        // Arrange
        var service = CreateService();
        SetupEdgarApiForSuccessfulImport();

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockEdgarApiService.Verify(
            s => s.GetCompanyFactsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_ShouldAddCompanyToRepository_WhenNotExists()
    {
        // Arrange
        var service = CreateService();
        SetupEdgarApiForSuccessfulImport();
        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_ShouldSkipImport_WhenCompanyAlreadyExists()
    {
        // Arrange
        var service = CreateService();
        var existingCompany = new Company(320193, "Apple Inc.");

        _mockRepository
            .Setup(r => r.GetByCikAsync(320193, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCompany);
        _mockRepository
            .Setup(r => r.GetByCikAsync(789019, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockRepository
            .Setup(r => r.GetByCikAsync(1318605, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        SetupEdgarApiForSuccessfulImport();

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockEdgarApiService.Verify(
            s => s.GetCompanyFactsAsync(320193, It.IsAny<CancellationToken>()),
            Times.Never);
        _mockEdgarApiService.Verify(
            s => s.GetCompanyFactsAsync(789019, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockEdgarApiService.Verify(
            s => s.GetCompanyFactsAsync(1318605, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldCreateCompanyWithCorrectData()
    {
        // Arrange
        var service = CreateService();
        var companyInfo = CreateEdgarCompanyInfo(320193, "Apple Inc.");
        
        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(320193, It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyInfo);

        Company? capturedCompany = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((c, ct) => capturedCompany = c)
            .ReturnsAsync((Company c, CancellationToken ct) => c);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        capturedCompany.Should().NotBeNull();
        capturedCompany!.Cik.Should().Be(320193);
        capturedCompany.Name.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task StartAsync_ShouldExtractIncomeDataFromEdgarResponse()
    {
        // Arrange
        var service = CreateService();
        var companyInfo = CreateEdgarCompanyInfo(320193, "Apple Inc.");
        
        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(320193, It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyInfo);

        Company? capturedCompany = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((c, ct) => capturedCompany = c)
            .ReturnsAsync((Company c, CancellationToken ct) => c);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        capturedCompany.Should().NotBeNull();
        capturedCompany!.Incomes.Should().HaveCount(2);
        capturedCompany.Incomes.Should().Contain(i => i.Year == 2021);
        capturedCompany.Incomes.Should().Contain(i => i.Year == 2022);
    }

    [Fact]
    public async Task StartAsync_ShouldFilterOnly10KForms()
    {
        // Arrange
        var service = CreateService();
        var companyInfo = new EdgarCompanyInfo
        {
            Cik = 320193,
            EntityName = "Apple Inc.",
            Facts = new EdgarCompanyInfo.InfoFact
            {
                UsGaap = new EdgarCompanyInfo.InfoFactUsGaap
                {
                    NetIncomeLoss = new EdgarCompanyInfo.InfoFactUsGaapNetIncomeLoss
                    {
                        Units = new EdgarCompanyInfo.InfoFactUsGaapIncomeLossUnits
                        {
                            Usd =
                            [
                                new() { Form = "10-K", Frame = "CY2022", Val = 99803000000m },
                                new() { Form = "10-Q", Frame = "CY2022", Val = 50000000000m }, // Should be filtered
                                new() { Form = "10-K", Frame = "CY2021", Val = 94680000000m }
                            ]
                        }
                    }
                }
            }
        };

        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(320193, It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyInfo);

        Company? capturedCompany = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((c, ct) => capturedCompany = c)
            .ReturnsAsync((Company c, CancellationToken ct) => c);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        capturedCompany!.Incomes.Should().HaveCount(2);
        capturedCompany.Incomes.Should().NotContain(i => i.Amount.Amount == 50000000000m);
    }

    [Fact]
    public async Task StartAsync_ShouldHandleNullEdgarResponse()
    {
        // Arrange
        var service = CreateService();
        
        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdgarCompanyInfo?)null);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_ShouldLogWarning_WhenNoDataReceived()
    {
        // Arrange
        var service = CreateService();
        
        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdgarCompanyInfo?)null);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No data received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_ShouldContinueImporting_WhenOneCompanyFails()
    {
        // Arrange
        var service = CreateService();
        
        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(320193, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API error"));
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(789019, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEdgarCompanyInfo(789019, "Microsoft"));
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(1318605, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEdgarCompanyInfo(1318605, "Tesla"));

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task StartAsync_ShouldLogError_WhenCompanyImportFails()
    {
        // Arrange
        var service = CreateService();
        var exception = new Exception("API error");
        
        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error importing company")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_ShouldLogStartAndCompletion()
    {
        // Arrange
        var service = CreateService();
        SetupEdgarApiForSuccessfulImport();

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting background data import")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_ShouldHandleCompanyWithNoFacts()
    {
        // Arrange
        var service = CreateService();
        var companyInfo = new EdgarCompanyInfo
        {
            Cik = 320193,
            EntityName = "Apple Inc.",
            Facts = null
        };

        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyInfo);

        Company? capturedCompany = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((c, ct) => capturedCompany = c)
            .ReturnsAsync((Company c, CancellationToken ct) => c);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        capturedCompany.Should().NotBeNull();
        capturedCompany!.Incomes.Should().BeEmpty();
    }

    [Fact]
    public async Task StartAsync_ShouldSelectMaxAbsoluteValue_WhenMultipleEntriesForSameYear()
    {
        // Arrange
        var service = CreateService();
        var companyInfo = new EdgarCompanyInfo
        {
            Cik = 320193,
            EntityName = "Apple Inc.",
            Facts = new EdgarCompanyInfo.InfoFact
            {
                UsGaap = new EdgarCompanyInfo.InfoFactUsGaap
                {
                    NetIncomeLoss = new EdgarCompanyInfo.InfoFactUsGaapNetIncomeLoss
                    {
                        Units = new EdgarCompanyInfo.InfoFactUsGaapIncomeLossUnits
                        {
                            Usd =
                            [
                                new() { Form = "10-K", Frame = "CY2022", Val = 99803000000m },
                                new() { Form = "10-K", Frame = "CY2022", Val = 50000000000m }, // Lower value
                                new() { Form = "10-K", Frame = "CY2022", Val = -100000000000m } // Higher abs value
                            ]
                        }
                    }
                }
            }
        };

        _mockRepository
            .Setup(r => r.GetByCikAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(320193, It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyInfo);

        Company? capturedCompany = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((c, ct) => capturedCompany = c)
            .ReturnsAsync((Company c, CancellationToken ct) => c);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        capturedCompany!.Incomes.Should().HaveCount(1);
        capturedCompany.Incomes.First().Amount.Amount.Should().Be(-100000000000m);
    }

    private ImportBackgroundService CreateService()
    {
        var options = Microsoft.Extensions.Options.Options.Create(_options);
        return new ImportBackgroundService(
            _mockServiceProvider.Object,
            _mockEdgarApiService.Object,
            options,
            _mockLogger.Object);
    }

    private void SetupEdgarApiForSuccessfulImport()
    {
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(320193, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEdgarCompanyInfo(320193, "Apple Inc."));
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(789019, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEdgarCompanyInfo(789019, "Microsoft Corporation"));
        _mockEdgarApiService
            .Setup(s => s.GetCompanyFactsAsync(1318605, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEdgarCompanyInfo(1318605, "Tesla, Inc."));
    }

    private static EdgarCompanyInfo CreateEdgarCompanyInfo(int cik, string name)
    {
        return new EdgarCompanyInfo
        {
            Cik = cik,
            EntityName = name,
            Facts = new EdgarCompanyInfo.InfoFact
            {
                UsGaap = new EdgarCompanyInfo.InfoFactUsGaap
                {
                    NetIncomeLoss = new EdgarCompanyInfo.InfoFactUsGaapNetIncomeLoss
                    {
                        Units = new EdgarCompanyInfo.InfoFactUsGaapIncomeLossUnits
                        {
                            Usd =
                            [
                                new() { Form = "10-K", Frame = "CY2022", Val = 99803000000m },
                                new() { Form = "10-K", Frame = "CY2021", Val = 94680000000m }
                            ]
                        }
                    }
                }
            }
        };
    }
}
