using Abp.Application.Services;
using MqSocial.MultiTenancy.Dto;

namespace MqSocial.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}

