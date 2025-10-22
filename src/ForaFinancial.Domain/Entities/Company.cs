using ForaFinancial.Domain.ValueObjects;

namespace ForaFinancial.Domain.Entities;

/// <summary>
/// Company aggregate root - contains business logic for funding calculations
/// </summary>
public class Company
{
    private static readonly Money HighIncomeThreshold = Money.FromDollar(10_000_000_000m);
    private const decimal HighIncomeRate = 0.1233m; // 12.33%
    private const decimal LowIncomeRate = 0.2151m;  // 21.51%
    private const decimal VowelBonus = 0.15m;       // 15%
    private const decimal DeclinesPenalty = 0.25m;  // 25%

    public static readonly char[] Vowels = ['A', 'E', 'I', 'O', 'U'];

    public static readonly int[] RequiredYears = [2018, 2019, 2020, 2021, 2022];

    public int Id { get; private set; }
    public int Cik { get; private set; }
    public string Name { get; private set; } = string.Empty;
    
    private readonly List<Income> _incomes = [];
    public IReadOnlyCollection<Income> Incomes => _incomes.AsReadOnly();

    // EF Core constructor
    private Company() { }

    public Company(int cik, string name)
    {
        if (cik <= 0)
            throw new ArgumentException("CIK must be positive", nameof(cik));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Cik = cik;
        Name = name;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        Name = name;
    }

    public void AddIncome(Income income)
    {
        var existingData = _incomes.FirstOrDefault(i => i.Year == income.Year);
        if (existingData != null)
        {
            _incomes.Remove(existingData);
        }

        _incomes.Add(income);
    }

    public void ClearIncomeData()
    {
        _incomes.Clear();
    }

    /// <summary>
    /// Calculates funding based on business rules
    /// </summary>
    public FundingCalculation CalculateFunding()
    {
        var standardIncome = CalculateStandardFundableAmount();
        var specialIncome = CalculateSpecialFundableAmount(standardIncome);

        return new FundingCalculation(
            Id,
            Name,
            Math.Round(standardIncome.Amount, 2),
            Math.Round(specialIncome.Amount, 2)
        );
    }

    /// <summary>
    /// Checks if company is eligible for funding
    /// </summary>
    public bool IsEligibleForFunding()
    {
        var incomeDict = _incomes.ToDictionary(i => i.Year, i => i.Amount);

        if (!RequiredYears.All(incomeDict.ContainsKey))
            return false;

        return incomeDict[2021] > Money.Zero && incomeDict[2022] > Money.Zero;
    }

    private Money CalculateStandardFundableAmount()
    {
        if (!IsEligibleForFunding())
            return Money.Zero;

        var incomeDict = _incomes.ToDictionary(i => i.Year, i => i.Amount);

        var highestIncome = RequiredYears.Max(year => incomeDict[year]);

        return highestIncome >= HighIncomeThreshold
            ? highestIncome * HighIncomeRate
            : highestIncome * LowIncomeRate;
    }

    private Money CalculateSpecialFundableAmount(Money standardAmount)
    {
        var specialAmount = standardAmount;

        if (StartsWithVowel())
            specialAmount += standardAmount * VowelBonus;

        if (HasIncomeDecline())
            specialAmount -= standardAmount * DeclinesPenalty;

        return specialAmount;
    }

    private bool StartsWithVowel()
    {
        return !string.IsNullOrEmpty(Name) && Vowels.Contains(char.ToUpper(Name[0]));
    }

    private bool HasIncomeDecline()
    {
        var income2021 = _incomes.FirstOrDefault(i => i.Year == 2021)?.Amount;
        var income2022 = _incomes.FirstOrDefault(i => i.Year == 2022)?.Amount;

        return income2021.HasValue &&
               income2022.HasValue &&
               income2022.Value < income2021.Value;
    }
}
