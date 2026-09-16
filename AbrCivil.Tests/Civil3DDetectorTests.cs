using System.Collections.Generic;
using System.Linq;
using AbrCivil.Modules.Core;
using Xunit;

public class Civil3DDetectorTests
{
    private class FakeRegistry : IRegistryProbe
    {
        public readonly Dictionary<string, string[]> SubKeys = new Dictionary<string, string[]>();
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();

        public string[] GetSubKeyNames(string path)
        {
            string[] found;
            return SubKeys.TryGetValue(path, out found) ? found : new string[0];
        }

        public string GetValue(string path, string name)
        {
            string found;
            return Values.TryGetValue(path + "|" + name, out found) ? found : null;
        }
    }

    private const string Root = @"SOFTWARE\Autodesk\AutoCAD";

    [Fact]
    public void Finds_single_civil3d_2024()
    {
        var reg = new FakeRegistry();
        reg.SubKeys[Root] = new[] { "R24.3" };
        reg.SubKeys[Root + @"\R24.3"] = new[] { "ACAD-7101:409" };
        reg.Values[Root + @"\R24.3\ACAD-7101:409|ProductName"] = "AutoCAD Civil 3D 2024";

        var report = new Civil3DDetector(reg, () => false).Detect();

        Assert.Equal(new[] { 2024 }, report.Years.ToArray());
        Assert.False(report.AcadRunning);
    }

    [Fact]
    public void Finds_two_versions_sorted()
    {
        var reg = new FakeRegistry();
        reg.SubKeys[Root] = new[] { "R25.1", "R24.3" };
        reg.SubKeys[Root + @"\R25.1"] = new[] { "ACAD-9101:409" };
        reg.SubKeys[Root + @"\R24.3"] = new[] { "ACAD-7101:409" };
        reg.Values[Root + @"\R25.1\ACAD-9101:409|ProductName"] = "AutoCAD Civil 3D 2026";
        reg.Values[Root + @"\R24.3\ACAD-7101:409|ProductName"] = "AutoCAD Civil 3D 2024";

        var report = new Civil3DDetector(reg, () => false).Detect();

        Assert.Equal(new[] { 2024, 2026 }, report.Years.ToArray());
    }

    [Fact]
    public void Plain_autocad_is_ignored()
    {
        var reg = new FakeRegistry();
        reg.SubKeys[Root] = new[] { "R24.3" };
        reg.SubKeys[Root + @"\R24.3"] = new[] { "ACAD-0001:409" };
        reg.Values[Root + @"\R24.3\ACAD-0001:409|ProductName"] = "AutoCAD 2024";

        var report = new Civil3DDetector(reg, () => false).Detect();

        Assert.Empty(report.Years);
    }

    [Fact]
    public void Subkey_without_product_name_does_not_throw()
    {
        var reg = new FakeRegistry();
        reg.SubKeys[Root] = new[] { "R24.3", "Не версия" };
        reg.SubKeys[Root + @"\R24.3"] = new[] { "ACAD-7101:409", "Broken" };
        reg.Values[Root + @"\R24.3\ACAD-7101:409|ProductName"] = "AutoCAD Civil 3D 2024";

        var report = new Civil3DDetector(reg, () => true).Detect();

        Assert.Equal(new[] { 2024 }, report.Years.ToArray());
        Assert.True(report.AcadRunning);
    }

    [Fact]
    public void Civil3d_of_unknown_newer_series_is_reported_separately()
    {
        var reg = new FakeRegistry();
        reg.SubKeys[Root] = new[] { "R26.1" };
        reg.SubKeys[Root + @"\R26.1"] = new[] { "ACAD-A101:409" };
        reg.Values[Root + @"\R26.1\ACAD-A101:409|ProductName"] = "AutoCAD Civil 3D 2028";

        var report = new Civil3DDetector(reg, () => false).Detect();

        Assert.Empty(report.Years);
        Assert.Equal(new[] { "R26.1" }, report.UnknownSeries.ToArray());
    }

    [Fact]
    public void Plain_autocad_of_unknown_series_is_ignored()
    {
        var reg = new FakeRegistry();
        reg.SubKeys[Root] = new[] { "R26.1" };
        reg.SubKeys[Root + @"\R26.1"] = new[] { "ACAD-0001:409" };
        reg.Values[Root + @"\R26.1\ACAD-0001:409|ProductName"] = "AutoCAD 2028";

        var report = new Civil3DDetector(reg, () => false).Detect();

        Assert.Empty(report.Years);
        Assert.Empty(report.UnknownSeries);
    }

    [Fact]
    public void Unknown_series_are_sorted()
    {
        var reg = new FakeRegistry();
        reg.SubKeys[Root] = new[] { "R27.0", "R26.1" };
        reg.SubKeys[Root + @"\R27.0"] = new[] { "ACAD-B101:409" };
        reg.SubKeys[Root + @"\R26.1"] = new[] { "ACAD-A101:409" };
        reg.Values[Root + @"\R27.0\ACAD-B101:409|ProductName"] = "AutoCAD Civil 3D 2029";
        reg.Values[Root + @"\R26.1\ACAD-A101:409|ProductName"] = "AutoCAD Civil 3D 2028";

        var report = new Civil3DDetector(reg, () => false).Detect();

        Assert.Equal(new[] { "R26.1", "R27.0" }, report.UnknownSeries.ToArray());
    }
}
