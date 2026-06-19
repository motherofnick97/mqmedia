using MqSocial.Configuration.Dto;
using System.Threading.Tasks;

namespace MqSocial.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
