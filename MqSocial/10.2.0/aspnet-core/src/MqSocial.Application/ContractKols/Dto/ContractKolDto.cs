using Abp.Application.Services.Dto;
using MqSocial.ContractKolResults.Dto;
using System;
using System.Collections.Generic;

namespace MqSocial.ContractKols.Dto;

public class ContractKolDto : EntityDto<Guid>
{
    public Guid KolId { get; set; }

    public string KolName { get; set; }

    public Guid? KolGeneralId { get; set; }

    public Guid ContractId { get; set; }

    public string ContractName { get; set; }

    public ContractKolStatus Status { get; set; }

    public int Cash { get; set; }

    public int Payment { get; set; }

    public int? TenantId { get; set; }

    public string Portrait { get; set; }

    public string ReviewCorner { get; set; }

    public string SampleSize { get; set; }

    public string SampleName { get; set; }

    public int SampleQuantity { get; set; }

    public ReceiveStatus SampleReceiveStatus { get; set; }

    public DateTime? AirTime { get; set; }

    public string Brief { get; set; }

    public string BriefLink { get; set; }

    public string Feedback { get; set; }

    public string Caption { get; set; }

    public string HashTag { get; set; }

    public string ReviewResult { get; set; }

    public List<ContractKolResultDto> Results { get; set; } = new();
}
