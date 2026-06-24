using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.Contracts.Dto;
using System;
using System.Linq;

namespace MqSocial.Contracts;

//[AbpAuthorize(PermissionNames.Pages_Contracts)]
public class ContractAppService : AsyncCrudAppService<Contract, ContractDto, Guid, PagedContractRequestDto, CreateContractDto, ContractDto>, IContractAppService
{
    public ContractAppService(IRepository<Contract, Guid> repository)
        : base(repository)
    {
    }

    protected override IQueryable<Contract> CreateFilteredQuery(PagedContractRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Note.Contains(input.Keyword))
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value);
    }

    protected override IQueryable<Contract> ApplySorting(IQueryable<Contract> query, PagedContractRequestDto input)
    {
        return query.OrderBy(x => x.Name);
    }
}
