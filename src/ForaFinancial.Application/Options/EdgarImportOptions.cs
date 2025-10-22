namespace ForaFinancial.Application.Options;

/// <summary>
/// Configuration options for EDGAR data import
/// </summary>
public class EdgarImportOptions
{
    public const string SectionName = "EdgarImport";

    /// <summary>
    /// List of company CIKs to import from EDGAR API
    /// </summary>
    public int[] CompanyCiks { get; set; } = [];
}
