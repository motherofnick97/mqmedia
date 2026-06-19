using Abp.MultiTenancy;
using MqSocial.Authorization.Users;

namespace MqSocial.MultiTenancy;

public class Tenant : AbpTenant<User>
{
    public Tenant()
    {
    }

    public Tenant(string tenancyName, string name)
        : base(tenancyName, name)
    {
    }
}
