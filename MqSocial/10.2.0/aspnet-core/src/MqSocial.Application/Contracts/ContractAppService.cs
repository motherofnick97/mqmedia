using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.ContractKols;
using MqSocial.Contracts.Dto;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace MqSocial.Contracts;

[AbpAuthorize(PermissionNames.Pages_Contracts)]
public class ContractAppService : AsyncCrudAppService<Contract, ContractDto, Guid, PagedContractRequestDto, CreateContractDto, ContractDto>, IContractAppService
{
    private readonly IRepository<ContractKol, Guid> _contractKolRepository;

    public ContractAppService(
        IRepository<Contract, Guid> repository,
        IRepository<ContractKol, Guid> contractKolRepository)
        : base(repository)
    {
        _contractKolRepository = contractKolRepository;
        CreatePermissionName = PermissionNames.Pages_Contracts_Create;
        UpdatePermissionName = PermissionNames.Pages_Contracts_Update;
        DeletePermissionName = PermissionNames.Pages_Contracts_Delete;
    }

    protected override Contract MapToEntity(CreateContractDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    protected override IQueryable<Contract> CreateFilteredQuery(PagedContractRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Note.Contains(input.Keyword))
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value);
    }

    protected override IQueryable<Contract> ApplySorting(IQueryable<Contract> query, PagedContractRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }

    public override async Task<ContractDto> UpdateAsync(ContractDto input)
    {
        var existing = await Repository.GetAsync(input.Id);
        if (existing.Status == ContractStatus.Complete)
            throw new UserFriendlyException("Hợp đồng đã hoàn thành, không thể chỉnh sửa.");

        var result = await base.UpdateAsync(input);

        if (input.Status == ContractStatus.Complete)
        {
            var contractKols = await _contractKolRepository.GetAll()
                .Where(x => x.ContractId == input.Id)
                .ToListAsync();

            foreach (var ck in contractKols)
            {
                ck.Status = ContractKolStatus.Done;
                await _contractKolRepository.UpdateAsync(ck);
            }
        }

        return result;
    }
}
