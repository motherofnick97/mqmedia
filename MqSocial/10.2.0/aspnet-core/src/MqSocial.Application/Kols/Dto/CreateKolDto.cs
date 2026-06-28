using MqSocial.Common.Enum;
using MqSocial.Kols;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Kols.Dto;

public class CreateKolDto
{
    [Required]
    [StringLength(Kol.MaxNameLength)]
    public string Name { get; set; }

    [StringLength(Kol.MaxDescriptionLength)]
    public string Note { get; set; }

    [StringLength(Kol.MaxNameLength)]
    public string Link { get; set; }

    public List<Guid> CareerIds { get; set; } = new();

    public ChannelType Channel { get; set; }

    public decimal? GeneralCast { get; set; }

    public int Follow { get; set; }

    public string AccountId { get; set; }

    public string Address { get; set; }

    public string Phone { get; set; }


}
