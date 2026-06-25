using Abp.Application.Services.Dto;
using MqSocial.Campaigns;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Campaigns.Dto;

public class CampaignDto : EntityDto<Guid>
{
    [Required]
    [StringLength(Campaign.MaxNameLength)]
    public string Name { get; set; }

    [StringLength(Campaign.MaxDescriptionLength)]
    public string Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CampaignStatus Status { get; set; }

    public int? Budget { get; set; }

    public Guid? CompanyId { get; set; }
}
