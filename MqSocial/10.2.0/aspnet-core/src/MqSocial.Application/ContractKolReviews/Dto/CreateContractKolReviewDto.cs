using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractKolReviews.Dto;

public class CreateContractKolReviewDto
{
    [Required]
    public Guid ContractKolId { get; set; }

    public string Review { get; set; }
}
