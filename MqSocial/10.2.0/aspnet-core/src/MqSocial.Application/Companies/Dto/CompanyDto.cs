using Abp.Application.Services.Dto;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Companies.Dto;

public class CompanyDto : EntityDto<Guid>
{
    [Required]
    public string Name { get; set; }
}
