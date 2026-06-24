using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractKols.Dto;

public class CreateContractKolDto
{
    [Required]
    public int KolId { get; set; }

    [Required]
    public string ContractId { get; set; }

    public ContractKolStatus Status { get; set; } = ContractKolStatus.Register;

    public int Cash { get; set; }

    public int Payment { get; set; }
}
