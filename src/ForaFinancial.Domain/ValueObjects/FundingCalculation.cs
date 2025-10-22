namespace ForaFinancial.Domain.ValueObjects;

/// <summary>
/// Value object representing the result of a funding calculation
/// </summary>
public record struct FundingCalculation(
    int CompanyId,
    string CompanyName,
    decimal StandardFundableAmount,
    decimal SpecialFundableAmount
);
