using Abp.Application.Services;
using MqSocial.Kols.ContractKols.Dto;

namespace MqSocial.Kols.ContractKols;

public interface IContractKolAppService : IAsyncCrudAppService<ContractKolDto, int, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>
{
}
