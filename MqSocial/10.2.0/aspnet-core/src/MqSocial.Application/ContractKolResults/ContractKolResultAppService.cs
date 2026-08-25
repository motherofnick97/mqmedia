using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using MqSocial.Authorization;
using MqSocial.ContractKolResults.Dto;
using MqSocial.ContractKols;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MqSocial.ContractKolResults;

[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolResultAppService : AsyncCrudAppService<ContractKolResult, ContractKolResultDto, Guid, PagedContractKolResultRequestDto, CreateContractKolResultDto, ContractKolResultDto>, IContractKolResultAppService
{
    private readonly IRepository<ContractKol, Guid> _contractKolRepository;

    public ContractKolResultAppService(IRepository<ContractKolResult, Guid> repository, IRepository<ContractKol, Guid> contractKolRepository)
        : base(repository)
    {
        _contractKolRepository = contractKolRepository;
        CreatePermissionName = PermissionNames.Pages_ContractKols_Create;
        UpdatePermissionName = PermissionNames.Pages_ContractKols_Update;
        DeletePermissionName = PermissionNames.Pages_ContractKols_Delete;
    }

    protected override ContractKolResult MapToEntity(CreateContractKolResultDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    private async Task EnsureContractKolNotDoneAsync(Guid contractKolId)
    {
        var contractKol = await _contractKolRepository.GetAsync(contractKolId);
        if (contractKol.Status == ContractKolStatus.Done)
            throw new UserFriendlyException("Hợp đồng đã hoàn thành, không thể thêm/xóa kết quả.");
    }

    public override async Task<ContractKolResultDto> CreateAsync(CreateContractKolResultDto input)
    {
        await EnsureContractKolNotDoneAsync(input.ContractKolId);
        return await base.CreateAsync(input);
    }

    public override async Task DeleteAsync(EntityDto<Guid> input)
    {
        var existing = await Repository.GetAsync(input.Id);
        await EnsureContractKolNotDoneAsync(existing.ContractKolId);
        await base.DeleteAsync(input);
    }

    protected override IQueryable<ContractKolResult> CreateFilteredQuery(PagedContractKolResultRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(input.ContractKolId.HasValue, x => x.ContractKolId == input.ContractKolId.Value)
            .WhereIf(input.ChannelType.HasValue, x => x.ChannelType == input.ChannelType.Value);
    }

    protected override IQueryable<ContractKolResult> ApplySorting(IQueryable<ContractKolResult> query, PagedContractKolResultRequestDto input)
    {
        return query.OrderBy(x => x.Id);
    }
}
