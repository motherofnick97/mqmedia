using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using MqSocial.Companies.Dto;
using System;
using System.Linq;

namespace MqSocial.Companies;

//[AbpAuthorize(PermissionNames.Pages_Companies)]
public class CompanyAppService : AsyncCrudAppService<Company, CompanyDto, Guid, PagedCompanyRequestDto, CreateCompanyDto, CompanyDto>, ICompanyAppService
{
    public CompanyAppService(IRepository<Company, Guid> repository) : base(repository) { }

    protected override IQueryable<Company> CreateFilteredQuery(PagedCompanyRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Keyword));
    }

    protected override IQueryable<Company> ApplySorting(IQueryable<Company> query, PagedCompanyRequestDto input)
    {
        return query.OrderBy(x => x.Name);
    }

    protected override Company MapToEntity(CreateCompanyDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }
}
