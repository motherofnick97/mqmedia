using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using MqSocial.ContractKolResults.Dto;
using System;
using System.Linq;

namespace MqSocial.ContractKolResults;

//[AbpAuthorize(PermissionNames.Pages_ContractKolResults)]
public class ContractKolResultAppService : AsyncCrudAppService<ContractKolResult, ContractKolResultDto, Guid, PagedContractKolResultRequestDto, CreateContractKolResultDto, ContractKolResultDto>, IContractKolResultAppService
{
    public ContractKolResultAppService(IRepository<ContractKolResult, Guid> repository)
        : base(repository)
    {
    }

    protected override IQueryable<ContractKolResult> CreateFilteredQuery(PagedContractKolResultRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(input.ContractKolId.HasValue, x => x.ContractKolId == input.ContractKolId.Value)
            .WhereIf(input.ChannelType.HasValue, x => x.ChannelType == input.ChannelType.Value);
    }

    protected override IQueryable<ContractKolResult> ApplySorting(IQueryable<ContractKolResult> query, PagedContractKolResultRequestDto input)
    {
        return query.OrderBy(x => x.Id);
    }
}
