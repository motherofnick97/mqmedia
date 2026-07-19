using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.Careers.Dto;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MqSocial.Careers;

[AbpAuthorize(PermissionNames.Pages_Careers)]
public class CareerAppService : AsyncCrudAppService<Career, CareerDto, Guid, PagedCareerRequestDto, CreateCareerDto, CareerDto>, ICareerAppService
{
    public CareerAppService(IRepository<Career, Guid> repository)
        : base(repository)
    {
        CreatePermissionName = PermissionNames.Pages_Careers_Create;
        UpdatePermissionName = PermissionNames.Pages_Careers_Update;
        DeletePermissionName = PermissionNames.Pages_Careers_Delete;
    }

    protected override IQueryable<Career> CreateFilteredQuery(PagedCareerRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Keyword));
    }

    protected override IQueryable<Career> ApplySorting(IQueryable<Career> query, PagedCareerRequestDto input)
    {
        return query.OrderBy(x => x.Name);
    }

    public override async Task<PagedResultDto<CareerDto>> GetAllAsync(PagedCareerRequestDto input)
    {
        using (CurrentUnitOfWork.SetTenantId(null)) 
        {
            return await base.GetAllAsync(input);
        }
    }

    public override async Task<CareerDto> GetAsync(EntityDto<Guid> input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            return await base.GetAsync(input);
        }
    }

    public override async Task<CareerDto> CreateAsync(CreateCareerDto input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            var existing = await Repository.GetAll()
                .FirstOrDefaultAsync(x => x.Name == input.Name);

            if (existing != null)
                throw new UserFriendlyException($"Nghề nghiệp '{input.Name}' đã tồn tại");

            return await base.CreateAsync(input);
        }
    }

    public override async Task<CareerDto> UpdateAsync(CareerDto input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            return await base.UpdateAsync(input);
        }
    }

    public override async Task DeleteAsync(EntityDto<Guid> input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            await base.DeleteAsync(input);
        }
    }
}
