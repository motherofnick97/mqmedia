using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.ContractKols.Dto;
using MqSocial.Kols;
using System.Linq;

namespace MqSocial.ContractKols;

[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolAppService : AsyncCrudAppService<ContractKol, ContractKolDto, int, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>, IContractKolAppService
{
    public ContractKolAppService(IRepository<ContractKol, int> repository)
        : base(repository)
    {
    }

    protected override IQueryable<ContractKol> CreateFilteredQuery(PagedContractKolRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(input.KolId.HasValue, x => x.KolId == input.KolId.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.ContractId), x => x.ContractId == input.ContractId)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value);
    }

    protected override IQueryable<ContractKol> ApplySorting(IQueryable<ContractKol> query, PagedContractKolRequestDto input)
    {
        return query.OrderBy(x => x.Id);
    }
}
