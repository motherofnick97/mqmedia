using AutoMapper;
using MqSocial.KolGenerals;

namespace MqSocial.KolGenerals.Dto;

public class KolGeneralMapProfile : Profile
{
    public KolGeneralMapProfile()
    {
        CreateMap<KolGeneral, KolGeneralDto>();
        CreateMap<KolGeneralDto, KolGeneral>();
        CreateMap<CreateKolGeneralDto, KolGeneral>();
    }
}
