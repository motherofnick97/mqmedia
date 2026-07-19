using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.Campaigns.Dto;
using MqSocial.ContractKols;
using MqSocial.Contracts;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace MqSocial.Campaigns;

[AbpAuthorize(PermissionNames.Pages_Campaigns)]
public class CampaignAppService : AsyncCrudAppService<Campaign, CampaignDto, Guid, PagedCampaignRequestDto, CreateCampaignDto, CampaignDto>, ICampaignAppService
{
    private readonly IRepository<Contract, Guid> _contractRepository;
    private readonly IRepository<ContractKol, Guid> _contractKolRepository;

    public CampaignAppService(
        IRepository<Campaign, Guid> repository,
        IRepository<Contract, Guid> contractRepository,
        IRepository<ContractKol, Guid> contractKolRepository)
        : base(repository)
    {
        _contractRepository = contractRepository;
        _contractKolRepository = contractKolRepository;
        CreatePermissionName = PermissionNames.Pages_Campaigns_Create;
        UpdatePermissionName = PermissionNames.Pages_Campaigns_Update;
        DeletePermissionName = PermissionNames.Pages_Campaigns_Delete;
    }

    protected override Campaign MapToEntity(CreateCampaignDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    protected override IQueryable<Campaign> CreateFilteredQuery(PagedCampaignRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Description.Contains(input.Keyword));
    }

    protected override IQueryable<Campaign> ApplySorting(IQueryable<Campaign> query, PagedCampaignRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }

    public override async Task<CampaignDto> UpdateAsync(CampaignDto input)
    {
        var result = await base.UpdateAsync(input);

        if (input.Status == CampaignStatus.Completed)
        {
            var contracts = await _contractRepository.GetAll()
                .Where(x => x.CampaignId == input.Id)
                .ToListAsync();

            foreach (var contract in contracts)
            {
                contract.Status = ContractStatus.Complete;
                await _contractRepository.UpdateAsync(contract);

                var contractKols = await _contractKolRepository.GetAll()
                    .Where(x => x.ContractId == contract.Id)
                    .ToListAsync();

                foreach (var ck in contractKols)
                {
                    ck.Status = ContractKolStatus.Done;
                    await _contractKolRepository.UpdateAsync(ck);
                }
            }
        }

        return result;
    }
}
