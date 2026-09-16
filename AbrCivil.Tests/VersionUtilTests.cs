using AbrCivil.Modules.Core;
using Xunit;

public class VersionUtilTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.1", "1.0.1", false)]
    [InlineData("1.2.0", "1.10.0", true)]
    [InlineData("2.0", "1.9.9", false)]
    public void IsNewer_compares_numerically(string installed, string catalog, bool expected)
    {
        Assert.Equal(expected, VersionUtil.IsNewer(catalog, installed));
    }

    [Fact]
    public void IsNewer_treats_unparsable_installed_as_outdated()
    {
        Assert.True(VersionUtil.IsNewer("1.0.0", "мусор"));
    }

    [Fact]
    public void IsNewer_returns_false_when_catalog_version_unparsable()
    {
        Assert.False(VersionUtil.IsNewer("мусор", "1.0.0"));
    }
}
