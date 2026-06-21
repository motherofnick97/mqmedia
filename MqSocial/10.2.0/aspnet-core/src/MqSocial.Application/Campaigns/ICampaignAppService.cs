using Abp.Application.Services;
using MqSocial.Campaigns.Dto;

namespace MqSocial.Campaigns;

public interface ICampaignAppService : IAsyncCrudAppService<CampaignDto, int, PagedCampaignRequestDto, CreateCampaignDto, CampaignDto>
{
}
