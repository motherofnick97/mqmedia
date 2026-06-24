using Abp.Application.Services.Dto;
using System;

namespace MqSocial.ContractKols.Dto;

public class ContractKolDto : EntityDto<Guid>
{
    public int KolId { get; set; }

    public string ContractId { get; set; }

    public ContractKolStatus Status { get; set; }

    public int Cash { get; set; }

    public int Payment { get; set; }

    public int? TenantId { get; set; }
}
