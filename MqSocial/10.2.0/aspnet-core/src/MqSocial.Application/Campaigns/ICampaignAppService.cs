using Abp.Application.Services;
using MqSocial.Campaigns.Dto;
using System;

namespace MqSocial.Campaigns;

public interface ICampaignAppService : IAsyncCrudAppService<CampaignDto, Guid, PagedCampaignRequestDto, CreateCampaignDto, CampaignDto>
{
}
