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
        context.CreatePermission(PermissionNames.Pages_Campaigns_Create, L("CampaignsCreate"));
        context.CreatePermission(PermissionNames.Pages_Campaigns_Update, L("CampaignsUpdate"));
        context.CreatePermission(PermissionNames.Pages_Campaigns_Delete, L("CampaignsDelete"));
        context.CreatePermission(PermissionNames.Pages_Kols, L("Kols"));
        context.CreatePermission(PermissionNames.Pages_Kols_Create, L("KolsCreate"));
        context.CreatePermission(PermissionNames.Pages_Kols_Update, L("KolsUpdate"));
        context.CreatePermission(PermissionNames.Pages_Kols_Delete, L("KolsDelete"));
        context.CreatePermission(PermissionNames.Pages_Contracts, L("Contracts"));
        context.CreatePermission(PermissionNames.Pages_Contracts_Create, L("ContractsCreate"));
        context.CreatePermission(PermissionNames.Pages_Contracts_Update, L("ContractsUpdate"));
        context.CreatePermission(PermissionNames.Pages_Contracts_Delete, L("ContractsDelete"));
        context.CreatePermission(PermissionNames.Pages_Companies, L("Companies"));
        context.CreatePermission(PermissionNames.Pages_Companies_Create, L("CompaniesCreate"));
        context.CreatePermission(PermissionNames.Pages_Companies_Update, L("CompaniesUpdate"));
        context.CreatePermission(PermissionNames.Pages_Companies_Delete, L("CompaniesDelete"));
        context.CreatePermission(PermissionNames.Pages_ContractKols, L("ContractKols"));
        context.CreatePermission(PermissionNames.Pages_ContractKols_Create, L("ContractKolsCreate"));
        context.CreatePermission(PermissionNames.Pages_ContractKols_Update, L("ContractKolsUpdate"));
        context.CreatePermission(PermissionNames.Pages_ContractKols_Delete, L("ContractKolsDelete"));
        context.CreatePermission(PermissionNames.Pages_ContractKols_Payment, L("ContractKolsPayment"));
        context.CreatePermission(PermissionNames.Pages_Careers, L("Careers"));
        context.CreatePermission(PermissionNames.Pages_Careers_Create, L("CareersCreate"));
        context.CreatePermission(PermissionNames.Pages_Careers_Update, L("CareersUpdate"));
        context.CreatePermission(PermissionNames.Pages_Careers_Delete, L("CareersDelete"));
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, MqSocialConsts.LocalizationSourceName);
    }
}
