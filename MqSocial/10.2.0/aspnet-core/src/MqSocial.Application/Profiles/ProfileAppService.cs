using Microsoft.AspNetCore.Identity;
using MqSocial.Users.Dto;
using System.Threading.Tasks;

namespace MqSocial.Profiles;

// Không có [AbpAuthorize] ở đây: đây là hành động tự phục vụ trên chính tài khoản đang đăng nhập
// (xác định qua AbpSession, không nhận userId từ input), nên không nên yêu cầu quyền Pages.Users
// (quyền quản trị danh sách user) như khi đặt chung trong UserAppService.
public class ProfileAppService : MqSocialAppServiceBase, IProfileAppService
{
    public async Task<bool> ChangePassword(ChangePasswordDto input)
    {
        await UserManager.InitializeOptionsAsync(AbpSession.TenantId);

        var user = await GetCurrentUserAsync();

        if (await UserManager.CheckPasswordAsync(user, input.CurrentPassword))
        {
            CheckErrors(await UserManager.ChangePasswordAsync(user, input.NewPassword));
        }
        else
        {
            CheckErrors(IdentityResult.Failed(new IdentityError
            {
                Description = "Incorrect password."
            }));
        }

        return true;
    }
}
