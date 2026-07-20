using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Mvc;
using MqSocial.Authorization;
using MqSocial.ContractTemplates.Dto;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MqSocial.ContractTemplates;

[AbpAuthorize(PermissionNames.Pages_ContractTemplates)]
public class ContractTemplateAppService : AsyncCrudAppService<ContractTemplate, ContractTemplateDto, Guid, PagedContractTemplateRequestDto, CreateContractTemplateDto, ContractTemplateDto>, IContractTemplateAppService
{
    public ContractTemplateAppService(IRepository<ContractTemplate, Guid> repository) : base(repository)
    {
        CreatePermissionName = PermissionNames.Pages_ContractTemplates_Create;
        UpdatePermissionName = PermissionNames.Pages_ContractTemplates_Update;
        DeletePermissionName = PermissionNames.Pages_ContractTemplates_Delete;
    }

    protected override IQueryable<ContractTemplate> CreateFilteredQuery(PagedContractTemplateRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Keyword));
    }

    protected override IQueryable<ContractTemplate> ApplySorting(IQueryable<ContractTemplate> query, PagedContractTemplateRequestDto input)
    {
        return query.OrderBy(x => x.Name);
    }

    protected override ContractTemplate MapToEntity(CreateContractTemplateDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    public async Task<UploadContractTemplateFileResultDto> UploadFileAsync([FromForm] UploadContractTemplateFileDto input)
    {
        if (input.File == null || input.File.Length == 0)
            throw new UserFriendlyException("Vui lòng chọn file để tải lên");

        if (!await PermissionChecker.IsGrantedAsync(CreatePermissionName) &&
            !await PermissionChecker.IsGrantedAsync(UpdatePermissionName))
        {
            throw new UserFriendlyException("Bạn không có quyền tải file lên");
        }

        // Lưu cạnh thư mục chạy app (không phải wwwroot) để tồn tại ổn định qua các lần deploy
        // (quy trình scp hiện tại không xóa thư mục thừa, chỉ ghi đè file build mới).
        var storageRoot = Path.Combine(AppContext.BaseDirectory, "ContractTemplates", "Templates");
        Directory.CreateDirectory(storageRoot);

        var ext = Path.GetExtension(input.File.FileName);
        var storedFileName = Guid.NewGuid().ToString("N") + ext;
        var fullPath = Path.Combine(storageRoot, storedFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await input.File.CopyToAsync(stream);
        }

        return new UploadContractTemplateFileResultDto
        {
            FilePath = storedFileName,
            FileName = input.File.FileName,
        };
    }
}
