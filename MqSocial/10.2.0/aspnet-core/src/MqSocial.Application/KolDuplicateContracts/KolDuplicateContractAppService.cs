using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.KolDuplicateContracts.Dto;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace MqSocial.KolDuplicateContracts;

[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class KolDuplicateContractAppService
    : AsyncCrudAppService<KolDuplicateContract, KolDuplicateContractDto, Guid, PagedKolDuplicateContractRequestDto, CreateKolDuplicateContractDto, KolDuplicateContractDto>,
      IKolDuplicateContractAppService
{
    public KolDuplicateContractAppService(IRepository<KolDuplicateContract, Guid> repository)
        : base(repository)
    {
        CreatePermissionName = PermissionNames.Pages_ContractKols_Create;
        UpdatePermissionName = PermissionNames.Pages_ContractKols_Update;
        DeletePermissionName = PermissionNames.Pages_ContractKols_Delete;
    }

    protected override KolDuplicateContract MapToEntity(CreateKolDuplicateContractDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    protected override IQueryable<KolDuplicateContract> CreateFilteredQuery(PagedKolDuplicateContractRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(input.FirstContractId.HasValue, x => x.FirstContractId == input.FirstContractId.Value)
            .WhereIf(input.SecondContractId.HasValue, x => x.SecondContractId == input.SecondContractId.Value);
    }

    protected override IQueryable<KolDuplicateContract> ApplySorting(IQueryable<KolDuplicateContract> query, PagedKolDuplicateContractRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }
}
