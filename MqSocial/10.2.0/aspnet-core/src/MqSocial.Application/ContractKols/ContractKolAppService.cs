using Abp.Application.Services;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MqSocial.Contracts;
using MqSocial.ContractKols.Dto;
using MqSocial.ContractKolReviews;
using MqSocial.KolDuplicateContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace MqSocial.ContractKols;

//[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolAppService : AsyncCrudAppService<ContractKol, ContractKolDto, Guid, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>, IContractKolAppService
{
    private readonly IRepository<Contract, Guid> _contractRepository;
    private readonly IRepository<KolDuplicateContract, Guid> _kolDuplicateContractRepository;
    private readonly IRepository<ContractKolReview, Guid> _contractKolReviewRepository;

    public ContractKolAppService(
        IRepository<ContractKol, Guid> repository,
        IRepository<KolDuplicateContract, Guid> kolDuplicateContractRepository,
        IRepository<Contract, Guid> contractRepository,
        IRepository<ContractKolReview, Guid> contractKolReviewRepository)
        : base(repository)
    {
        _kolDuplicateContractRepository = kolDuplicateContractRepository;
        _contractRepository = contractRepository;
        _contractKolReviewRepository = contractKolReviewRepository;
    }

    protected override ContractKol MapToEntity(CreateContractKolDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
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

        return await base.CreateAsync(input);
    }

    public override async Task<ContractKolDto> UpdateAsync(ContractKolDto input)
    {
        var existing = await Repository.GetAsync(input.Id);
        var oldReviewResult = existing.ReviewResult;

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
