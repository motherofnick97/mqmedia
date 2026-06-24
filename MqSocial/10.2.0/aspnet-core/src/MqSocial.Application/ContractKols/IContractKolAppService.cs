using Abp.Application.Services;
using MqSocial.ContractKols.Dto;
using System;

namespace MqSocial.ContractKols;

public interface IContractKolAppService : IAsyncCrudAppService<ContractKolDto, Guid, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>
{
}
