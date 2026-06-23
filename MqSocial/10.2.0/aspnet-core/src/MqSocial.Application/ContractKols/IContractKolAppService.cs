using Abp.Application.Services;
using MqSocial.ContractKols.Dto;

namespace MqSocial.ContractKols;

public interface IContractKolAppService : IAsyncCrudAppService<ContractKolDto, int, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>
{
}
