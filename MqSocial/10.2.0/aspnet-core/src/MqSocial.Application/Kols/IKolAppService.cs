using Abp.Application.Services;
using MqSocial.Kols.Dto;
using System;
using System.Threading.Tasks;

namespace MqSocial.Kols;

public interface IKolAppService : IAsyncCrudAppService<KolDto, Guid, PagedKolRequestDto, CreateKolDto, KolDto>
{
}
