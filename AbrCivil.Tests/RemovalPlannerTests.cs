using System.Collections.Generic;
using System.Linq;
using AbrCivil.Modules.Core;
using Xunit;

public class RemovalPlannerTests
{
    private static ModuleEntry Entry(string name, string title)
    {
        return new ModuleEntry { Name = name, Title = title, Version = "1.0.0" };
    }

    private static InstalledBundle Bundle(string folder, string name, string version = "1.0.0", bool disabled = false)
    {
        return new InstalledBundle { Name = name, Version = version, Directory = @"C:\p\" + folder, IsDisabled = disabled };
    }

    private static List<ModuleEntry> Catalog()
    {
        return new List<ModuleEntry>
        {
            Entry("abr-civil-modules", "Библиотека модулей"),
            Entry("AbrLispManager", "Диспетчер лиспов"),
            Entry("AbrBasemap", "Диспетчер карт"),
        };
    }

    [Fact]
    public void Foreign_bundles_are_hidden()
    {
        var items = RemovalPlanner.Build(new List<InstalledBundle>
        {
            Bundle("KartogrammaPlugin.bundle", "KartogrammaPlugin"),
            Bundle("Autodesk Save to Web and Mobile.bundle", "Autodesk Save to Web and Mobile"),
            Bundle("AbrBasemap.bundle", "AbrBasemap"),
        }, Catalog());

        Assert.Equal(new[] { "AbrBasemap" }, items.Select(i => i.Name).ToArray());
        Assert.Equal("Диспетчер карт", items[0].Title);
    }

    [Fact]
    public void Abr_folder_outside_catalog_is_listed_by_name()
    {
        var items = RemovalPlanner.Build(
            new List<InstalledBundle> { Bundle("AbrHello.bundle", "AbrHello", "0.1.0") }, Catalog());

        Assert.Single(items);
        Assert.Equal("AbrHello", items[0].Title);
        Assert.Equal("0.1.0", items[0].Version);
        Assert.False(items[0].IsLibrary);
    }

    [Fact]
    public void Catalog_name_in_non_abr_folder_is_listed()
    {
        var items = RemovalPlanner.Build(
            new List<InstalledBundle> { Bundle("LispManager.bundle", "abrlispmanager") }, Catalog());

        Assert.Equal("Диспетчер лиспов", items.Single().Title);
    }

    [Fact]
    public void Order_is_library_then_catalog_then_unknown_by_name()
    {
        var items = RemovalPlanner.Build(new List<InstalledBundle>
        {
            Bundle("AbrZeta.bundle", "AbrZeta"),
            Bundle("AbrBasemap.bundle", "AbrBasemap"),
            Bundle("AbrAlpha.bundle", "AbrAlpha"),
            Bundle("AbrLispManager.bundle", "AbrLispManager"),
            Bundle("AbrCivilModules.bundle", "abr-civil-modules"),
        }, Catalog());

        Assert.Equal(new[] { "abr-civil-modules", "AbrLispManager", "AbrBasemap", "AbrAlpha", "AbrZeta" },
            items.Select(i => i.Name).ToArray());
        Assert.True(items[0].IsLibrary);
        Assert.False(items[1].IsLibrary);
    }

    [Fact]
    public void Disabled_flag_and_directory_are_carried()
    {
        var item = RemovalPlanner.Build(
            new List<InstalledBundle> { Bundle("AbrBasemap.bundle", "AbrBasemap", "1.0.0", true) }, Catalog()).Single();

        Assert.True(item.IsDisabled);
        Assert.Equal(@"C:\p\AbrBasemap.bundle", item.Directory);
    }

    [Fact]
    public void Empty_catalog_still_lists_abr_bundles()
    {
        var items = RemovalPlanner.Build(new List<InstalledBundle>
        {
            Bundle("AbrBasemap.bundle", "AbrBasemap"),
            Bundle("AbrCivilModules.bundle", "abr-civil-modules"),
            Bundle("KartogrammaPlugin.bundle", "KartogrammaPlugin"),
        }, new List<ModuleEntry>());

        Assert.Equal(new[] { "abr-civil-modules", "AbrBasemap" }, items.Select(i => i.Name).ToArray());
        Assert.Equal("Библиотека модулей", items[0].Title);
        Assert.Equal("AbrBasemap", items[1].Title);
    }

    [Fact]
    public void Null_inputs_are_safe()
    {
        Assert.Empty(RemovalPlanner.Build(null, Catalog()));
        Assert.Single(RemovalPlanner.Build(
            new List<InstalledBundle> { Bundle("AbrBasemap.bundle", "AbrBasemap") }, null));
    }
}
