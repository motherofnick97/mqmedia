using AutoMapper;

namespace MqSocial.ContractKolReviews.Dto;

public class ContractKolReviewMapProfile : Profile
{
    public ContractKolReviewMapProfile()
    {
        CreateMap<ContractKolReview, ContractKolReviewDto>();
        CreateMap<ContractKolReviewDto, ContractKolReview>()
            .ForMember(dest => dest.ContractKol, opt => opt.Ignore());
        CreateMap<CreateContractKolReviewDto, ContractKolReview>()
            .ForMember(dest => dest.ContractKol, opt => opt.Ignore());
    }
}
