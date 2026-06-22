using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Companies;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MqSocial.Campaigns;

public class Campaign : FullAuditedEntity<int>, IMayHaveTenant
{
    public const int MaxNameLength = 256;
    public const int MaxDescriptionLength = 5000;

    [Required]
    [StringLength(MaxNameLength)]
    public string Name { get; set; }

    [StringLength(MaxDescriptionLength)]
    public string Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CampaignStatus Status { get; set; }

    public int? Budget { get; set; }

    public int? TenantId { get; set; }

    public int? CompanyId { get; set; }

    [ForeignKey("CompanyId")]
    public Company Company { get; set; }
}
