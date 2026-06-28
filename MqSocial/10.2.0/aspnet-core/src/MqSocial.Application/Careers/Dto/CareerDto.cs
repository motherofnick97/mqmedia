using Abp.Application.Services.Dto;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Careers.Dto;

public class CareerDto : EntityDto<Guid>
{
    [Required]
    public string Name { get; set; }
}
