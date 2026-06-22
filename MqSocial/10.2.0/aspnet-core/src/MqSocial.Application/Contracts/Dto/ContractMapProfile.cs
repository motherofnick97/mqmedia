using AutoMapper;
using MqSocial.Contracts;

namespace MqSocial.Contracts.Dto;

public class ContractMapProfile : Profile
{
    public ContractMapProfile()
    {
        CreateMap<Contract, ContractDto>();
        CreateMap<ContractDto, Contract>();
        CreateMap<CreateContractDto, Contract>();
    }
}
