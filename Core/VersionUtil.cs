using System;

namespace AbrCivil.Modules.Core
{
    internal static class VersionUtil
    {
        /// <summary>Версия каталога новее установленной? Неразбираемая установленная считается устаревшей.</summary>
        public static bool IsNewer(string catalogVersion, string installedVersion)
        {
            Version catalog;
            if (!TryParse(catalogVersion, out catalog)) return false;

            Version installed;
            if (!TryParse(installedVersion, out installed)) return true;

            return catalog > installed;
        }

        public static bool TryParse(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return Version.TryParse(text.Trim(), out version);
        }
    }
}
