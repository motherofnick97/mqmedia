using Abp.Application.Services;
using MqSocial.Emails.Dto;
using System.Threading.Tasks;

namespace MqSocial.Emails;

public interface IEmailAppService : IApplicationService
{
    Task SendAsync(SendEmailDto input);
}
