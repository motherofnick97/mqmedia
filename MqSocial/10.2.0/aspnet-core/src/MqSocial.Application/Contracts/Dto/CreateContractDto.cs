using MqSocial.Contracts;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Contracts.Dto;

public class CreateContractDto
{
    [Required]
    [StringLength(Contract.MaxNameLength)]
    public string Name { get; set; }

    [StringLength(Contract.MaxDescriptionLength)]
    public string Note { get; set; }

    public int? CampaignId { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Prepare;

    public int? Budget { get; set; }
}
