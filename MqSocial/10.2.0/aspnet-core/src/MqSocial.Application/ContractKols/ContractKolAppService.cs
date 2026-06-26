using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HtmlAgilityPack;
using MqSocial.Authorization;
using MqSocial.ContractKols.Dto;
using MqSocial.Kols.Dto;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;

namespace MqSocial.ContractKols;

//[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolAppService : AsyncCrudAppService<ContractKol, ContractKolDto, Guid, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>, IContractKolAppService
{

    private readonly IHttpClientFactory _httpClientFactory;

    public ContractKolAppService(IRepository<ContractKol, Guid> repository, IHttpClientFactory httpClientFactory)
        : base(repository)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override IQueryable<ContractKol> CreateFilteredQuery(PagedContractKolRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(input.KolId.HasValue, x => x.KolId == input.KolId.Value)
            .WhereIf(input.ContractId.HasValue, x => x.ContractId == input.ContractId.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value);
    }

    protected override IQueryable<ContractKol> ApplySorting(IQueryable<ContractKol> query, PagedContractKolRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }
}
