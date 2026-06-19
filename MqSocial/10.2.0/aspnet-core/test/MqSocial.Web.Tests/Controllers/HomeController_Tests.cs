using MqSocial.Models.TokenAuth;
using MqSocial.Web.Controllers;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace MqSocial.Web.Tests.Controllers;

public class HomeController_Tests : MqSocialWebTestBase
{
    [Fact]
    public async Task Index_Test()
    {
        await AuthenticateAsync(null, new AuthenticateModel
        {
            UserNameOrEmailAddress = "admin",
            Password = "123qwe"
        });

        //Act
        var response = await GetResponseAsStringAsync(
            GetUrl<HomeController>(nameof(HomeController.Index))
        );

        //Assert
        response.ShouldNotBeNullOrEmpty();
    }
}