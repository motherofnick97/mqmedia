using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.Contracts;
using MqSocial.ContractKols.Dto;
using MqSocial.ContractKolReviews;
using MqSocial.KolDuplicateContracts;
using MqSocial.Kols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace MqSocial.ContractKols;

[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolAppService : AsyncCrudAppService<ContractKol, ContractKolDto, Guid, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>, IContractKolAppService
{
    private readonly IRepository<Contract, Guid> _contractRepository;
    private readonly IRepository<KolDuplicateContract, Guid> _kolDuplicateContractRepository;
    private readonly IRepository<ContractKolReview, Guid> _contractKolReviewRepository;
    private readonly IRepository<Kol, Guid> _kolRepository;

    public ContractKolAppService(
        IRepository<ContractKol, Guid> repository,
        IRepository<KolDuplicateContract, Guid> kolDuplicateContractRepository,
        IRepository<Contract, Guid> contractRepository,
        IRepository<ContractKolReview, Guid> contractKolReviewRepository,
        IRepository<Kol, Guid> kolRepository)
        : base(repository)
    {
        _kolDuplicateContractRepository = kolDuplicateContractRepository;
        _contractRepository = contractRepository;
        _contractKolReviewRepository = contractKolReviewRepository;
        _kolRepository = kolRepository;
        CreatePermissionName = PermissionNames.Pages_ContractKols_Create;
        UpdatePermissionName = PermissionNames.Pages_ContractKols_Update;
        DeletePermissionName = PermissionNames.Pages_ContractKols_Delete;
    }

    protected override ContractKol MapToEntity(CreateContractKolDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    protected override IQueryable<ContractKol> CreateFilteredQuery(PagedContractKolRequestDto input)
    {
        // Không Include(x => x.Kol) ở đây: Kol là entity dùng chung giữa các tenant (TenantId luôn null,
        // xem KolAppService dùng CurrentUnitOfWork.SetTenantId(null) khi truy vấn Kol). Vì KolId là khóa
        // ngoại bắt buộc, EF Core sẽ áp global tenant-filter của Kol lên cả navigation Include và loại
        // luôn dòng ContractKol nếu Kol không khớp tenant hiện tại — làm mất hết kết quả tìm kiếm.
        // KolName được nạp riêng (bỏ qua tenant filter) trong GetAllAsync bên dưới.
        return Repository.GetAll()
            .Include(x => x.Contract)
            .WhereIf(input.KolId.HasValue, x => x.KolId == input.KolId.Value)
            .WhereIf(input.ContractId.HasValue, x => x.ContractId == input.ContractId.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value);
    }

    protected override IQueryable<ContractKol> ApplySorting(IQueryable<ContractKol> query, PagedContractKolRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }

    public override async Task<PagedResultDto<ContractKolDto>> GetAllAsync(PagedContractKolRequestDto input)
    {
        var result = await base.GetAllAsync(input);

        var kolIds = result.Items.Select(x => x.KolId).Distinct().ToList();
        if (kolIds.Count > 0)
        {
            Dictionary<Guid, string> kolNames;
            using (CurrentUnitOfWork.SetTenantId(null))
            {
                kolNames = await _kolRepository.GetAll()
                    .Where(x => kolIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name);
            }

            foreach (var item in result.Items)
            {
                item.KolName = kolNames.TryGetValue(item.KolId, out var name) ? name : null;
            }
        }

        if (!await IsPaymentGrantedAsync())
        {
            foreach (var item in result.Items)
            {
                item.Payment = 0;
            }
        }

        return result;
    }

    public override async Task<ContractKolDto> GetAsync(EntityDto<Guid> input)
    {
        var result = await base.GetAsync(input);

        if (!await IsPaymentGrantedAsync())
        {
            result.Payment = 0;
        }

        return result;
    }

    private Task<bool> IsPaymentGrantedAsync()
    {
        return PermissionChecker.IsGrantedAsync(PermissionNames.Pages_ContractKols_Payment);
    }

    public override async Task<ContractKolDto> CreateAsync(CreateContractKolDto input)
    {
        var existing = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.ContractId == input.ContractId && x.KolId == input.KolId);

        if (existing != null)
            throw new UserFriendlyException($"KOL đã được thêm vào hợp đồng trước đó");

        var conflictingContracts = await GetConflictingContractNamesAsync(input.ContractId, input.KolId);
        if (conflictingContracts.Count > 0)
        {
            var names = string.Join(", ", conflictingContracts);
            throw new UserFriendlyException($"KOL đã nằm trong hợp đồng không được phép trùng: {names}");
        }

        if (!await IsPaymentGrantedAsync())
        {
            input.Payment = 0;
        }

        return await base.CreateAsync(input);
    }

    public override async Task<ContractKolDto> UpdateAsync(ContractKolDto input)
    {
        var existing = await Repository.GetAsync(input.Id);
        var oldReviewResult = existing.ReviewResult;

        if (!await IsPaymentGrantedAsync())
        {
            input.Payment = existing.Payment;
        }

        var isNewReview = !string.IsNullOrWhiteSpace(input.ReviewResult) && input.ReviewResult != oldReviewResult;

        if (isNewReview)
        {
            var contract = await _contractRepository.GetAsync(existing.ContractId);
            var reviewCount = await _contractKolReviewRepository.CountAsync(x => x.ContractKolId == input.Id);

            if (reviewCount >= contract.MaxReviewTime)
                throw new UserFriendlyException($"KOL này đã đạt giới hạn {contract.MaxReviewTime} lần review của hợp đồng.");
        }

        var result = await base.UpdateAsync(input);

        if (isNewReview)
        {
            await _contractKolReviewRepository.InsertAsync(new ContractKolReview
            {
                ContractKolId = input.Id,
                Review = input.ReviewResult,
                TenantId = AbpSession.TenantId,
            });
        }

        return result;
    }

    private async Task<List<string>> GetConflictingContractNamesAsync(Guid contractId, Guid kolId)
    {
        var duplicateContractIds = await _kolDuplicateContractRepository.GetAll()
            .Where(x => x.FirstContractId == contractId || x.SecondContractId == contractId)
            .Select(x => x.FirstContractId == contractId ? x.SecondContractId : x.FirstContractId)
            .ToListAsync();

        if (!duplicateContractIds.Any())
            return new List<string>();

        var conflictingContractIds = await Repository.GetAll()
            .Where(x => x.KolId == kolId && duplicateContractIds.Contains(x.ContractId))
            .Select(x => x.ContractId)
            .Distinct()
            .ToListAsync();

        if (!conflictingContractIds.Any())
            return new List<string>();

        return await _contractRepository.GetAll()
            .Where(x => conflictingContractIds.Contains(x.Id))
            .Select(x => x.Name)
            .ToListAsync();
    }
}
