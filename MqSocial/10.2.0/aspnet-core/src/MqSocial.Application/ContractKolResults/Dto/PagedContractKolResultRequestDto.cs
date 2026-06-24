using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using MqSocial.Common.Enum;
using System;

namespace MqSocial.ContractKolResults.Dto;

public class PagedContractKolResultRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Sorting { get; set; }

    public Guid? ContractKolId { get; set; }

    public ChannelType? ChannelType { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "Id";
        }
    }
}
