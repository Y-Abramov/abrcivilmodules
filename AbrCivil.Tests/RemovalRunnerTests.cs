using System;
using System.IO;
using AbrCivil.Modules.Core;
using Xunit;

public class RemovalRunnerTests
{
    private static string NewRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), "abrremove_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static RemovalItem MakeBundle(string root, string folder, string name, bool disabled)
    {
        var dir = Path.Combine(root, "plugins", folder);
        var contents = Path.Combine(dir, "Contents", "2024");
        Directory.CreateDirectory(contents);
        File.WriteAllText(Path.Combine(contents, name + ".dll"), "dll");
        File.WriteAllText(Path.Combine(dir, disabled ? "PackageContents.xml.disabled" : "PackageContents.xml"),
            "<ApplicationPackage Name=\"" + name + "\" AppVersion=\"1.0.0\" />");
        return new RemovalItem { Name = name, Title = name, Version = "1.0.0", Directory = dir, IsDisabled = disabled };
    }

    private static string MakeBasemapCache(string root)
    {
        var cache = Path.Combine(root, "Local", @"Abr\Basemap\cache");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "tile.png"), "x");
        return cache;
    }

    private static BundleInstaller Installer(string root)
    {
        // Uninstall nothing downloads - downloader is not needed.
        return new BundleInstaller(Path.Combine(root, "plugins"), Path.Combine(root, "tmp"), null);
    }

    private static string Local(string root) { return Path.Combine(root, "Local"); }

    [Fact]
    public void Removes_enabled_bundle()
    {
        var root = NewRoot();
        var item = MakeBundle(root, "AbrPaving.bundle", "AbrPaving", false);

        var result = RemovalRunner.Remove(item, false, Installer(root), Local(root));

        Assert.Equal(RemovalOutcome.Removed, result.Outcome);
        Assert.False(Directory.Exists(item.Directory));
        Directory.Delete(root, true);
    }

    [Fact]
    public void Removes_disabled_bundle()
    {
        var root = NewRoot();
        var item = MakeBundle(root, "AbrPaving.bundle", "AbrPaving", true);

        var result = RemovalRunner.Remove(item, false, Installer(root), Local(root));

        Assert.Equal(RemovalOutcome.Removed, result.Outcome);
        Assert.False(Directory.Exists(item.Directory));
        Directory.Delete(root, true);
    }

    [Fact]
    public void Keeps_cache_when_not_requested()
    {
        var root = NewRoot();
        var cache = MakeBasemapCache(root);
        var item = MakeBundle(root, "AbrBasemap.bundle", "AbrBasemap", false);

        var result = RemovalRunner.Remove(item, false, Installer(root), Local(root));

        Assert.Equal(RemovalOutcome.Removed, result.Outcome);
        Assert.True(File.Exists(Path.Combine(cache, "tile.png")));
        Directory.Delete(root, true);
    }

    [Fact]
    public void Cleans_cache_when_requested()
    {
        var root = NewRoot();
        var cache = MakeBasemapCache(root);
        var item = MakeBundle(root, "AbrBasemap.bundle", "AbrBasemap", false);

        var result = RemovalRunner.Remove(item, true, Installer(root), Local(root));

        Assert.Equal(RemovalOutcome.Removed, result.Outcome);
        Assert.False(Directory.Exists(cache));
        Directory.Delete(root, true);
    }

    [Fact]
    public void Locked_cache_file_gives_leftovers_with_path()
    {
        var root = NewRoot();
        var cache = MakeBasemapCache(root);
        var item = MakeBundle(root, "AbrBasemap.bundle", "AbrBasemap", false);

        RemovalResult result;
        using (new FileStream(Path.Combine(cache, "tile.png"), FileMode.Open, FileAccess.Read, FileShare.None))
            result = RemovalRunner.Remove(item, true, Installer(root), Local(root));

        Assert.Equal(RemovalOutcome.RemovedWithLeftovers, result.Outcome);
        Assert.Equal(cache, result.Detail);
        Assert.False(Directory.Exists(item.Directory));
        Directory.Delete(root, true);
    }

    [Fact]
    public void Locked_dll_leaves_folder_without_manifest()
    {
        var root = NewRoot();
        var item = MakeBundle(root, "AbrPaving.bundle", "AbrPaving", false);
        var dll = Path.Combine(item.Directory, "Contents", "2024", "AbrPaving.dll");

        RemovalResult result;
        using (new FileStream(dll, FileMode.Open, FileAccess.Read, FileShare.None))
            result = RemovalRunner.Remove(item, false, Installer(root), Local(root));

        Assert.Equal(RemovalOutcome.RemovedWithLeftovers, result.Outcome);
        Assert.Equal(item.Directory, result.Detail);
        Assert.False(File.Exists(Path.Combine(item.Directory, "PackageContents.xml")));
        Directory.Delete(root, true);
    }

    [Fact]
    public void Locked_manifest_is_failure()
    {
        var root = NewRoot();
        var item = MakeBundle(root, "AbrPaving.bundle", "AbrPaving", false);
        var manifest = Path.Combine(item.Directory, "PackageContents.xml");

        RemovalResult result;
        using (new FileStream(manifest, FileMode.Open, FileAccess.Read, FileShare.None))
            result = RemovalRunner.Remove(item, false, Installer(root), Local(root));

        Assert.Equal(RemovalOutcome.Failed, result.Outcome);
        Assert.StartsWith("Файл занят", result.Detail);
        Assert.True(Directory.Exists(item.Directory));
        Directory.Delete(root, true);
    }
}
