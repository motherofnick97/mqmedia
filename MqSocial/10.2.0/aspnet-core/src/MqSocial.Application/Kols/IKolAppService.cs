using Abp.Application.Services;
using MqSocial.Kols.Dto;
using System.Threading.Tasks;

namespace MqSocial.Kols;

public interface IKolAppService : IAsyncCrudAppService<KolDto, int, PagedKolRequestDto, CreateKolDto, KolDto>
{
    Task<CreateKolDto> CrawlKolInfoByUrl(CrawlKolInfoInput input);
}
