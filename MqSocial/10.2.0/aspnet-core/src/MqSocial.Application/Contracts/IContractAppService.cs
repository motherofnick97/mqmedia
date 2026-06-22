using Abp.Application.Services;
using MqSocial.Contracts.Dto;

namespace MqSocial.Contracts;

public interface IContractAppService : IAsyncCrudAppService<ContractDto, string, PagedContractRequestDto, CreateContractDto, ContractDto>
{
}
