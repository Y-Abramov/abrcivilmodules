using System.Collections.Generic;
using AbrCivil.Modules.Core;
using Xunit;

public class SystemRequirementsTests
{
    private class FakeRegistry : IRegistryProbe
    {
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();

        public string[] GetSubKeyNames(string path) { return new string[0]; }

        public string GetValue(string path, string name)
        {
            string found;
            return Values.TryGetValue(path + "|" + name, out found) ? found : null;
        }
    }

    private const string Key = SystemRequirements.WindowsKey;
    private const long Gb = 1024L * 1024 * 1024;

    private static SystemInfo ReadWindows(string major, string build, string currentVersion)
    {
        var reg = new FakeRegistry();
        if (major != null) reg.Values[Key + "|CurrentMajorVersionNumber"] = major;
        if (currentVersion != null) reg.Values[Key + "|CurrentVersion"] = currentVersion;
        if (build != null) reg.Values[Key + "|CurrentBuild"] = build;
        return SystemRequirements.Read(reg, true, "C:", 42 * Gb);
    }

    [Fact]
    public void Read_takes_major_and_build_from_registry()
    {
        var info = ReadWindows("10", "19045", "6.3");

        Assert.Equal(10, info.WindowsMajor);
        Assert.Equal(19045, info.WindowsBuild);
        Assert.True(info.Is64Bit);
        Assert.Equal("C:", info.Drive);
        Assert.Equal(42 * Gb, info.FreeBytes);
    }

    [Fact]
    public void Read_falls_back_to_current_version_on_old_windows()
    {
        var info = ReadWindows(null, "9600", "6.3");

        Assert.Equal(6, info.WindowsMajor);
        Assert.Equal(9600, info.WindowsBuild);
    }

    [Fact]
    public void Windows_10_and_11_are_told_apart_by_build()
    {
        var win10 = SystemRequirements.Evaluate(ReadWindows("10", "19045", "6.3"))[0];
        var win11 = SystemRequirements.Evaluate(ReadWindows("10", "22631", "6.3"))[0];

        Assert.Equal("Windows 10 x64", win10.Text);
        Assert.Equal(CheckLevel.Ok, win10.Level);
        Assert.Equal("Windows 11 x64", win11.Text);
        Assert.Equal(CheckLevel.Ok, win11.Level);
    }

    [Fact]
    public void Old_windows_is_warning()
    {
        var check = SystemRequirements.Evaluate(ReadWindows(null, "9600", "6.3"))[0];

        Assert.Equal("Windows старше 10 - модули не проверялись", check.Text);
        Assert.Equal(CheckLevel.Warn, check.Level);
    }

    [Fact]
    public void Unknown_windows_version_is_warning_not_old()
    {
        var check = SystemRequirements.Evaluate(ReadWindows(null, null, null))[0];

        Assert.Equal("Версию Windows определить не удалось", check.Text);
        Assert.Equal(CheckLevel.Warn, check.Level);
    }

    [Fact]
    public void Bit32_is_single_warning()
    {
        var info = new SystemInfo { WindowsMajor = 10, WindowsBuild = 19045, Is64Bit = false, Drive = "C:", FreeBytes = 42 * Gb };

        var checks = SystemRequirements.Evaluate(info);

        Assert.Single(checks);
        Assert.Equal("32-разрядная Windows - Civil 3D не запустится", checks[0].Text);
        Assert.Equal(CheckLevel.Warn, checks[0].Level);
    }

    [Fact]
    public void Free_space_ok_and_low()
    {
        var ok = new SystemInfo { WindowsMajor = 10, WindowsBuild = 19045, Is64Bit = true, Drive = "C:", FreeBytes = 42 * Gb };
        var low = new SystemInfo { WindowsMajor = 10, WindowsBuild = 19045, Is64Bit = true, Drive = "C:", FreeBytes = 400L * 1024 * 1024 };

        var okDisk = SystemRequirements.Evaluate(ok)[1];
        var lowDisk = SystemRequirements.Evaluate(low)[1];

        Assert.Equal("Свободно на диске C: 42 ГБ", okDisk.Text);
        Assert.Equal(CheckLevel.Ok, okDisk.Level);
        Assert.Equal("Мало места на диске C: 400 МБ", lowDisk.Text);
        Assert.Equal(CheckLevel.Warn, lowDisk.Level);
    }

    [Fact]
    public void Boundaries_build_22000_and_exact_min_free_space()
    {
        var info = new SystemInfo { WindowsMajor = 10, WindowsBuild = 22000, Is64Bit = true, Drive = "C:", FreeBytes = SystemRequirements.MinFreeBytes };

        var checks = SystemRequirements.Evaluate(info);

        Assert.Equal("Windows 11 x64", checks[0].Text);
        Assert.Equal(CheckLevel.Ok, checks[0].Level);
        Assert.Equal("Свободно на диске C: 500 МБ", checks[1].Text);
        Assert.Equal(CheckLevel.Ok, checks[1].Level);
    }

    [Fact]
    public void Unknown_free_space_adds_no_row()
    {
        var info = new SystemInfo { WindowsMajor = 10, WindowsBuild = 19045, Is64Bit = true, Drive = "C:", FreeBytes = -1 };

        Assert.Single(SystemRequirements.Evaluate(info));
    }

    private static ModuleEntry Library(int from, int to)
    {
        var entry = new ModuleEntry { Name = "abr-civil-modules" };
        for (var year = from; year <= to; year++) entry.Compatibility.Add(year.ToString());
        return entry;
    }

    [Fact]
    public void Compatibility_note_for_year_newer_than_tested()
    {
        Assert.Equal("Civil 3D 2027 новее проверенных (2015-2026) - модули поставятся, работа не проверялась",
            SystemRequirements.CompatibilityNote(2027, Library(2015, 2026)));
    }

    [Fact]
    public void Compatibility_note_for_year_older_than_supported()
    {
        Assert.Equal("Civil 3D 2014 не входит в поддерживаемые (2015-2026)",
            SystemRequirements.CompatibilityNote(2014, Library(2015, 2026)));
    }

    [Fact]
    public void Compatibility_note_lists_years_when_not_continuous()
    {
        var library = new ModuleEntry { Name = "abr-civil-modules" };
        library.Compatibility.Add("2021");
        library.Compatibility.Add("2024");
        library.Compatibility.Add("2025");
        library.Compatibility.Add("2026");

        Assert.Equal("Civil 3D 2022 не входит в поддерживаемые (2021, 2024, 2025, 2026)",
            SystemRequirements.CompatibilityNote(2022, library));
    }

    [Fact]
    public void No_compatibility_note_when_supported_unknown_or_no_catalog()
    {
        Assert.Null(SystemRequirements.CompatibilityNote(2022, Library(2015, 2026)));
        Assert.Null(SystemRequirements.CompatibilityNote(0, Library(2015, 2026)));
        Assert.Null(SystemRequirements.CompatibilityNote(2027, null));
        Assert.Null(SystemRequirements.CompatibilityNote(2027, new ModuleEntry()));
    }
}
