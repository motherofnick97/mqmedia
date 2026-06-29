using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.Companies.Dto;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MqSocial.Companies;

//[AbpAuthorize(PermissionNames.Pages_Companies)]
public class CompanyAppService : AsyncCrudAppService<Company, CompanyDto, Guid, PagedCompanyRequestDto, CreateCompanyDto, CompanyDto>, ICompanyAppService
{
    public CompanyAppService(IRepository<Company, Guid> repository)
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

    public override async Task<CompanyDto> CreateAsync(CreateCompanyDto input)
    {
        var existing = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.Name == input.Name);

        if (existing != null)
            throw new UserFriendlyException($"Công ty '{input.Name}' đã tồn tại");

        return await base.CreateAsync(input);
    }
}
