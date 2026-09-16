using System;
using System.Collections.Generic;
using System.IO;

namespace AbrCivil.Modules.Core
{
    /// <summary>Строка страницы удаления.</summary>
    internal class RemovalItem
    {
        /// <summary>Name из PackageContents.xml - ключ карты кэша.</summary>
        public string Name = "";
        public string Title = "";
        public string Version = "";
        public string Directory = "";
        public bool IsDisabled;
        public bool IsLibrary;
    }

    /// <summary>
    /// Что показать на странице удаления. Только модули ABR: имя есть в каталоге или папка
    /// Abr*.bundle (ловит модули, выпавшие из каталога). Чужие бандлы не показываются никогда.
    /// Каталог может быть пустым - удаление работает без сети.
    /// </summary>
    internal static class RemovalPlanner
    {
        public const string LibraryTitle = "Библиотека модулей";

        public static List<RemovalItem> Build(List<InstalledBundle> installed, List<ModuleEntry> catalog)
        {
            var result = new List<RemovalItem>();
            if (installed == null) return result;
            if (catalog == null) catalog = new List<ModuleEntry>();

            var known = new List<KeyValuePair<int, RemovalItem>>();
            var unknown = new List<RemovalItem>();

            foreach (var bundle in installed)
            {
                var index = IndexOf(catalog, bundle.Name);
                if (index < 0 && !IsAbrFolder(bundle.Directory)) continue;

                var isLibrary = string.Equals(bundle.Name, SetupPlanner.LibraryName, StringComparison.OrdinalIgnoreCase);
                var item = new RemovalItem
                {
                    Name       = bundle.Name,
                    Title      = TitleOf(bundle, index >= 0 ? catalog[index] : null, isLibrary),
                    Version    = bundle.Version,
                    Directory  = bundle.Directory,
                    IsDisabled = bundle.IsDisabled,
                    IsLibrary  = isLibrary
                };

                if (isLibrary) result.Add(item);
                else if (index >= 0) known.Add(new KeyValuePair<int, RemovalItem>(index, item));
                else unknown.Add(item);
            }

            known.Sort((a, b) => a.Key.CompareTo(b.Key));
            unknown.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            foreach (var pair in known) result.Add(pair.Value);
            result.AddRange(unknown);
            return result;
        }

        private static string TitleOf(InstalledBundle bundle, ModuleEntry entry, bool isLibrary)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.Title)) return entry.Title;
            return isLibrary ? LibraryTitle : bundle.Name;
        }

        private static int IndexOf(List<ModuleEntry> catalog, string name)
        {
            for (var i = 0; i < catalog.Count; i++)
                if (string.Equals(catalog[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        /// <summary>Тот же признак, что у ShadowScanner: папка Abr*.bundle.</summary>
        private static bool IsAbrFolder(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return false;
            var folder = Path.GetFileName(directory.TrimEnd('\\', '/'));
            return folder.StartsWith("Abr", StringComparison.OrdinalIgnoreCase)
                && folder.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase);
        }
    }
}
