using Abp.Application.Services;
using MqSocial.Sessions.Dto;
using System.Threading.Tasks;

namespace MqSocial.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
