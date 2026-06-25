using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractKols.Dto;

public class CreateContractKolDto
{
    [Required]
    public Guid KolId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    public ContractKolStatus Status { get; set; } = ContractKolStatus.Register;

    public int Cash { get; set; }

    public int Payment { get; set; }

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
}
