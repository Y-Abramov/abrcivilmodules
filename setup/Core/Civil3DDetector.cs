using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AbrCivil.Modules.Core
{
    /// <summary>Чтение реестра. Реальная реализация - в RegistryProbe вне Core,
    /// чтобы тестовый проект не тянул Microsoft.Win32 на обоих таргетах.</summary>
    internal interface IRegistryProbe
    {
        /// <summary>Имена подключей по пути относительно HKEY_LOCAL_MACHINE. Пустой массив, если ключа нет.</summary>
        string[] GetSubKeyNames(string path);

        /// <summary>Строковое значение или null.</summary>
        string GetValue(string path, string name);
    }

    internal class EnvironmentReport
    {
        public readonly List<int> Years = new List<int>();

        /// <summary>Серии Civil 3D новее таблицы SeriesDetector ("R26.1"): год не известен, но продукт стоит.</summary>
        public readonly List<string> UnknownSeries = new List<string>();

        public bool AcadRunning;

        /// <summary>Год, под который считается совместимость. 0 - Civil 3D не найден.</summary>
        public int HostYear
        {
            get
            {
                int max = 0;
                foreach (var y in Years) if (y > max) max = y;
                return max;
            }
        }
    }

    /// <summary>Установленные Civil 3D по реестру + признак работающего acad.exe.</summary>
    internal class Civil3DDetector
    {
        public const string AutoCadRoot = @"SOFTWARE\Autodesk\AutoCAD";

        private static readonly Regex ReleaseKey = new Regex(@"^R(\d+)\.(\d+)$", RegexOptions.IgnoreCase);

        private readonly IRegistryProbe _registry;
        private readonly Func<bool> _acadRunning;

        public Civil3DDetector(IRegistryProbe registry, Func<bool> acadRunning)
        {
            _registry    = registry;
            _acadRunning = acadRunning;
        }

        public EnvironmentReport Detect()
        {
            var report = new EnvironmentReport();
            report.AcadRunning = _acadRunning();

            foreach (var release in _registry.GetSubKeyNames(AutoCadRoot))
            {
                var m = ReleaseKey.Match(release ?? "");
                if (!m.Success) continue;

                var version = new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
                var year = SeriesDetector.YearFromAcadVersion(version);
                var newer = year == 0 && SeriesDetector.IsNewerThanKnown(version);
                if (year == 0 && !newer) continue;

                var releasePath = AutoCadRoot + "\\" + release;
                foreach (var product in _registry.GetSubKeyNames(releasePath))
                {
                    var name = _registry.GetValue(releasePath + "\\" + product, "ProductName");
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name.IndexOf("Civil", StringComparison.OrdinalIgnoreCase) < 0) continue;

                    if (newer)
                    {
                        var series = SeriesDetector.SeriesString(version);
                        if (!report.UnknownSeries.Contains(series)) report.UnknownSeries.Add(series);
                    }
                    else if (!report.Years.Contains(year))
                    {
                        report.Years.Add(year);
                    }
                    break;
                }
            }

            report.Years.Sort();
            report.UnknownSeries.Sort(StringComparer.OrdinalIgnoreCase);
            return report;
        }
    }
}
