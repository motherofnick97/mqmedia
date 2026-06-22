using AutoMapper;
using MqSocial.Companies;

namespace MqSocial.Companies.Dto;

public class CompanyMapProfile : Profile
{
    public CompanyMapProfile()
    {
        CreateMap<Company, CompanyDto>();
        CreateMap<CompanyDto, Company>();
        CreateMap<CreateCompanyDto, Company>();
    }
}
