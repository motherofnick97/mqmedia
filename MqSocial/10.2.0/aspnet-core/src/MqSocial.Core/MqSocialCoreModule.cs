using Abp.Localization;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Security;
using Abp.Timing;
using Abp.Zero;
using Abp.Zero.Configuration;
using MqSocial.Authorization.Roles;
using MqSocial.Authorization.Users;
using MqSocial.Configuration;
using MqSocial.Localization;
using MqSocial.MultiTenancy;
using MqSocial.Timing;

namespace MqSocial;

[DependsOn(typeof(AbpZeroCoreModule))]
public class MqSocialCoreModule : AbpModule
{
    public override void PreInitialize()
    {
        // PostgreSQL's "timestamp with time zone" only accepts DateTime.Kind == Utc.
        // Abp's default clock provider (Unspecified) hands back Kind=Local, which Npgsql rejects.
        Clock.Provider = ClockProviders.Utc;

        Configuration.Auditing.IsEnabledForAnonymousUsers = true;

        // Declare entity types
        Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
        Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
        Configuration.Modules.Zero().EntityTypes.User = typeof(User);

        MqSocialLocalizationConfigurer.Configure(Configuration.Localization);

        // Enable this line to create a multi-tenant application.
        Configuration.MultiTenancy.IsEnabled = MqSocialConsts.MultiTenancyEnabled;

        // Configure roles
        AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

        Configuration.Settings.Providers.Add<AppSettingProvider>();

        Configuration.Localization.Languages.Add(new LanguageInfo("fa", "فارسی", "famfamfam-flags ir"));

        Configuration.Settings.SettingEncryptionConfiguration.DefaultPassPhrase = MqSocialConsts.DefaultPassPhrase;
        SimpleStringCipher.DefaultPassPhrase = MqSocialConsts.DefaultPassPhrase;
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MqSocialCoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;
    }
}
