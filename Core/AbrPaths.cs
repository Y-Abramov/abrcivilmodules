using System;
using System.IO;

namespace AbrCivil.Modules.Core
{
    internal static class AbrPaths
    {
        /// <summary>%AppData%\Autodesk\ApplicationPlugins - установка без прав администратора.</summary>
        public static string PluginsRoot
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Autodesk", "ApplicationPlugins");
            }
        }

        /// <summary>Папка данных стора: кеш каталога, временные загрузки.</summary>
        public static string DataRoot
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ABR", "Civil");
            }
        }

        public static string CatalogCacheFile
        {
            get { return Path.Combine(DataRoot, "abr_catalog_cache.json"); }
        }

        public static string BundleDir(string moduleName)
        {
            return Path.Combine(PluginsRoot, moduleName + ".bundle");
        }

        public static void EnsureDataRoot()
        {
            Directory.CreateDirectory(DataRoot);
        }
    }
}
