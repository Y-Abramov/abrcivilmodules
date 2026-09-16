using System;
using System.IO;
using System.Linq;
using AbrCivil.Modules.Core;
using Xunit;

public class ShadowScannerTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "abrshadow_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Finds_only_abr_bundles()
    {
        var root = TempDir();
        Directory.CreateDirectory(Path.Combine(root, "AbrCivilModules.bundle"));
        Directory.CreateDirectory(Path.Combine(root, "AbrCartogram.bundle"));
        Directory.CreateDirectory(Path.Combine(root, "SomeVendor.bundle"));
        Directory.CreateDirectory(Path.Combine(root, "AbrNotABundle"));
        File.WriteAllText(Path.Combine(root, "AbrFile.bundle"), "x");

        var found = ShadowScanner.Find(root).Select(Path.GetFileName).OrderBy(n => n).ToArray();

        Assert.Equal(new[] { "AbrCartogram.bundle", "AbrCivilModules.bundle" }, found);
    }

    [Fact]
    public void Missing_root_returns_empty()
    {
        Assert.Empty(ShadowScanner.Find(Path.Combine(Path.GetTempPath(), "no_such_" + Guid.NewGuid())));
    }

    [Fact]
    public void Remove_deletes_folders()
    {
        var root = TempDir();
        var bundle = Path.Combine(root, "AbrCivilModules.bundle");
        Directory.CreateDirectory(Path.Combine(bundle, "Contents"));
        File.WriteAllText(Path.Combine(bundle, "PackageContents.xml"), "<ApplicationPackage/>");

        var failed = ShadowScanner.Remove(new[] { bundle });

        Assert.Empty(failed);
        Assert.False(Directory.Exists(bundle));
    }
}
