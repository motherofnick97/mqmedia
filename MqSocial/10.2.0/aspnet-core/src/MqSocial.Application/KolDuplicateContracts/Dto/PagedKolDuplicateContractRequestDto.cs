using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using System;

namespace MqSocial.KolDuplicateContracts.Dto;

public class PagedKolDuplicateContractRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Sorting { get; set; }

    public Guid? FirstContractId { get; set; }

    public Guid? SecondContractId { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
            Sorting = "CreationTime DESC";
    }
}
