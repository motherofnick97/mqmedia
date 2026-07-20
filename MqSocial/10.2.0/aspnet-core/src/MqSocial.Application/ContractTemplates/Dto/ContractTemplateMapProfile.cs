using AutoMapper;
using MqSocial.ContractTemplates;

namespace MqSocial.ContractTemplates.Dto;

public class ContractTemplateMapProfile : Profile
{
    public ContractTemplateMapProfile()
    {
        CreateMap<ContractTemplate, ContractTemplateDto>();
        CreateMap<ContractTemplateDto, ContractTemplate>();
        CreateMap<CreateContractTemplateDto, ContractTemplate>();
    }
}
