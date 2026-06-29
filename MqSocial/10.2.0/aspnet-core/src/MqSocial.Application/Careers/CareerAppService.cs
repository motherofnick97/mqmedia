using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MqSocial.Careers.Dto;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MqSocial.Careers;

public class CareerAppService : AsyncCrudAppService<Career, CareerDto, Guid, PagedCareerRequestDto, CreateCareerDto, CareerDto>, ICareerAppService
{
    public CareerAppService(IRepository<Career, Guid> repository)
        : base(repository)
    {
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

    public override async Task<CareerDto> CreateAsync(CreateCareerDto input)
    {
        var existing = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.Name == input.Name);

        if (existing != null)
            throw new UserFriendlyException($"Nghề nghiệp '{input.Name}' đã tồn tại");

        return await base.CreateAsync(input);
    }
}
