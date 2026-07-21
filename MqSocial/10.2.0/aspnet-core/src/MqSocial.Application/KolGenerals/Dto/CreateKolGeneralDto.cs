using MqSocial.Common.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.KolGenerals.Dto;

public class CreateKolGeneralDto
{
    [Required]
    public string FullName { get; set; }

    public string Phone { get; set; }

    public string Address { get; set; }

    public DateTime Dob { get; set; }

    public string Identity { get; set; }

    public Bank Bank { get; set; }

    public string BankNumber { get; set; }

    public string BankOwner { get; set; }

    public List<Guid> KolIds { get; set; } = new();
}
