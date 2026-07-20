using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace MqSocial.ContractTemplates.Dto;

public class PagedContractTemplateRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Keyword { get; set; }

    public string Sorting { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "Name";
        }

        Keyword = Keyword?.Trim();
    }
}
