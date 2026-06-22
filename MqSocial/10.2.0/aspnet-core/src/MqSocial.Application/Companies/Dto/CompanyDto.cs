using Abp.Application.Services.Dto;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Companies.Dto;

public class CompanyDto : EntityDto<int>
{
    [Required]
    public string Name { get; set; }
}
