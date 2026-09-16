using System;
using System.Collections.Generic;
using System.IO;

namespace AbrCivil.Modules.Core
{
    /// <summary>
    /// Кэш модулей - только то, что скачивается или создаётся заново. Работа пользователя
    /// (пресеты, library.json, vehicles.json, catalog_user.json, любые settings/sources.json,
    /// всё в Roaming) сюда не входит и утилитой не удаляется никогда.
    /// Пути обязаны совпадать с кодом модулей: сменил путь кэша в модуле - поправь карту здесь.
    /// Удаляем только точные пути, не папки модулей: Robur DemLoader делит с Civil-модулями
    /// %AppData%\Abr\DemLoader и кэши. Регистр пути для Windows не важен - ABR и Abr одна папка.
    /// </summary>
    internal static class CacheMap
    {
        private static readonly Dictionary<string, string[]> Relative =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // libmod/Core/AbrPaths.cs (кэш каталога, tmp Стора) + setup/Core/SetupCatalogSource.cs (tmp утилиты)
                { "abr-civil-modules", new[] { @"ABR\Civil\abr_catalog_cache.json", @"ABR\Civil\tmp", @"ABR\Civil\setup" } },
                // Shared/Geo/Net/TileCache.cs - общий с AbrDemLoader и Robur DemLoader, качается заново
                { "AbrBasemap", new[] { @"Abr\Basemap\cache" } },
                // demloader/Core/Dem/DemCache.cs - общий с Robur DemLoader, качается заново
                { "AbrDemLoader", new[] { @"Abr\DemLoader\cache" } },
                // lisp/Core/LispPaths.cs IndexCacheFile
                { "AbrLispManager", new[] { @"ABR\LispManager\index-cache.json" } },
                // cartogram/Core/CartogramPaths.cs LogFile
                { "AbrCartogram", new[] { @"ABR\Civil\Cartogram\cartogram.log" } },
            };

        public static List<string> For(string moduleName, string localAppData)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(localAppData)) return result;

            string[] relative;
            if (!Relative.TryGetValue(moduleName, out relative)) return result;

            foreach (var path in relative) result.Add(Path.Combine(localAppData, path));
            return result;
        }

        /// <summary>Удаляет файлы и папки целиком. Отсутствующий путь - не ошибка.
        /// Возвращает пути, которые удалить не удалось.</summary>
        public static List<string> Clean(IEnumerable<string> paths)
        {
            var failed = new List<string>();
            foreach (var path in paths)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                    else if (Directory.Exists(path)) Directory.Delete(path, true);
                }
                catch (Exception)
                {
                    failed.Add(path);
                }
            }
            return failed;
        }
    }
}
