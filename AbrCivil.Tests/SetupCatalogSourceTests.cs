using System;
using System.Collections.Generic;
using System.IO;
using AbrCivil.Modules.Core;
using Xunit;

public class SetupCatalogSourceTests
{
    private const string Json = @"{ ""modules"": [
        { ""name"": ""abr-civil-modules"", ""title"": ""Библиотека модулей"", ""version"": ""1.0.0"",
          ""bundle_url"": ""https://example/a.zip"", ""compatibility"": [""2024""] } ] }";

    private class FakeDownloader : IFileDownloader
    {
        public readonly List<string> Calls = new List<string>();
        public readonly List<string> Failing = new List<string>();
        public string Payload = Json;

        public string Download(string url, string targetFile)
        {
            Calls.Add(url);
            if (Failing.Contains(url)) throw new InvalidOperationException("down: " + url);

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
            File.WriteAllText(targetFile, Payload);
            return targetFile;
        }
    }

    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "abrsetup_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Live_catalog_wins()
    {
        var fake = new FakeDownloader();
        var sut = new SetupCatalogSource(fake, () => Json, TempDir());

        var modules = sut.Load();

        Assert.Single(modules);
        Assert.Equal(CatalogOrigin.Live, sut.Origin);
        Assert.Single(fake.Calls);
    }

    [Fact]
    public void Mirror_used_when_live_is_down()
    {
        var fake = new FakeDownloader();
        fake.Failing.Add(CatalogClient.CatalogUrl);
        var sut = new SetupCatalogSource(fake, () => Json, TempDir());

        var modules = sut.Load();

        Assert.Single(modules);
        Assert.Equal(CatalogOrigin.Mirror, sut.Origin);
        Assert.Equal(CatalogClient.CatalogBackupUrl, fake.Calls[1]);
    }

    [Fact]
    public void Embedded_used_when_both_sources_are_down()
    {
        var fake = new FakeDownloader();
        fake.Failing.Add(CatalogClient.CatalogUrl);
        fake.Failing.Add(CatalogClient.CatalogBackupUrl);
        var sut = new SetupCatalogSource(fake, () => Json, TempDir());

        var modules = sut.Load();

        Assert.Single(modules);
        Assert.Equal(CatalogOrigin.Embedded, sut.Origin);
    }

    [Fact]
    public void Broken_payload_falls_through_to_embedded()
    {
        var fake = new FakeDownloader { Payload = "<html>404</html>" };
        var sut = new SetupCatalogSource(fake, () => Json, TempDir());

        var modules = sut.Load();

        Assert.Single(modules);
        Assert.Equal(CatalogOrigin.Embedded, sut.Origin);
        Assert.Equal(2, fake.Calls.Count);
    }
}
