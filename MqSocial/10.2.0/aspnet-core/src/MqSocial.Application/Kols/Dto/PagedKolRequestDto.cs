using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using MqSocial.Common.Enum;
using System;

namespace MqSocial.Kols.Dto;

public class PagedKolRequestDto : PagedResultRequestDto, IShouldNormalize
{
    public string Keyword { get; set; }

    public string Sorting { get; set; }

    public Guid? CareerId { get; set; }

    public ChannelType? Channel { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "CreationTime";
        }

        Keyword = Keyword?.Trim();
    }
}
