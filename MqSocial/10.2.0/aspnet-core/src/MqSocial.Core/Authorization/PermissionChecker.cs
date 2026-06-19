using Abp.Authorization;
using MqSocial.Authorization.Roles;
using MqSocial.Authorization.Users;

namespace MqSocial.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
