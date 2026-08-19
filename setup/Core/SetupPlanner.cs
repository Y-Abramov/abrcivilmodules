using System;
using System.Collections.Generic;

namespace AbrCivil.Modules.Core
{
    /// <summary>Строка списка на шаге выбора модулей.</summary>
    internal class ModuleChoice
    {
        public ModuleEntry Entry;
        public ModuleState State;
        public InstalledBundle Installed;
        public bool Checked;
        public bool Locked;
        public bool IsLibrary;
        public string StatusText = "";
    }

    /// <summary>
    /// Дефолты галок и подписи состояний. Утилита ничего не удаляет: снятая галка
    /// означает «не трогать», а не «удалить» - удаление живёт в Сторе внутри Civil 3D.
    /// </summary>
    internal static class SetupPlanner
    {
        /// <summary>name библиотеки в catalog.json (совпадает с AppName её PackageContents.xml).</summary>
        public const string LibraryName = "abr-civil-modules";

        public static List<ModuleChoice> Build(
            List<ModuleEntry> catalog, List<InstalledBundle> installed, int hostYear)
        {
            var result = new List<ModuleChoice>();
            if (catalog == null) return result;

            foreach (var entry in catalog)
            {
                var isLibrary = string.Equals(entry.Name, LibraryName, StringComparison.OrdinalIgnoreCase);
                var state     = ModuleStateResolver.Resolve(entry, installed, EffectiveYear(entry, hostYear));

                var choice = new ModuleChoice
                {
                    Entry     = entry,
                    State     = state,
                    Installed = ModuleStateResolver.Find(installed, entry.Name),
                    IsLibrary = isLibrary,
                    Locked    = isLibrary || state == ModuleState.Incompatible
                };

                switch (state)
                {
                    case ModuleState.NotInstalled:
                        choice.Checked = true;
                        choice.StatusText = entry.Version;
                        break;

                    case ModuleState.UpdateAvailable:
                        choice.Checked = true;
                        choice.StatusText = "установлен " + choice.Installed.Version + " > " + entry.Version;
                        break;

                    case ModuleState.Installed:
                        choice.Checked = false;
                        choice.StatusText = "установлен " + choice.Installed.Version;
                        break;

                    case ModuleState.Disabled:
                        // Отключённая библиотека = нет Стора и нет обновлений, поэтому её включаем.
                        choice.Checked = isLibrary;
                        choice.StatusText = isLibrary ? "отключён, будет включён" : "отключён";
                        break;

                    default: // Incompatible
                        choice.Checked = false;
                        choice.StatusText = "не для Civil 3D " + hostYear;
                        break;
                }

                if (isLibrary) result.Insert(0, choice);
                else result.Add(choice);
            }

            return result;
        }

        /// <summary>Civil 3D не найден - совместимость не проверяем: подставляем год,
        /// который заведомо удовлетворяет записи каталога.</summary>
        private static int EffectiveYear(ModuleEntry entry, int hostYear)
        {
            if (hostYear > 0) return hostYear;
            if (entry.Compatibility.Count == 0) return 0;

            int parsed;
            return int.TryParse(entry.Compatibility[0], out parsed) ? parsed : 0;
        }
    }
}
