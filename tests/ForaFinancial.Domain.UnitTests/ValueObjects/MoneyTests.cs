using FluentAssertions;
using ForaFinancial.Domain.ValueObjects;
using Xunit;

namespace ForaFinancial.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldCreateMoneyWithAmountAndCurrency()
    {
        // Arrange & Act
        var money = new Money(100.50m, CurrencyCode.USD);

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void FromDollar_ShouldCreateMoneyInUSD()
    {
        // Arrange & Act
        var money = Money.FromDollar(250.75m);

        // Assert
        money.Amount.Should().Be(250.75m);
        money.Currency.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void Zero_ShouldReturnZeroMoney()
    {
        // Arrange & Act
        var zero = Money.Zero;

        // Assert
        zero.Amount.Should().Be(0m);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameAmountAndCurrency()
    {
        // Arrange
        var money1 = Money.FromDollar(100m);
        var money2 = Money.FromDollar(100m);

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentAmounts()
    {
        // Arrange
        var money1 = Money.FromDollar(100m);
        var money2 = Money.FromDollar(200m);

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 != money2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100m, CurrencyCode.USD);
        var money2 = new Money(100m, CurrencyCode.EUR);

        // Act & Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void CompareTo_ShouldReturnPositive_WhenGreaterAmount()
    {
        // Arrange
        var money1 = Money.FromDollar(200m);
        var money2 = Money.FromDollar(100m);

        // Act & Assert
        money1.CompareTo(money2).Should().BePositive();
        (money1 > money2).Should().BeTrue();
        (money1 >= money2).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_ShouldReturnNegative_WhenSmallerAmount()
    {
        // Arrange
        var money1 = Money.FromDollar(100m);
        var money2 = Money.FromDollar(200m);

        // Act & Assert
        money1.CompareTo(money2).Should().BeNegative();
        (money1 < money2).Should().BeTrue();
        (money1 <= money2).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_ShouldThrowException_WhenDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100m, CurrencyCode.USD);
        var money2 = new Money(100m, CurrencyCode.EUR);

        // Act & Assert
        var act = () => money1.CompareTo(money2);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot compare Money with different currencies.");
    }

    [Fact]
    public void ComparisonOperators_ShouldWorkWithZero()
    {
        // Arrange
        var money = Money.FromDollar(100m);
        var zero = Money.Zero;

        // Act & Assert
        (money > zero).Should().BeTrue();
        (money >= zero).Should().BeTrue();
        (zero < money).Should().BeTrue();
        (zero <= money).Should().BeTrue();
    }

    [Fact]
    public void Addition_ShouldAddAmounts_WhenSameCurrency()
    {
        // Arrange
        var money1 = Money.FromDollar(100m);
        var money2 = Money.FromDollar(50m);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void Addition_ShouldThrowException_WhenDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100m, CurrencyCode.USD);
        var money2 = new Money(50m, CurrencyCode.EUR);

        // Act & Assert
        var act = () => money1 + money2;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add Money with different currencies.");
    }

    [Fact]
    public void Addition_WithZero_ShouldReturnOriginalMoney()
    {
        // Arrange
        var money = Money.FromDollar(100m);
        var zero = Money.Zero;

        // Act
        var result1 = money + zero;
        var result2 = zero + money;

        // Assert
        result1.Should().Be(money);
        result2.Should().Be(money);
    }

    [Fact]
    public void Subtraction_ShouldSubtractAmounts_WhenSameCurrency()
    {
        // Arrange
        var money1 = Money.FromDollar(100m);
        var money2 = Money.FromDollar(30m);

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(70m);
        result.Currency.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void Subtraction_ShouldThrowException_WhenDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100m, CurrencyCode.USD);
        var money2 = new Money(50m, CurrencyCode.EUR);

        // Act & Assert
        var act = () => money1 - money2;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot subtract Money with different currencies.");
    }

    [Fact]
    public void Subtraction_WithZero_ShouldReturnOriginalMoney()
    {
        // Arrange
        var money = Money.FromDollar(100m);
        var zero = Money.Zero;

        // Act
        var result = money - zero;

        // Assert
        result.Should().Be(money);
    }

    [Fact]
    public void Multiplication_ShouldMultiplyAmount()
    {
        // Arrange
        var money = Money.FromDollar(100m);

        // Act
        var result = money * 2.5m;

        // Assert
        result.Amount.Should().Be(250m);
        result.Currency.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void Multiplication_ByZero_ShouldReturnZero()
    {
        // Arrange
        var money = Money.FromDollar(100m);

        // Act
        var result = money * 0m;

        // Assert
        result.Should().Be(Money.Zero);
    }

    [Fact]
    public void Division_ShouldDivideAmount()
    {
        // Arrange
        var money = Money.FromDollar(100m);

        // Act
        var result = money / 4m;

        // Assert
        result.Amount.Should().Be(25m);
        result.Currency.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public void Division_ByZero_ShouldThrowException()
    {
        // Arrange
        var money = Money.FromDollar(100m);

        // Act & Assert
        var act = () => money / 0m;
        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void Division_OfZero_ShouldReturnZero()
    {
        // Arrange
        var zero = Money.Zero;

        // Act
        var result = zero / 5m;

        // Assert
        result.Should().Be(Money.Zero);
    }

    [Fact]
    public void GetHashCode_ShouldBeSame_ForEqualMoney()
    {
        // Arrange
        var money1 = Money.FromDollar(100m);
        var money2 = Money.FromDollar(100m);

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }
}
