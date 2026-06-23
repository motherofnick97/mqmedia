using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using MqSocial.Kols;

namespace MqSocial.ContractKols.Dto;

public class PagedContractKolRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Sorting { get; set; }

    public int? KolId { get; set; }

    public string ContractId { get; set; }

    public ContractKolStatus? Status { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "Id";
        }
    }
}
