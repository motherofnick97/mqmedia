using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using MqSocial.ContractKolReviews.Dto;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace MqSocial.ContractKolReviews;

public class ContractKolReviewAppService
    : AsyncCrudAppService<ContractKolReview, ContractKolReviewDto, Guid, PagedContractKolReviewRequestDto, CreateContractKolReviewDto, ContractKolReviewDto>,
      IContractKolReviewAppService
{
    public ContractKolReviewAppService(IRepository<ContractKolReview, Guid> repository)
        : base(repository)
    {
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
