using AutoMapper;
using MqSocial.Campaigns;

namespace MqSocial.Campaigns.Dto;

public class CampaignMapProfile : Profile
{
    public CampaignMapProfile()
    {
        CreateMap<Campaign, CampaignDto>();
        CreateMap<CampaignDto, Campaign>();
        CreateMap<CreateCampaignDto, Campaign>();
    }
}
