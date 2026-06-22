using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace MqSocial.Authorization;

public class MqSocialAuthorizationProvider : AuthorizationProvider
{
    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
        context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
        context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
        context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
        context.CreatePermission(PermissionNames.Pages_Campaigns, L("Campaigns"));
        context.CreatePermission(PermissionNames.Pages_Kols, L("Kols"));
        context.CreatePermission(PermissionNames.Pages_Contracts, L("Contracts"));
        context.CreatePermission(PermissionNames.Pages_Companies, L("Companies"));
        context.CreatePermission(PermissionNames.Pages_ContractKols, L("ContractKols"));
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, MqSocialConsts.LocalizationSourceName);
    }
}
