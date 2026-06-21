using AutoMapper;
using MqSocial.Kols;

namespace MqSocial.Kols.Dto;

public class KolMapProfile : Profile
{
    public KolMapProfile()
    {
        CreateMap<Kol, KolDto>();
        CreateMap<KolDto, Kol>();
        CreateMap<CreateKolDto, Kol>();
    }
}
