using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using MqSocial.EntityFrameworkCore;
using MqSocial.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace MqSocial.Web.Tests;

[DependsOn(
    typeof(MqSocialWebMvcModule),
    typeof(AbpAspNetCoreTestBaseModule)
)]
public class MqSocialWebTestModule : AbpModule
{
    public MqSocialWebTestModule(MqSocialEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
    }

    public override void PreInitialize()
    {
        Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(MqSocialWebTestModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<ApplicationPartManager>()
            .AddApplicationPartsIfNotAddedBefore(typeof(MqSocialWebMvcModule).Assembly);
    }
}