using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Microsoft.EntityFrameworkCore;
using MqSocial.Common.Enum;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Kols;

[Index(nameof(AccountId), nameof(Channel), IsUnique = true)]
public class Kol : FullAuditedEntity<Guid>, IMayHaveTenant
{
    public const int MaxNameLength = 256;
    public const int MaxDescriptionLength = 5000;

    [Required]
    [StringLength(MaxNameLength)]
    public string Name { get; set; }

    [StringLength(MaxDescriptionLength)]
    public string Note { get; set; }

    [StringLength(MaxNameLength)]
    public string Link { get; set; }

    public KolCareer Career { get; set; }

    [Required]
    public ChannelType Channel { get; set; }

    public int GeneralCast { get; set; }

    public int Follow { get; set; }

    [Required]
    public string AccountId { get; set; }

    public int? TenantId { get; set; }

    public string Address { get; set; }

    public string Phone { get; set; }

}
