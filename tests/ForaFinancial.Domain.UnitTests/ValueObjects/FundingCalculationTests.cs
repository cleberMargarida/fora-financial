using FluentAssertions;
using ForaFinancial.Domain.ValueObjects;
using Xunit;

namespace ForaFinancial.Domain.UnitTests.ValueObjects;

public class FundingCalculationTests
{
    [Fact]
    public void Constructor_ShouldCreateFundingCalculation_WithAllProperties()
    {
        // Arrange & Act
        var calculation = new FundingCalculation(
            CompanyId: 1,
            CompanyName: "Test Company",
            StandardFundableAmount: 1000000.50m,
            SpecialFundableAmount: 1150000.75m
        );

        // Assert
        calculation.CompanyId.Should().Be(1);
        calculation.CompanyName.Should().Be("Test Company");
        calculation.StandardFundableAmount.Should().Be(1000000.50m);
        calculation.SpecialFundableAmount.Should().Be(1150000.75m);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenAllPropertiesMatch()
    {
        // Arrange
        var calculation1 = new FundingCalculation(1, "Test", 1000m, 1100m);
        var calculation2 = new FundingCalculation(1, "Test", 1000m, 1100m);

        // Act & Assert
        calculation1.Should().Be(calculation2);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenPropertiesDiffer()
    {
        // Arrange
        var calculation1 = new FundingCalculation(1, "Test", 1000m, 1100m);
        var calculation2 = new FundingCalculation(2, "Test", 1000m, 1100m);

        // Act & Assert
        calculation1.Should().NotBe(calculation2);
    }

    [Fact]
    public void Deconstruct_ShouldExtractAllProperties()
    {
        // Arrange
        var calculation = new FundingCalculation(1, "Test Company", 1000m, 1100m);

        // Act
        var (id, name, standard, special) = calculation;

        // Assert
        id.Should().Be(1);
        name.Should().Be("Test Company");
        standard.Should().Be(1000m);
        special.Should().Be(1100m);
    }
}
