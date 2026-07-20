using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractKols.Dto;

public class SendContractKolsEmailDto
{
    [Required]
    public Guid ContractId { get; set; }

    public ContractKolStatus? Status { get; set; }

    [Required]
    public List<string> To { get; set; } = new();

    [Required]
    public string Subject { get; set; }

    public string Body { get; set; }
}
