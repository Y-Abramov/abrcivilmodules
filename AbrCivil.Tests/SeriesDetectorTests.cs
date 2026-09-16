using System;
using AbrCivil.Modules.Core;
using Xunit;

public class SeriesDetectorTests
{
    [Theory]
    [InlineData(20, 0, 2015)]
    [InlineData(20, 1, 2016)]
    [InlineData(21, 0, 2017)]
    [InlineData(22, 0, 2018)]
    [InlineData(23, 0, 2019)]
    [InlineData(23, 1, 2020)]
    [InlineData(24, 0, 2021)]
    [InlineData(24, 1, 2022)]
    [InlineData(24, 2, 2023)]
    [InlineData(24, 3, 2024)]
    [InlineData(25, 0, 2025)]
    [InlineData(25, 1, 2026)]
    [InlineData(26, 0, 2027)]
    [InlineData(19, 0, 0)]
    public void YearFromAcadVersion_maps_known_series(int major, int minor, int expected)
    {
        Assert.Equal(expected, SeriesDetector.YearFromAcadVersion(new Version(major, minor)));
    }

    [Theory]
    [InlineData(26, 1)]
    [InlineData(27, 0)]
    public void Series_above_newest_known_is_reported_as_newer(int major, int minor)
    {
        Assert.True(SeriesDetector.IsNewerThanKnown(new Version(major, minor)));
    }

    [Theory]
    [InlineData(26, 0)]
    [InlineData(24, 3)]
    [InlineData(19, 0)]
    public void Known_and_older_series_are_not_reported_as_newer(int major, int minor)
    {
        Assert.False(SeriesDetector.IsNewerThanKnown(new Version(major, minor)));
    }

    [Fact]
    public void SeriesString_formats_as_R_major_minor()
    {
        Assert.Equal("R24.3", SeriesDetector.SeriesString(new Version(24, 3)));
    }
}
