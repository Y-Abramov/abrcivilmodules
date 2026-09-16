using System.Collections.Generic;
using System.Linq;
using AbrCivil.Modules.Core;
using Xunit;

public class SetupPlannerTests
{
    private static ModuleEntry Entry(string name, string version, params string[] compat)
    {
        var e = new ModuleEntry { Name = name, Title = name, Version = version, BundleUrl = "https://example/" + name + ".zip" };
        e.Compatibility.AddRange(compat);
        return e;
    }

    private static InstalledBundle Installed(string name, string version, bool disabled = false)
    {
        return new InstalledBundle { Name = name, Version = version, Directory = @"c:\p\" + name + ".bundle", IsDisabled = disabled };
    }

    [Fact]
    public void Not_installed_is_checked()
    {
        var choices = SetupPlanner.Build(
            new List<ModuleEntry> { Entry("AbrCartogram", "1.0.0", "2024") },
            new List<InstalledBundle>(), 2024);

        Assert.True(choices[0].Checked);
        Assert.False(choices[0].Locked);
        Assert.Equal("1.0.0", choices[0].StatusText);
    }

    [Fact]
    public void Update_available_is_checked_with_both_versions()
    {
        var choices = SetupPlanner.Build(
            new List<ModuleEntry> { Entry("AbrCartogram", "1.1.0", "2024") },
            new List<InstalledBundle> { Installed("AbrCartogram", "1.0.0") }, 2024);

        Assert.True(choices[0].Checked);
        Assert.Equal("установлен 1.0.0 > 1.1.0", choices[0].StatusText);
    }

    [Fact]
    public void Up_to_date_is_unchecked()
    {
        var choices = SetupPlanner.Build(
            new List<ModuleEntry> { Entry("AbrCartogram", "1.0.0", "2024") },
            new List<InstalledBundle> { Installed("AbrCartogram", "1.0.0") }, 2024);

        Assert.False(choices[0].Checked);
        Assert.Equal("установлен 1.0.0", choices[0].StatusText);
    }

    [Fact]
    public void Disabled_module_is_unchecked_but_library_is_reenabled()
    {
        var choices = SetupPlanner.Build(
            new List<ModuleEntry>
            {
                Entry("AbrCartogram", "1.0.0", "2024"),
                Entry(SetupPlanner.LibraryName, "1.0.0", "2024")
            },
            new List<InstalledBundle>
            {
                Installed("AbrCartogram", "1.0.0", disabled: true),
                Installed(SetupPlanner.LibraryName, "1.0.0", disabled: true)
            }, 2024);

        var library = choices.First(c => c.IsLibrary);
        var module  = choices.First(c => !c.IsLibrary);

        Assert.False(module.Checked);
        Assert.Equal("отключён", module.StatusText);
        Assert.True(library.Checked);
        Assert.Equal("отключён, будет включён", library.StatusText);
    }

    [Fact]
    public void Incompatible_is_locked_and_unchecked()
    {
        var choices = SetupPlanner.Build(
            new List<ModuleEntry> { Entry("AbrCartogram", "1.0.0", "2026") },
            new List<InstalledBundle>(), 2024);

        Assert.False(choices[0].Checked);
        Assert.True(choices[0].Locked);
        Assert.Equal("не для Civil 3D 2024", choices[0].StatusText);
    }

    [Fact]
    public void Library_goes_first_and_is_always_locked()
    {
        var choices = SetupPlanner.Build(
            new List<ModuleEntry>
            {
                Entry("AbrCartogram", "1.0.0", "2024"),
                Entry(SetupPlanner.LibraryName, "1.0.0", "2024")
            },
            new List<InstalledBundle>(), 2024);

        Assert.True(choices[0].IsLibrary);
        Assert.True(choices[0].Locked);
        Assert.True(choices[0].Checked);
    }

    [Fact]
    public void Without_civil3d_compatibility_is_not_checked()
    {
        var choices = SetupPlanner.Build(
            new List<ModuleEntry> { Entry("AbrCartogram", "1.0.0", "2026") },
            new List<InstalledBundle>(), 0);

        Assert.True(choices[0].Checked);
        Assert.False(choices[0].Locked);
    }
}
