using ForaFinancial.Application.DTOs;
using ForaFinancial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ForaFinancial.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController(IFundingCalculationService fundingService) : ControllerBase
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Retrieves all companies with their calculated standard and special fundable amounts.
    /// Optionally filter by company name starting with a specific letter.
    /// </remarks>
    /// <param name="startsWith">Optional: Filter companies where name starts with this letter/string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of companies with their funding amounts</returns>
    /// <response code="200">Returns the list of companies with funding calculations</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CompanyFundingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanies([FromQuery] string? startsWith = null, CancellationToken cancellationToken = default)
    {
        var companies = await fundingService.GetCompanyFundingAsync(startsWith, cancellationToken);
        return Ok(companies);
    }
}
