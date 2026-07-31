using AutoMapper;

namespace MqSocial.ContractKols.Dto;

public class ContractKolMapProfile : Profile
{
    public ContractKolMapProfile()
    {
        // Results được nạp thủ công trong ContractKolAppService.GetAllAsync (không có navigation
        // property tương ứng trên entity), bỏ qua ở đây để AutoMapper không ghi đè về rỗng.
        CreateMap<ContractKol, ContractKolDto>()
            .ForMember(dest => dest.Results, opt => opt.Ignore());
        CreateMap<ContractKolDto, ContractKol>();
        CreateMap<CreateContractKolDto, ContractKol>();
    }
}
