using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.KolGenerals.Dto;
using MqSocial.Kols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MqSocial.KolGenerals;

[AbpAuthorize(PermissionNames.Pages_KolGenerals)]
public class KolGeneralAppService : AsyncCrudAppService<KolGeneral, KolGeneralDto, Guid, PagedKolGeneralRequestDto, CreateKolGeneralDto, KolGeneralDto>, IKolGeneralAppService
{
    // KolGeneral (giống Kol) dùng chung giữa các tenant: TenantId luôn null trong DB, nên tất cả
    // thao tác đọc/ghi ở đây đều bọc CurrentUnitOfWork.SetTenantId(null) để không bị global
    // tenant-filter của ABP loại mất dữ liệu — đúng pattern đã dùng ở KolAppService.
    private readonly IRepository<Kol, Guid> _kolRepository;

    public KolGeneralAppService(IRepository<KolGeneral, Guid> repository, IRepository<Kol, Guid> kolRepository) : base(repository)
    {
        _kolRepository = kolRepository;
        CreatePermissionName = PermissionNames.Pages_KolGenerals_Create;
        UpdatePermissionName = PermissionNames.Pages_KolGenerals_Update;
        DeletePermissionName = PermissionNames.Pages_KolGenerals_Delete;
    }

    protected override IQueryable<KolGeneral> CreateFilteredQuery(PagedKolGeneralRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.FullName.Contains(input.Keyword) ||
                x.Phone.Contains(input.Keyword) ||
                x.Identity.Contains(input.Keyword));
    }

    protected override IQueryable<KolGeneral> ApplySorting(IQueryable<KolGeneral> query, PagedKolGeneralRequestDto input)
    {
        return query.OrderBy(x => x.FullName);
    }

    public override async Task<PagedResultDto<KolGeneralDto>> GetAllAsync(PagedKolGeneralRequestDto input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            var result = await base.GetAllAsync(input);

            var kolGeneralIds = result.Items.Select(x => x.Id).ToList();
            if (kolGeneralIds.Count > 0)
            {
                var kolIdsByKolGeneralId = await _kolRepository.GetAll()
                    .Where(x => x.KolGeneralId.HasValue && kolGeneralIds.Contains(x.KolGeneralId.Value))
                    .GroupBy(x => x.KolGeneralId.Value)
                    .Select(g => new { KolGeneralId = g.Key, KolIds = g.Select(x => x.Id).ToList() })
                    .ToDictionaryAsync(x => x.KolGeneralId, x => x.KolIds);

                foreach (var item in result.Items)
                {
                    item.KolIds = kolIdsByKolGeneralId.TryGetValue(item.Id, out var kolIds) ? kolIds : new List<Guid>();
                }
            }

            return result;
        }
    }

    public override async Task<KolGeneralDto> GetAsync(EntityDto<Guid> input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            var result = await base.GetAsync(input);

            result.KolIds = await _kolRepository.GetAll()
                .Where(x => x.KolGeneralId == result.Id)
                .Select(x => x.Id)
                .ToListAsync();

            return result;
        }
    }

    public override async Task<KolGeneralDto> CreateAsync(CreateKolGeneralDto input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            var result = await base.CreateAsync(input);

            if (input.KolIds != null && input.KolIds.Count > 0)
            {
                var kols = await _kolRepository.GetAll()
                    .Where(x => input.KolIds.Contains(x.Id))
                    .ToListAsync();

                foreach (var kol in kols)
                {
                    kol.KolGeneralId = result.Id;
                }
            }

            result.KolIds = input.KolIds ?? new List<Guid>();
            return result;
        }
    }

    public override async Task<KolGeneralDto> UpdateAsync(KolGeneralDto input)
    {
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            var result = await base.UpdateAsync(input);

            var newKolIds = input.KolIds ?? new List<Guid>();
            var currentlyLinked = await _kolRepository.GetAll()
                .Where(x => x.KolGeneralId == input.Id)
                .ToListAsync();

            foreach (var kol in currentlyLinked.Where(x => !newKolIds.Contains(x.Id)))
            {
                kol.KolGeneralId = null;
            }

            var toLinkIds = newKolIds.Except(currentlyLinked.Select(x => x.Id)).ToList();
            if (toLinkIds.Count > 0)
            {
                var toLink = await _kolRepository.GetAll()
                    .Where(x => toLinkIds.Contains(x.Id))
                    .ToListAsync();

                foreach (var kol in toLink)
                {
                    kol.KolGeneralId = input.Id;
                }
            }

            result.KolIds = newKolIds;
            return result;
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
