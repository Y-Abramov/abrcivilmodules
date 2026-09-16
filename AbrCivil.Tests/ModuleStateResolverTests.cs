using System.Collections.Generic;
using AbrCivil.Modules.Core;
using Xunit;

public class ModuleStateResolverTests
{
    private static ModuleEntry Entry(string name, string version, params string[] compat)
    {
        var e = new ModuleEntry { Name = name, Version = version };
        e.Compatibility.AddRange(compat);
        return e;
    }

    private static InstalledBundle Installed(string name, string version)
    {
        return new InstalledBundle { Name = name, Version = version, Directory = @"C:\x\" + name + ".bundle" };
    }

    [Fact]
    public void Not_installed_when_no_bundle_found()
    {
        var state = ModuleStateResolver.Resolve(Entry("abr-hello", "1.0.0", "2024"), new List<InstalledBundle>(), 2024);
        Assert.Equal(ModuleState.NotInstalled, state);
    }

    [Fact]
    public void Installed_when_versions_match()
    {
        var state = ModuleStateResolver.Resolve(Entry("abr-hello", "1.0.0", "2024"),
            new List<InstalledBundle> { Installed("abr-hello", "1.0.0") }, 2024);
        Assert.Equal(ModuleState.Installed, state);
    }

    [Fact]
    public void Update_available_when_catalog_newer()
    {
        var state = ModuleStateResolver.Resolve(Entry("abr-hello", "1.1.0", "2024"),
            new List<InstalledBundle> { Installed("abr-hello", "1.0.0") }, 2024);
        Assert.Equal(ModuleState.UpdateAvailable, state);
    }

    [Fact]
    public void Incompatible_wins_over_everything()
    {
        var state = ModuleStateResolver.Resolve(Entry("abr-hello", "1.1.0", "2026"),
            new List<InstalledBundle> { Installed("abr-hello", "1.0.0") }, 2024);
        Assert.Equal(ModuleState.Incompatible, state);
    }

    [Fact]
    public void Empty_compatibility_means_any_series()
    {
        var state = ModuleStateResolver.Resolve(Entry("abr-hello", "1.0.0"), new List<InstalledBundle>(), 2024);
        Assert.Equal(ModuleState.NotInstalled, state);
    }

    [Fact]
    public void Host_newer_than_catalog_is_not_blocked()
    {
        var entry = Entry("abr-hello", "1.0.0", "2015", "2016", "2017", "2018", "2019", "2020",
            "2021", "2022", "2023", "2024", "2025", "2026");

        Assert.Equal(ModuleState.NotInstalled,
            ModuleStateResolver.Resolve(entry, new List<InstalledBundle>(), 2027));
    }

    [Fact]
    public void Unknown_host_year_is_not_blocked()
    {
        var state = ModuleStateResolver.Resolve(Entry("abr-hello", "1.0.0", "2024"), new List<InstalledBundle>(), 0);
        Assert.Equal(ModuleState.NotInstalled, state);
    }

    [Fact]
    public void Year_inside_gap_of_catalog_stays_incompatible()
    {
        var state = ModuleStateResolver.Resolve(Entry("abr-hello", "1.0.0", "2021", "2024", "2025"),
            new List<InstalledBundle>(), 2022);
        Assert.Equal(ModuleState.Incompatible, state);
    }
}
