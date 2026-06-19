using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace MqSocial.Controllers
{
    public abstract class MqSocialControllerBase : AbpController
    {
        protected MqSocialControllerBase()
        {
            LocalizationSourceName = MqSocialConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
