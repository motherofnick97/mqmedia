using Abp.Application.Services.Dto;
using MqSocial.Common.Enum;
using MqSocial.Kols;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Kols.Dto;

public class KolDto : EntityDto<Guid>
{
    [Required]
    [StringLength(Kol.MaxNameLength)]
    public string Name { get; set; }

    [StringLength(Kol.MaxDescriptionLength)]
    public string Note { get; set; }

    [StringLength(Kol.MaxNameLength)]
    public string Link { get; set; }

    public DateTime? EndDate { get; set; }

    public KolCareer Career { get; set; }

    public ChannelType Channel { get; set; }

    public decimal? GeneralCast { get; set; }

    public int Follow { get; set; }
}
