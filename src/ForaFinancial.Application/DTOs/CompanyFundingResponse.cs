namespace ForaFinancial.Application.DTOs;

/// <summary>
/// DTO for company funding response
/// </summary>
public class CompanyFundingResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the company
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the company name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the standard fundable amount calculated based on income thresholds
    /// </summary>
    public decimal StandardFundableAmount { get; set; }
    
    /// <summary>
    /// Gets or sets the special fundable amount with bonuses/penalties applied
    /// </summary>
    public decimal SpecialFundableAmount { get; set; }
}
