using ForaFinancial.Domain.ValueObjects;

namespace ForaFinancial.Domain.Entities;

/// <summary>
/// Income entity - represents yearly income for a company
/// </summary>
public class Income
{
    public int Id { get; private set; }
    public int CompanyId { get; private set; }
    public int Year { get; private set; }
    public Money Amount { get; private set; }
    
    public Company? Company { get; private set; }

    // EF Core constructor
    private Income() { }

    public Income(int companyId, int year, Money amount)
    {
        if (year < 1900 || year > 2100)
            throw new ArgumentException("Invalid year", nameof(year));

        CompanyId = companyId;
        Year = year;
        Amount = amount;
    }

    public void UpdateAmount(Money amount)
    {
        Amount = amount;
    }
}
