using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.ContractKolReviews.Dto;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace MqSocial.ContractKolReviews;

[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolReviewAppService
    : AsyncCrudAppService<ContractKolReview, ContractKolReviewDto, Guid, PagedContractKolReviewRequestDto, CreateContractKolReviewDto, ContractKolReviewDto>,
      IContractKolReviewAppService
{
    public ContractKolReviewAppService(IRepository<ContractKolReview, Guid> repository)
        : base(repository)
    {
        CreatePermissionName = PermissionNames.Pages_ContractKols_Create;
        UpdatePermissionName = PermissionNames.Pages_ContractKols_Update;
        DeletePermissionName = PermissionNames.Pages_ContractKols_Delete;
    }

    protected override ContractKolReview MapToEntity(CreateContractKolReviewDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    protected override IQueryable<ContractKolReview> CreateFilteredQuery(PagedContractKolReviewRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(input.ContractKolId.HasValue, x => x.ContractKolId == input.ContractKolId.Value);
    }

    protected override IQueryable<ContractKolReview> ApplySorting(IQueryable<ContractKolReview> query, PagedContractKolReviewRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }
}
