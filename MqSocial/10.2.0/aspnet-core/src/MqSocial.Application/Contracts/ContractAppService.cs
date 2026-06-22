using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.Contracts.Dto;
using System.Linq;

namespace MqSocial.Contracts;

[AbpAuthorize(PermissionNames.Pages_Contracts)]
public class ContractAppService : AsyncCrudAppService<Contract, ContractDto, string, PagedContractRequestDto, CreateContractDto, ContractDto>, IContractAppService
{
    public ContractAppService(IRepository<Contract, string> repository)
        : base(repository)
    {
    }

    protected override IQueryable<Contract> CreateFilteredQuery(PagedContractRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Note.Contains(input.Keyword))
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value)
            .WhereIf(input.CampaignId.HasValue, x => x.Campaign.Id == input.CampaignId.Value);
    }

    protected override IQueryable<Contract> ApplySorting(IQueryable<Contract> query, PagedContractRequestDto input)
    {
        return query.OrderBy(x => x.Name);
    }
}
