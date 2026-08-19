using System;
using System.Collections.Generic;
using System.IO;

namespace AbrCivil.Modules.Core
{
    /// <summary>
    /// Civil 3D сканирует и %AppData%\Autodesk\ApplicationPlugins, и %ProgramData%\Autodesk\ApplicationPlugins.
    /// Старая копия в ProgramData перекрывает свежую в профиле пользователя - живой инцидент 2026-08-20.
    /// </summary>
    internal static class ShadowScanner
    {
        public static string ProgramDataPluginsRoot
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Autodesk", "ApplicationPlugins");
            }
        }

        public static List<string> Find(string pluginsRoot)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(pluginsRoot) || !Directory.Exists(pluginsRoot)) return result;

            try
            {
                foreach (var dir in Directory.GetDirectories(pluginsRoot, "Abr*.bundle"))
                    result.Add(dir);
            }
            catch (Exception)
            {
                // Нет доступа к чужой папке - показывать нечего, но окно ронять нельзя.
            }
            return result;
        }

        /// <summary>Возвращает папки, которые удалить не удалось.</summary>
        public static List<string> Remove(IEnumerable<string> dirs)
        {
            var failed = new List<string>();
            foreach (var dir in dirs)
            {
                try
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                }
                catch (Exception)
                {
                    failed.Add(dir);
                }
            }
            return failed;
        }
    }
}
