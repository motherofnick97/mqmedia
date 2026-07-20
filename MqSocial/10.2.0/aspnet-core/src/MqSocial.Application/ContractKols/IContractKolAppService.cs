using Abp.Application.Services;
using MqSocial.ContractKols.Dto;
using System;
using System.Threading.Tasks;

namespace MqSocial.ContractKols;

public interface IContractKolAppService : IAsyncCrudAppService<ContractKolDto, Guid, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>
{
    Task SendListEmailAsync(SendContractKolsEmailDto input);
}
