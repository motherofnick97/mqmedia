using Abp.Application.Services.Dto;
using System;

namespace MqSocial.ContractKolReviews.Dto;

public class ContractKolReviewDto : EntityDto<Guid>
{
    public Guid ContractKolId { get; set; }

    public string Review { get; set; }
}
