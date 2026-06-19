using Abp.Application.Services;
using MqSocial.Authorization.Accounts.Dto;
using System.Threading.Tasks;

namespace MqSocial.Authorization.Accounts;

public interface IAccountAppService : IApplicationService
{
    Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

    Task<RegisterOutput> Register(RegisterInput input);
}
