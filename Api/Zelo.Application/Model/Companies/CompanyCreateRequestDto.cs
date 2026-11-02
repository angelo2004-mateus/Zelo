using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Companies;

public class CompanyCreateRequestDto
{
    [Required]
    public required string LegalName { get; set; }

    public string? TradeName { get; set; }

    [Required]
    public required string FederalTaxNumber { get; set; }
}
