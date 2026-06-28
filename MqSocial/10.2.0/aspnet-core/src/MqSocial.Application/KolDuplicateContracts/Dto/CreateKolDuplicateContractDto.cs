using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.KolDuplicateContracts.Dto;

public class CreateKolDuplicateContractDto
{
    [Required]
    public Guid FirstContractId { get; set; }

    [Required]
    public Guid SecondContractId { get; set; }
}
