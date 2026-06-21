using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.Campaigns.Dto;
using System;
using System.Linq;

namespace MqSocial.Campaigns;

[AbpAuthorize(PermissionNames.Pages_Campaigns)]
public class CampaignAppService : AsyncCrudAppService<Campaign, CampaignDto, int, PagedCampaignRequestDto, CreateCampaignDto, CampaignDto>, ICampaignAppService
{
    public CampaignAppService(IRepository<Campaign, int> repository)
        : base(repository)
    {
    }

    protected override IQueryable<Campaign> CreateFilteredQuery(PagedCampaignRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Description.Contains(input.Keyword))
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value);
    }

    protected override IQueryable<Campaign> ApplySorting(IQueryable<Campaign> query, PagedCampaignRequestDto input)
    {
        return query.OrderBy(x => x.GetType().GetProperty(input.Sorting));
    }
}
