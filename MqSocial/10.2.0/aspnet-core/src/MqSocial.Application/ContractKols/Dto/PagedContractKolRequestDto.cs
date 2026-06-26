using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using System;

namespace MqSocial.ContractKols.Dto;

public class PagedContractKolRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Sorting { get; set; }

    public Guid? KolId { get; set; }

    public Guid? ContractId { get; set; }

    public ContractKolStatus? Status { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "Id";
        }
    }
}
