using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using System;

namespace MqSocial.ContractKolReviews.Dto;

public class PagedContractKolReviewRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Sorting { get; set; }

    public Guid? ContractKolId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
            Sorting = "CreationTime DESC";
    }
}
