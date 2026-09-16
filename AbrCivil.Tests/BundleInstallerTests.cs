using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using AbrCivil.Modules.Core;
using Xunit;

public class BundleInstallerTests
{
    private class FakeDownloader : IFileDownloader
    {
        private readonly string _source;
        public FakeDownloader(string source) { _source = source; }

        public string Download(string url, string targetFile)
        {
            File.Copy(_source, targetFile, true);
            return targetFile;
        }
    }

    private static string MakeBundleZip(string dir, string bundleFolder, string version)
    {
        var stage = Path.Combine(dir, "stage", bundleFolder);
        Directory.CreateDirectory(Path.Combine(stage, "Contents", "2024"));
        File.WriteAllText(Path.Combine(stage, "PackageContents.xml"),
            "<ApplicationPackage SchemaVersion=\"1.0\" Name=\"abr-hello\" AppVersion=\"" + version + "\" />");
        File.WriteAllText(Path.Combine(stage, "Contents", "2024", "Abr.Civil.Hello.dll"), "версия " + version);

        var zip = Path.Combine(dir, "bundle-" + version + ".zip");
        ZipFile.CreateFromDirectory(Path.Combine(dir, "stage"), zip);
        Directory.Delete(Path.Combine(dir, "stage"), true);
        return zip;
    }

    private static string Sha256(string file)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(file))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    [Fact]
    public void Install_extracts_bundle_into_plugins_root()
    {
        var dir = NewDir();
        var zip = MakeBundleZip(dir, "AbrHello.bundle", "1.0.0");
        var plugins = Path.Combine(dir, "plugins");

        var installer = new BundleInstaller(plugins, Path.Combine(dir, "tmp"), new FakeDownloader(zip));
        installer.Install("abr-hello", "https://example/x.zip", Sha256(zip));

        var target = Path.Combine(plugins, "AbrHello.bundle");
        Assert.True(File.Exists(Path.Combine(target, "PackageContents.xml")));
        Assert.True(File.Exists(Path.Combine(target, "Contents", "2024", "Abr.Civil.Hello.dll")));

        Directory.Delete(dir, true);
    }

    [Fact]
    public void Install_aborts_on_hash_mismatch()
    {
        var dir = NewDir();
        var zip = MakeBundleZip(dir, "AbrHello.bundle", "1.0.0");
        var plugins = Path.Combine(dir, "plugins");

        var installer = new BundleInstaller(plugins, Path.Combine(dir, "tmp"), new FakeDownloader(zip));

        Assert.Throws<InvalidOperationException>(() =>
            installer.Install("abr-hello", "https://example/x.zip", "00FF00FF"));

        Assert.False(Directory.Exists(Path.Combine(plugins, "AbrHello.bundle")));
        Assert.Empty(Directory.GetFiles(Path.Combine(dir, "tmp"), "*.zip"));

        Directory.Delete(dir, true);
    }

    [Fact]
    public void Install_upgrades_existing_bundle_content()
    {
        var dir = NewDir();
        var plugins = Path.Combine(dir, "plugins");

        var v1 = MakeBundleZip(dir, "AbrHello.bundle", "1.0.0");
        new BundleInstaller(plugins, Path.Combine(dir, "tmp"), new FakeDownloader(v1))
            .Install("abr-hello", "https://example/x.zip", Sha256(v1));

        var v2 = MakeBundleZip(dir, "AbrHello.bundle", "2.0.0");
        new BundleInstaller(plugins, Path.Combine(dir, "tmp"), new FakeDownloader(v2))
            .Install("abr-hello", "https://example/x.zip", Sha256(v2));

        var dll = Path.Combine(plugins, "AbrHello.bundle", "Contents", "2024", "Abr.Civil.Hello.dll");
        Assert.Equal("версия 2.0.0", File.ReadAllText(dll));

        Directory.Delete(dir, true);
    }

    [Fact]
    public void Uninstall_removes_bundle_directory()
    {
        var dir = NewDir();
        var plugins = Path.Combine(dir, "plugins");
        var zip = MakeBundleZip(dir, "AbrHello.bundle", "1.0.0");

        var installer = new BundleInstaller(plugins, Path.Combine(dir, "tmp"), new FakeDownloader(zip));
        installer.Install("abr-hello", "https://example/x.zip", Sha256(zip));
        installer.Uninstall(Path.Combine(plugins, "AbrHello.bundle"));

        Assert.False(Directory.Exists(Path.Combine(plugins, "AbrHello.bundle")));
        Directory.Delete(dir, true);
    }

    private static string NewDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }
}
