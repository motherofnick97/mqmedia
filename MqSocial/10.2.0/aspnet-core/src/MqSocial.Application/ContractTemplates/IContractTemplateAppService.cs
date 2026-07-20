using Abp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using MqSocial.ContractTemplates.Dto;
using System;
using System.Threading.Tasks;

namespace MqSocial.ContractTemplates;

public interface IContractTemplateAppService : IAsyncCrudAppService<ContractTemplateDto, Guid, PagedContractTemplateRequestDto, CreateContractTemplateDto, ContractTemplateDto>
{
    Task<UploadContractTemplateFileResultDto> UploadFileAsync([FromForm] UploadContractTemplateFileDto input);
}
