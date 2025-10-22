using FluentAssertions;
using ForaFinancial.Domain.Entities;
using ForaFinancial.Domain.ValueObjects;
using Xunit;

namespace ForaFinancial.Domain.UnitTests.Entities;

public class IncomeTests
{
    [Fact]
    public void Constructor_ShouldCreateIncome_WithValidData()
    {
        // Arrange
        var companyId = 1;
        var year = 2022;
        var amount = Money.FromDollar(1000000m);

        // Act
        var income = new Income(companyId, year, amount);

        // Assert
        income.CompanyId.Should().Be(companyId);
        income.Year.Should().Be(year);
        income.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void Constructor_ShouldThrowException_WhenYearIsInvalid(int invalidYear)
    {
        // Arrange
        var companyId = 1;
        var amount = Money.FromDollar(1000000m);

        // Act
        var act = () => new Income(companyId, invalidYear, amount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid year*")
            .WithParameterName("year");
    }

    [Theory]
    [InlineData(1900)]
    [InlineData(2000)]
    [InlineData(2022)]
    [InlineData(2100)]
    public void Constructor_ShouldAcceptValidYears(int validYear)
    {
        // Arrange
        var companyId = 1;
        var amount = Money.FromDollar(1000000m);

        // Act
        var income = new Income(companyId, validYear, amount);

        // Assert
        income.Year.Should().Be(validYear);
    }

    [Fact]
    public void UpdateAmount_ShouldUpdateAmount()
    {
        // Arrange
        var income = new Income(1, 2022, Money.FromDollar(1000000m));
        var newAmount = Money.FromDollar(2000000m);

        // Act
        income.UpdateAmount(newAmount);

        // Assert
        income.Amount.Should().Be(newAmount);
    }

    [Fact]
    public void UpdateAmount_ShouldAllowZeroAmount()
    {
        // Arrange
        var income = new Income(1, 2022, Money.FromDollar(1000000m));

        // Act
        income.UpdateAmount(Money.Zero);

        // Assert
        income.Amount.Should().Be(Money.Zero);
    }

    [Fact]
    public void UpdateAmount_ShouldAllowNegativeAmount()
    {
        // Arrange
        var income = new Income(1, 2022, Money.FromDollar(1000000m));
        var negativeAmount = Money.FromDollar(-500000m);

        // Act
        income.UpdateAmount(negativeAmount);

        // Assert
        income.Amount.Should().Be(negativeAmount);
    }
}
