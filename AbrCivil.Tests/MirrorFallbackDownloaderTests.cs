using System;
using System.Collections.Generic;
using AbrCivil.Modules.Core;
using Xunit;

public class MirrorFallbackDownloaderTests
{
    private const string GitHubUrl =
        "https://github.com/Y-Abramov/abrcartogram/releases/download/v1.0.0/AbrCartogram-1.0.0.zip";
    private const string MirrorUrl =
        "https://storage.yandexcloud.net/abrmove-modules/bundle/AbrCartogram-1.0.0.zip";

    private class FakeDownloader : IFileDownloader
    {
        public readonly List<string> Calls = new List<string>();
        public readonly List<string> Failing = new List<string>();
        public bool FailAll;

        public string Download(string url, string targetFile)
        {
            Calls.Add(url);
            if (FailAll || Failing.Contains(url))
                throw new InvalidOperationException("network down: " + url);
            return targetFile;
        }
    }

    [Fact]
    public void Primary_success_does_not_touch_mirror()
    {
        var fake = new FakeDownloader();
        var sut = new MirrorFallbackDownloader(fake);

        sut.Download(GitHubUrl, "c:\\tmp\\a.zip");

        Assert.Single(fake.Calls);
        Assert.Equal(GitHubUrl, fake.Calls[0]);
        Assert.False(sut.LastDownloadUsedMirror);
    }

    [Fact]
    public void Primary_failure_falls_back_to_mirror_by_file_name()
    {
        var fake = new FakeDownloader();
        fake.Failing.Add(GitHubUrl);
        var sut = new MirrorFallbackDownloader(fake);

        sut.Download(GitHubUrl, "c:\\tmp\\a.zip");

        Assert.Equal(2, fake.Calls.Count);
        Assert.Equal(MirrorUrl, fake.Calls[1]);
        Assert.True(sut.LastDownloadUsedMirror);
    }

    [Fact]
    public void Both_sources_down_throws()
    {
        var fake = new FakeDownloader { FailAll = true };
        var sut = new MirrorFallbackDownloader(fake);

        Assert.Throws<InvalidOperationException>(() => sut.Download(GitHubUrl, "c:\\tmp\\a.zip"));
        Assert.Equal(2, fake.Calls.Count);
    }

    [Fact]
    public void Query_and_fragment_are_stripped_from_file_name()
    {
        var sut = new MirrorFallbackDownloader(new FakeDownloader());

        Assert.Equal(MirrorUrl, sut.MirrorUrl(GitHubUrl + "?raw=true#top"));
    }
}
