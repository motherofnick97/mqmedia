using System.ComponentModel.DataAnnotations;

namespace MqSocial.Companies.Dto;

public class CreateCompanyDto
{
    [Required]
    public string Name { get; set; }
}
