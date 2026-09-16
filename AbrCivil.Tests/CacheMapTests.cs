using System;
using System.IO;
using System.Linq;
using AbrCivil.Modules.Core;
using Xunit;

public class CacheMapTests
{
    private const string Local = @"C:\Users\u\AppData\Local";

    [Fact]
    public void Library_cache_paths()
    {
        Assert.Equal(new[]
        {
            Path.Combine(Local, @"ABR\Civil\abr_catalog_cache.json"),
            Path.Combine(Local, @"ABR\Civil\tmp"),
            Path.Combine(Local, @"ABR\Civil\setup"),
        }, CacheMap.For("abr-civil-modules", Local).ToArray());
    }

    [Theory]
    [InlineData("AbrBasemap", @"Abr\Basemap\cache")]
    [InlineData("AbrDemLoader", @"Abr\DemLoader\cache")]
    [InlineData("AbrLispManager", @"ABR\LispManager\index-cache.json")]
    [InlineData("AbrCartogram", @"ABR\Civil\Cartogram\cartogram.log")]
    public void Module_cache_path(string module, string relative)
    {
        Assert.Equal(new[] { Path.Combine(Local, relative) }, CacheMap.For(module, Local).ToArray());
    }

    [Theory]
    [InlineData("AbrPlanStrip")]
    [InlineData("AbrVehiclePassage")]
    [InlineData("AbrPaving")]
    [InlineData("AbrHello")]
    [InlineData("")]
    public void Modules_without_cache_return_empty(string module)
    {
        Assert.Empty(CacheMap.For(module, Local));
    }

    [Fact]
    public void Name_match_ignores_case()
    {
        Assert.Single(CacheMap.For("abrbasemap", Local));
    }

    [Fact]
    public void Map_never_points_to_user_work()
    {
        var modules = new[] { "abr-civil-modules", "AbrBasemap", "AbrDemLoader", "AbrLispManager",
                              "AbrCartogram", "AbrPlanStrip", "AbrVehiclePassage", "AbrPaving" };
        var forbidden = new[] { "settings.json", "sources.json", "library.json", "vehicles.json",
                                "passage-settings.json", "catalog_user.json" };

        foreach (var path in modules.SelectMany(m => CacheMap.For(m, Local)))
        {
            Assert.DoesNotContain(Path.GetFileName(path), forbidden);
            Assert.DoesNotContain("presets", path, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Local + @"\", path);
        }
    }

    private static string TempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), "abrcache_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Clean_removes_cache_and_keeps_user_data()
    {
        var root = TempRoot();
        var local = Path.Combine(root, "Local");
        var roaming = Path.Combine(root, "Roaming");

        var demTiles = Path.Combine(local, @"Abr\DemLoader\cache\srtm");
        Directory.CreateDirectory(demTiles);
        File.WriteAllText(Path.Combine(demTiles, "n55e037.tif"), "x");

        var settings = Path.Combine(roaming, @"Abr\DemLoader\settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settings));
        File.WriteAllText(settings, "{}");

        var presets = Path.Combine(local, @"ABR\Civil\Cartogram\presets");
        Directory.CreateDirectory(presets);
        File.WriteAllText(Path.Combine(presets, "my.json"), "{}");
        var log = Path.Combine(local, @"ABR\Civil\Cartogram\cartogram.log");
        File.WriteAllText(log, "log");

        var failed = CacheMap.Clean(CacheMap.For("AbrDemLoader", local).Concat(CacheMap.For("AbrCartogram", local)));

        Assert.Empty(failed);
        Assert.False(Directory.Exists(Path.Combine(local, @"Abr\DemLoader\cache")));
        Assert.False(File.Exists(log));
        Assert.True(File.Exists(settings));
        Assert.True(File.Exists(Path.Combine(presets, "my.json")));

        Directory.Delete(root, true);
    }

    [Fact]
    public void Clean_missing_path_is_not_failure()
    {
        var missing = Path.Combine(Path.GetTempPath(), "abrcache_missing_" + Guid.NewGuid().ToString("N"));

        Assert.Empty(CacheMap.Clean(new[] { missing }));
    }

    [Fact]
    public void Clean_reports_locked_path()
    {
        var dir = TempRoot();
        var file = Path.Combine(dir, "index-cache.json");
        File.WriteAllText(file, "{}");

        using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var failed = CacheMap.Clean(new[] { dir });
            Assert.Equal(new[] { dir }, failed.ToArray());
        }

        Directory.Delete(dir, true);
    }
}
