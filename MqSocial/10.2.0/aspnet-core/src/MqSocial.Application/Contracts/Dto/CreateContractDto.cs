using MqSocial.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Contracts.Dto;

public class CreateContractDto
{
    [Required]
    [StringLength(Contract.MaxNameLength)]
    public string Name { get; set; }

    [StringLength(Contract.MaxDescriptionLength)]
    public string Note { get; set; }

    public Guid? CampaignId { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Prepare;

    public int? Budget { get; set; }

    public string Request { get; set; }

    public string Script { get; set; }

    public int MaxReviewTime { get; set; }

    public string ProductName { get; set; }
}
