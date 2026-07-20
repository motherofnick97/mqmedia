using Abp.Application.Services.Dto;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractTemplates.Dto;

public class ContractTemplateDto : EntityDto<Guid>
{
    [Required]
    public string Name { get; set; }

    public string FilePath { get; set; }
}
