using System.IO;
using System.Linq;
using AbrCivil.Modules.Core;
using Xunit;

public class InstalledScannerTests
{
    private static string MakeBundle(string root, string folder, string name, string version)
    {
        var dir = Path.Combine(root, folder);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "PackageContents.xml"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<ApplicationPackage SchemaVersion=\"1.0\" Name=\"" + name + "\" AppVersion=\"" + version + "\" />");
        return dir;
    }

    [Fact]
    public void Scan_reads_name_and_version()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        MakeBundle(root, "AbrHello.bundle", "AbrHello", "1.2.3");

        var found = InstalledScanner.Scan(root);

        Assert.Single(found);
        Assert.Equal("AbrHello", found[0].Name);
        Assert.Equal("1.2.3", found[0].Version);

        Directory.Delete(root, true);
    }

    [Fact]
    public void Scan_skips_broken_manifest_without_throwing()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        var broken = Path.Combine(root, "Broken.bundle");
        Directory.CreateDirectory(broken);
        File.WriteAllText(Path.Combine(broken, "PackageContents.xml"), "<Application");
        MakeBundle(root, "AbrHello.bundle", "AbrHello", "1.0.0");

        var found = InstalledScanner.Scan(root);

        Assert.Single(found);
        Assert.Equal("AbrHello", found[0].Name);

        Directory.Delete(root, true);
    }

    [Fact]
    public void Scan_returns_empty_when_root_missing()
    {
        Assert.Empty(InstalledScanner.Scan(Path.Combine(Path.GetTempPath(), "нет-такой-папки-" + Path.GetRandomFileName())));
    }
}
