using Abp.Application.Services;
using MqSocial.KolGenerals.Dto;
using System;

namespace MqSocial.KolGenerals;

public interface IKolGeneralAppService : IAsyncCrudAppService<KolGeneralDto, Guid, PagedKolGeneralRequestDto, CreateKolGeneralDto, KolGeneralDto>
{
}
