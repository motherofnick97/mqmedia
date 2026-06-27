using MqSocial.Common.Enum;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractKolResults.Dto;

public class CreateContractKolResultDto
{
    [Required]
    public Guid ContractKolId { get; set; }

    public DateTime? PostTime { get; set; }

    public string PostLink { get; set; }

    public int? View { get; set; }

    public int? Comment { get; set; }

    public int? Save { get; set; }

    public int? Share { get; set; }

    public int? Like { get; set; }

    public ChannelType ChannelType { get; set; }
}
