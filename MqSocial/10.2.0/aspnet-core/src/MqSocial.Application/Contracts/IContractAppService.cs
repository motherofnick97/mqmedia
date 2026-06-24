using Abp.Application.Services;
using MqSocial.Contracts.Dto;
using System;

namespace MqSocial.Contracts;

public interface IContractAppService : IAsyncCrudAppService<ContractDto, Guid, PagedContractRequestDto, CreateContractDto, ContractDto>
{
}
