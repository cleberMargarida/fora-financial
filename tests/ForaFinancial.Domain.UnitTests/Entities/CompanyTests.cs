using FluentAssertions;
using ForaFinancial.Domain.Entities;
using ForaFinancial.Domain.ValueObjects;
using Xunit;

namespace ForaFinancial.Domain.UnitTests.Entities;

public class CompanyTests
{
    [Fact]
    public void Constructor_ShouldCreateCompany_WithValidData()
    {
        // Arrange
        var cik = 123456;
        var name = "Test Company Inc.";

        // Act
        var company = new Company(cik, name);

        // Assert
        company.Cik.Should().Be(cik);
        company.Name.Should().Be(name);
        company.Incomes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenCikIsZero()
    {
        // Arrange
        var cik = 0;
        var name = "Test Company";

        // Act
        var act = () => new Company(cik, name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("CIK must be positive*")
            .WithParameterName("cik");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenCikIsNegative()
    {
        // Arrange
        var cik = -1;
        var name = "Test Company";

        // Act
        var act = () => new Company(cik, name);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("CIK must be positive*")
            .WithParameterName("cik");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowException_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var cik = 123456;

        // Act
        var act = () => new Company(cik, invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_ShouldUpdateName()
    {
        // Arrange
        var company = new Company(123456, "Old Name");
        var newName = "New Name";

        // Act
        company.UpdateName(newName);

        // Assert
        company.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_ShouldThrowException_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var company = new Company(123456, "Valid Name");

        // Act
        var act = () => company.UpdateName(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void AddIncome_ShouldAddIncomeToCollection()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        var income = new Income(company.Id, 2022, Money.FromDollar(1000000m));

        // Act
        company.AddIncome(income);

        // Assert
        company.Incomes.Should().HaveCount(1);
        company.Incomes.Should().Contain(income);
    }

    [Fact]
    public void AddIncome_ShouldReplaceExistingIncome_WhenYearMatches()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        var income1 = new Income(company.Id, 2022, Money.FromDollar(1000000m));
        var income2 = new Income(company.Id, 2022, Money.FromDollar(2000000m));

        // Act
        company.AddIncome(income1);
        company.AddIncome(income2);

        // Assert
        company.Incomes.Should().HaveCount(1);
        company.Incomes.First().Amount.Should().Be(Money.FromDollar(2000000m));
    }

    [Fact]
    public void AddIncome_ShouldAddMultipleIncomes_ForDifferentYears()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        var income2021 = new Income(company.Id, 2021, Money.FromDollar(1000000m));
        var income2022 = new Income(company.Id, 2022, Money.FromDollar(2000000m));

        // Act
        company.AddIncome(income2021);
        company.AddIncome(income2022);

        // Assert
        company.Incomes.Should().HaveCount(2);
    }

    [Fact]
    public void ClearIncomeData_ShouldRemoveAllIncomes()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(2000000m)));

        // Act
        company.ClearIncomeData();

        // Assert
        company.Incomes.Should().BeEmpty();
    }

    [Fact]
    public void IsEligibleForFunding_ShouldReturnFalse_WhenNoIncomeData()
    {
        // Arrange
        var company = new Company(123456, "Test Company");

        // Act
        var result = company.IsEligibleForFunding();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEligibleForFunding_ShouldReturnFalse_WhenMissingRequiredYears()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(2000000m)));

        // Act
        var result = company.IsEligibleForFunding();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEligibleForFunding_ShouldReturnFalse_When2021IncomeIsZero()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.Zero));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(2000000m)));

        // Act
        var result = company.IsEligibleForFunding();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEligibleForFunding_ShouldReturnFalse_When2022IncomeIsZero()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.Zero));

        // Act
        var result = company.IsEligibleForFunding();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEligibleForFunding_ShouldReturnTrue_WhenAllRequirementsAreMet()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1000000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(2000000m)));

        // Act
        var result = company.IsEligibleForFunding();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CalculateFunding_ShouldReturnZero_WhenNotEligible()
    {
        // Arrange
        var company = new Company(123456, "Test Company");

        // Act
        var result = company.CalculateFunding();

        // Assert
        result.StandardFundableAmount.Should().Be(0m);
        result.SpecialFundableAmount.Should().Be(0m);
    }

    [Fact]
    public void CalculateFunding_ShouldApplyLowIncomeRate_WhenBelowThreshold()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        var highestIncome = Money.FromDollar(5_000_000_000m); // Below 10B threshold
        
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(2_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(3_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(4_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, highestIncome));

        // Act
        var result = company.CalculateFunding();

        // Assert
        var expected = Math.Round(5_000_000_000m * 0.2151m, 2);
        result.StandardFundableAmount.Should().Be(expected);
    }

    [Fact]
    public void CalculateFunding_ShouldApplyHighIncomeRate_WhenAboveThreshold()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        var highestIncome = Money.FromDollar(15_000_000_000m); // Above 10B threshold
        
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(10_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(11_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(12_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(13_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, highestIncome));

        // Act
        var result = company.CalculateFunding();

        // Assert
        var expected = Math.Round(15_000_000_000m * 0.1233m, 2);
        result.StandardFundableAmount.Should().Be(expected);
    }

    [Theory]
    [InlineData("Apple Inc.")]
    [InlineData("Amazon")]
    [InlineData("Exxon Mobil")]
    [InlineData("Intel Corporation")]
    [InlineData("Oracle")]
    [InlineData("Uber Technologies")]
    public void CalculateFunding_ShouldApplyVowelBonus_WhenNameStartsWithVowel(string name)
    {
        // Arrange
        var company = new Company(123456, name);
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(1_000_000_000m)));

        // Act
        var result = company.CalculateFunding();

        // Assert
        var standardAmount = 1_000_000_000m * 0.2151m;
        var expectedSpecial = Math.Round(standardAmount + (standardAmount * 0.15m), 2);
        result.SpecialFundableAmount.Should().Be(expectedSpecial);
    }

    [Theory]
    [InlineData("Boeing")]
    [InlineData("Microsoft")]
    [InlineData("Tesla")]
    public void CalculateFunding_ShouldNotApplyVowelBonus_WhenNameDoesNotStartWithVowel(string name)
    {
        // Arrange
        var company = new Company(123456, name);
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(1_000_000_000m)));

        // Act
        var result = company.CalculateFunding();

        // Assert
        var expected = Math.Round(1_000_000_000m * 0.2151m, 2);
        result.SpecialFundableAmount.Should().Be(expected);
    }

    [Fact]
    public void CalculateFunding_ShouldApplyDeclinePenalty_WhenIncomeDeclines()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(2_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(1_500_000_000m))); // Decline

        // Act
        var result = company.CalculateFunding();

        // Assert
        var standardAmount = 2_000_000_000m * 0.2151m;
        var expectedSpecial = Math.Round(standardAmount - (standardAmount * 0.25m), 2);
        result.SpecialFundableAmount.Should().Be(expectedSpecial);
    }

    [Fact]
    public void CalculateFunding_ShouldNotApplyDeclinePenalty_WhenIncomeGrows()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1_500_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(2_000_000_000m))); // Growth

        // Act
        var result = company.CalculateFunding();

        // Assert
        var expected = Math.Round(2_000_000_000m * 0.2151m, 2);
        result.SpecialFundableAmount.Should().Be(expected);
    }

    [Fact]
    public void CalculateFunding_ShouldApplyBothVowelBonusAndDeclinePenalty()
    {
        // Arrange
        var company = new Company(123456, "Apple Inc.");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(2_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(1_500_000_000m))); // Decline

        // Act
        var result = company.CalculateFunding();

        // Assert
        var standardAmount = 2_000_000_000m * 0.2151m;
        var withVowelBonus = standardAmount + (standardAmount * 0.15m);
        var withDeclinePenalty = withVowelBonus - (standardAmount * 0.25m);
        var expected = Math.Round(withDeclinePenalty, 2);
        result.SpecialFundableAmount.Should().Be(expected);
    }

    [Fact]
    public void CalculateFunding_ShouldReturnCorrectCompanyInfo()
    {
        // Arrange
        var company = new Company(123456, "Test Company Inc.");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1_000_000_000m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(1_000_000_000m)));

        // Act
        var result = company.CalculateFunding();

        // Assert
        result.CompanyId.Should().Be(company.Id);
        result.CompanyName.Should().Be("Test Company Inc.");
    }

    [Fact]
    public void CalculateFunding_ShouldRoundToTwoDecimalPlaces()
    {
        // Arrange
        var company = new Company(123456, "Test Company");
        company.AddIncome(new Income(company.Id, 2018, Money.FromDollar(1_234_567.89m)));
        company.AddIncome(new Income(company.Id, 2019, Money.FromDollar(1_234_567.89m)));
        company.AddIncome(new Income(company.Id, 2020, Money.FromDollar(1_234_567.89m)));
        company.AddIncome(new Income(company.Id, 2021, Money.FromDollar(1_234_567.89m)));
        company.AddIncome(new Income(company.Id, 2022, Money.FromDollar(1_234_567.89m)));

        // Act
        var result = company.CalculateFunding();

        // Assert
        result.StandardFundableAmount.Should().Be(Math.Round(1_234_567.89m * 0.2151m, 2));
        result.SpecialFundableAmount.Should().Be(Math.Round(1_234_567.89m * 0.2151m, 2));
    }
}
