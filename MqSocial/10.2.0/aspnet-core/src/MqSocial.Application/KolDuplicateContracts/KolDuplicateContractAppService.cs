using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using MqSocial.KolDuplicateContracts.Dto;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace MqSocial.KolDuplicateContracts;

public class KolDuplicateContractAppService
    : AsyncCrudAppService<KolDuplicateContract, KolDuplicateContractDto, Guid, PagedKolDuplicateContractRequestDto, CreateKolDuplicateContractDto, KolDuplicateContractDto>,
      IKolDuplicateContractAppService
{
    public KolDuplicateContractAppService(IRepository<KolDuplicateContract, Guid> repository)
        : base(repository)
    {
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
