using MqSocial.Campaigns;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Campaigns.Dto;

public class CreateCampaignDto
{
    [Required]
    [StringLength(Campaign.MaxNameLength)]
    public string Name { get; set; }

    [StringLength(Campaign.MaxDescriptionLength)]
    public string Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public int? Budget { get; set; }
    public Guid CompanyId { get; set; }
}
