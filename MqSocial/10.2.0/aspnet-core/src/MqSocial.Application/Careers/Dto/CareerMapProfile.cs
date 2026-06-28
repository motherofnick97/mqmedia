using AutoMapper;

namespace MqSocial.Careers.Dto;

public class CareerMapProfile : Profile
{
    public CareerMapProfile()
    {
        CreateMap<Career, CareerDto>();
        CreateMap<CareerDto, Career>();
        CreateMap<CreateCareerDto, Career>();
    }
}
