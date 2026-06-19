using Abp.Authorization;
using Abp.Runtime.Session;
using MqSocial.Configuration.Dto;
using System.Threading.Tasks;

namespace MqSocial.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : MqSocialAppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
