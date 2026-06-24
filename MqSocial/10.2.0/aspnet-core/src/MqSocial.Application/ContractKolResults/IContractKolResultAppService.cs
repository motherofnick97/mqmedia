using Abp.Application.Services;
using MqSocial.ContractKolResults.Dto;
using System;

namespace MqSocial.ContractKolResults;

public interface IContractKolResultAppService : IAsyncCrudAppService<ContractKolResultDto, Guid, PagedContractKolResultRequestDto, CreateContractKolResultDto, ContractKolResultDto>
{
}
