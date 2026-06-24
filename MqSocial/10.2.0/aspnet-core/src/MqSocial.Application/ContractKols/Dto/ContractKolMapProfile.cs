using AutoMapper;

namespace MqSocial.ContractKols.Dto;

public class ContractKolMapProfile : Profile
{
    public ContractKolMapProfile()
    {
        CreateMap<ContractKol, ContractKolDto>();
        CreateMap<ContractKolDto, ContractKol>();
        CreateMap<CreateContractKolDto, ContractKol>();
    }
}
