using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using MqSocial.Configuration;
using MqSocial.EntityFrameworkCore;
using MqSocial.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace MqSocial.Migrator;

[DependsOn(typeof(MqSocialEntityFrameworkModule))]
public class MqSocialMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public MqSocialMigratorModule(MqSocialEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(MqSocialMigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            MqSocialConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MqSocialMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
