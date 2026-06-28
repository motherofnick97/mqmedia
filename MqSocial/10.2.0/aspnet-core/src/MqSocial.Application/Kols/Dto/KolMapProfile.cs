using AutoMapper;
using MqSocial.Kols;
using System.Linq;

namespace MqSocial.Kols.Dto;

public class KolMapProfile : Profile
{
    public KolMapProfile()
    {
        CreateMap<Kol, KolDto>()
            .ForMember(dest => dest.CareerIds, opt => opt.MapFrom(src =>
                src.KolCareers != null ? src.KolCareers.Select(k => k.CareerId).ToList() : new()))
            .ForMember(dest => dest.CareerNames, opt => opt.MapFrom(src =>
                src.KolCareers != null ? src.KolCareers.Where(k => k.Career != null).Select(k => k.Career.Name).ToList() : new()));

        CreateMap<KolDto, Kol>()
            .ForMember(dest => dest.KolCareers, opt => opt.Ignore());

        CreateMap<CreateKolDto, Kol>()
            .ForMember(dest => dest.KolCareers, opt => opt.Ignore());
    }
}
