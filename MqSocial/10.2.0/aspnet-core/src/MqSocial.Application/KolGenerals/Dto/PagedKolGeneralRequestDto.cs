using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace MqSocial.KolGenerals.Dto;

public class PagedKolGeneralRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Keyword { get; set; }

    public string Sorting { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "FullName";
        }

        Keyword = Keyword?.Trim();
    }
}
