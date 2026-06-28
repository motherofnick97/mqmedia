using AutoMapper;

namespace MqSocial.KolDuplicateContracts.Dto;

public class KolDuplicateContractMapProfile : Profile
{
    public KolDuplicateContractMapProfile()
    {
        CreateMap<KolDuplicateContract, KolDuplicateContractDto>();
        CreateMap<KolDuplicateContractDto, KolDuplicateContract>();
        CreateMap<CreateKolDuplicateContractDto, KolDuplicateContract>();
    }
}
