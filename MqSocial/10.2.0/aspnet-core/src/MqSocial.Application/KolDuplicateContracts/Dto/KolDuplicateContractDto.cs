using Abp.Application.Services.Dto;
using System;

namespace MqSocial.KolDuplicateContracts.Dto;

public class KolDuplicateContractDto : EntityDto<Guid>
{
    public Guid FirstContractId { get; set; }

    public Guid SecondContractId { get; set; }
}
