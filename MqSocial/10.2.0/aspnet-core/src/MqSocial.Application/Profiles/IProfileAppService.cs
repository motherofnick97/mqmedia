using Abp.Application.Services;
using MqSocial.Users.Dto;
using System.Threading.Tasks;

namespace MqSocial.Profiles;

public interface IProfileAppService : IApplicationService
{
    Task<bool> ChangePassword(ChangePasswordDto input);
}
