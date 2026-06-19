using Abp.Modules;
using Abp.Reflection.Extensions;
using MqSocial.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MqSocial.Web.Host.Startup
{
    [DependsOn(
       typeof(MqSocialWebCoreModule))]
    public class MqSocialWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public MqSocialWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(MqSocialWebHostModule).GetAssembly());
        }
    }
}
