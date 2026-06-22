using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using MqSocial.Authorization;
using MqSocial.Companies.Dto;
using System.Linq;

namespace MqSocial.Companies;

[AbpAuthorize(PermissionNames.Pages_Companies)]
public class CompanyAppService : AsyncCrudAppService<Company, CompanyDto, int, PagedCompanyRequestDto, CreateCompanyDto, CompanyDto>, ICompanyAppService
{
    public CompanyAppService(IRepository<Company, int> repository)
        : base(repository)
    {
    }

    protected override IQueryable<Company> CreateFilteredQuery(PagedCompanyRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Keyword));
    }

    protected override IQueryable<Company> ApplySorting(IQueryable<Company> query, PagedCompanyRequestDto input)
    {
        return query.OrderBy(x => x.Name);
    }
}
