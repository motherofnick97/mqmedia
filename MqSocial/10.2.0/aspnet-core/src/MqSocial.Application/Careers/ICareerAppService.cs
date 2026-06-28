using Abp.Application.Services;
using MqSocial.Careers.Dto;
using System;

namespace MqSocial.Careers;

public interface ICareerAppService : IAsyncCrudAppService<CareerDto, Guid, PagedCareerRequestDto, CreateCareerDto, CareerDto>
{
}
