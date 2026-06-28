using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using MqSocial.Careers.Dto;
using System;
using System.Linq;

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
}
