using Emaus.Api.Services.Menu;

namespace Emaus.Api.Tests.Services;

public class MenuServiceTests
{
    private readonly MenuService _sut = new();

    [Fact]
    public void GetMenu_DefaultProject_ReturnsEmausMenuWithFourTabsEndingInMenu()
    {
        var menu = _sut.GetMenu(project: null);

        Assert.Equal(4, menu.Tabs.Count);
        Assert.Equal("menu", menu.Tabs[^1].Key);
        Assert.Contains(menu.Tabs, t => t.Key == "locations");
    }

    [Theory]
    [InlineData("bob")]
    [InlineData("BOB")]
    [InlineData("Bob")]
    public void GetMenu_BobProjectCaseInsensitive_ReturnsBobMenu(string project)
    {
        var menu = _sut.GetMenu(project);

        Assert.Equal(4, menu.Tabs.Count);
        Assert.Equal("menu", menu.Tabs[^1].Key);
        Assert.Contains(menu.Tabs, t => t.Key == "bob-dashboard");
        Assert.DoesNotContain(menu.Tabs, t => t.Key == "locations");
    }

    [Fact]
    public void GetMenu_UnknownProject_FallsBackToEmausMenu()
    {
        var menu = _sut.GetMenu("something-else");

        Assert.Contains(menu.Tabs, t => t.Key == "locations");
    }
}
