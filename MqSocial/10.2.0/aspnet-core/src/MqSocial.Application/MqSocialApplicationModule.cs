using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using MqSocial.Authorization;
using OfficeOpenXml;

namespace MqSocial;

[DependsOn(
    typeof(MqSocialCoreModule),
    typeof(AbpAutoMapperModule))]
public class MqSocialApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<MqSocialAuthorizationProvider>();
    }

    public override void Initialize()
    {
        ExcelPackage.License.SetNonCommercialPersonal("MqSocial");

        var thisAssembly = typeof(MqSocialApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }
}
