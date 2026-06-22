using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using MqSocial.Contracts;

namespace MqSocial.Contracts.Dto;

public class PagedContractRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Keyword { get; set; }

    public string Sorting { get; set; }

    public ContractStatus? Status { get; set; }

    public int? CampaignId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "Name";
        }

        Keyword = Keyword?.Trim();
    }
}
