using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractTemplates.Dto;

public class CreateContractTemplateDto
{
    [Required]
    public string Name { get; set; }

    public string FilePath { get; set; }
}
