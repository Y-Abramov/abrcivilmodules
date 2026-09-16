using System.Collections.Generic;
using System.Globalization;

namespace AbrCivil.Modules.Core
{
    internal enum CheckLevel { Ok, Warn }

    /// <summary>Строка проверки на шаге 1.</summary>
    internal class RequirementCheck
    {
        public string Text = "";
        public CheckLevel Level;
    }

    /// <summary>Снимок машины для проверки требований.</summary>
    internal class SystemInfo
    {
        /// <summary>10 и для Windows 10, и для Windows 11. 0 - прочитать не удалось.</summary>
        public int WindowsMajor;
        /// <summary>22000 и выше - Windows 11.</summary>
        public int WindowsBuild;
        public bool Is64Bit;
        /// <summary>"C:" - диск профиля пользователя.</summary>
        public string Drive = "";
        /// <summary>-1 - прочитать не удалось, строка места не выводится.</summary>
        public long FreeBytes = -1;
    }

    /// <summary>
    /// Технические требования, которые проверяются на машине. Ни одна проверка не блокирует
    /// установку - только цвет строки. Полный текст требований - ресурс requirements.txt.
    /// </summary>
    internal static class SystemRequirements
    {
        public const string WindowsKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        public const long MinFreeBytes = 500L * 1024 * 1024;

        private const int Windows11Build = 22000;

        /// <summary>
        /// Environment.OSVersion на net48 без манифеста совместимости отдаёт 6.2 и на Windows 10/11,
        /// поэтому версия берётся из реестра. CurrentMajorVersionNumber появился в Windows 10.
        /// </summary>
        public static SystemInfo Read(IRegistryProbe registry, bool is64Bit, string drive, long freeBytes)
        {
            var info = new SystemInfo { Is64Bit = is64Bit, Drive = drive ?? "", FreeBytes = freeBytes };

            int major;
            info.WindowsMajor = TryInt(registry.GetValue(WindowsKey, "CurrentMajorVersionNumber"), out major)
                ? major
                : MajorFromCurrentVersion(registry.GetValue(WindowsKey, "CurrentVersion"));

            int build;
            if (TryInt(registry.GetValue(WindowsKey, "CurrentBuild"), out build))
                info.WindowsBuild = build;

            return info;
        }

        public static List<RequirementCheck> Evaluate(SystemInfo info)
        {
            var result = new List<RequirementCheck>();

            if (!info.Is64Bit)
                result.Add(Warn("32-разрядная Windows - Civil 3D не запустится"));
            else if (info.WindowsMajor <= 0)
                result.Add(Warn("Версию Windows определить не удалось"));
            else if (info.WindowsMajor < 10)
                result.Add(Warn("Windows старше 10 - модули не проверялись"));
            else
                result.Add(Ok(info.WindowsBuild >= Windows11Build ? "Windows 11 x64" : "Windows 10 x64"));

            if (info.Is64Bit && info.FreeBytes >= 0)
            {
                result.Add(info.FreeBytes >= MinFreeBytes
                    ? Ok("Свободно на диске " + info.Drive + " " + FormatSize(info.FreeBytes))
                    : Warn("Мало места на диске " + info.Drive + " " + FormatSize(info.FreeBytes)));
            }

            return result;
        }

        /// <summary>null - год не известен, каталога нет или год поддерживается.</summary>
        public static string CompatibilityNote(int hostYear, ModuleEntry library)
        {
            if (hostYear <= 0 || library == null || library.Compatibility.Count == 0) return null;
            if (library.Compatibility.Contains(hostYear.ToString(CultureInfo.InvariantCulture))) return null;

            var years = CompatibilityRange.Years(library.Compatibility);

            var supported = "";
            if (years.Count > 0)
            {
                var continuous = true;
                for (var i = 1; i < years.Count; i++)
                {
                    if (years[i] == years[i - 1] + 1) continue;
                    continuous = false;
                    break;
                }

                supported = continuous
                    ? " (" + years[0].ToString(CultureInfo.InvariantCulture) + "-" + years[years.Count - 1].ToString(CultureInfo.InvariantCulture) + ")"
                    : " (" + string.Join(", ", YearsToStrings(years)) + ")";
            }

            var host = hostYear.ToString(CultureInfo.InvariantCulture);

            return CompatibilityRange.IsNewerThanListed(library.Compatibility, hostYear)
                ? "Civil 3D " + host + " новее проверенных" + supported + " - модули поставятся, работа не проверялась"
                : "Civil 3D " + host + " не входит в поддерживаемые" + supported;
        }

        /// <summary>Целые гигабайты от 1 ГБ, иначе мегабайты.</summary>
        public static string FormatSize(long bytes)
        {
            const long mb = 1024L * 1024;
            const long gb = mb * 1024;
            return bytes >= gb
                ? (bytes / gb).ToString(CultureInfo.InvariantCulture) + " ГБ"
                : (bytes / mb).ToString(CultureInfo.InvariantCulture) + " МБ";
        }

        private static int MajorFromCurrentVersion(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            var dot = value.IndexOf('.');
            int major;
            return TryInt(dot < 0 ? value : value.Substring(0, dot), out major) ? major : 0;
        }

        private static bool TryInt(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private static string[] YearsToStrings(List<int> years)
        {
            var result = new string[years.Count];
            for (var i = 0; i < years.Count; i++)
                result[i] = years[i].ToString(CultureInfo.InvariantCulture);
            return result;
        }

        private static RequirementCheck Ok(string text)   { return new RequirementCheck { Text = text, Level = CheckLevel.Ok }; }
        private static RequirementCheck Warn(string text) { return new RequirementCheck { Text = text, Level = CheckLevel.Warn }; }
    }
}
