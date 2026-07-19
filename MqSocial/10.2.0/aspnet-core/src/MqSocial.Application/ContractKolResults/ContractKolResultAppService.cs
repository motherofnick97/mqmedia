using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.ContractKolResults.Dto;
using System;
using System.Linq;

namespace MqSocial.ContractKolResults;

[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolResultAppService : AsyncCrudAppService<ContractKolResult, ContractKolResultDto, Guid, PagedContractKolResultRequestDto, CreateContractKolResultDto, ContractKolResultDto>, IContractKolResultAppService
{
    public ContractKolResultAppService(IRepository<ContractKolResult, Guid> repository)
        : base(repository)
    {
        CreatePermissionName = PermissionNames.Pages_ContractKols_Create;
        UpdatePermissionName = PermissionNames.Pages_ContractKols_Update;
        DeletePermissionName = PermissionNames.Pages_ContractKols_Delete;
    }

    protected override ContractKolResult MapToEntity(CreateContractKolResultDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
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
