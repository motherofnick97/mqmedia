using AutoMapper;

namespace MqSocial.ContractKolResults.Dto;

public class ContractKolResultMapProfile : Profile
{
    public ContractKolResultMapProfile()
    {
        CreateMap<ContractKolResult, ContractKolResultDto>();
        CreateMap<ContractKolResultDto, ContractKolResult>();
        CreateMap<CreateContractKolResultDto, ContractKolResult>();
    }
}
