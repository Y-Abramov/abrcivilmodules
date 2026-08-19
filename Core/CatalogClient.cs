using System;
using System.Collections.Generic;
using System.IO;

namespace AbrCivil.Modules.Core
{
    /// <summary>
    /// Каталог линейки: сеть, при недоступности - последний успешно загруженный кеш.
    /// Резервное зеркало в v1 не используется, поле оставлено для следующей волны.
    /// </summary>
    internal class CatalogClient
    {
        public const string CatalogUrl =
            "https://raw.githubusercontent.com/Y-Abramov/civil3d-modules/main/catalog.json";

        /// <summary>Зеркало на случай недоступности GitHub (тот же приём, что у Robur -
        /// см. reference_module_repos_status/project_live_catalog_monorepo).</summary>
        public const string CatalogBackupUrl =
            "https://storage.yandexcloud.net/abrmove-civil-modules/catalog.json";

        private readonly IFileDownloader _downloader;

        public CatalogClient(IFileDownloader downloader) { _downloader = downloader; }

        public bool LastLoadWasOffline { get; private set; }

        public List<ModuleEntry> Load()
        {
            AbrPaths.EnsureDataRoot();

            var fromPrimary = TryLoad(CatalogUrl);
            if (fromPrimary != null) return fromPrimary;

            if (!string.IsNullOrEmpty(CatalogBackupUrl))
            {
                var fromBackup = TryLoad(CatalogBackupUrl);
                if (fromBackup != null) return fromBackup;
            }

            LastLoadWasOffline = true;
            if (File.Exists(AbrPaths.CatalogCacheFile))
                return CatalogParser.Parse(File.ReadAllText(AbrPaths.CatalogCacheFile));

            return new List<ModuleEntry>();
        }

        /// <summary>null - источник недоступен или отдал пустой каталог, пробуем дальше.</summary>
        private List<ModuleEntry> TryLoad(string url)
        {
            var temp = Path.Combine(AbrPaths.DataRoot, "catalog_download.json");
            try
            {
                _downloader.Download(url, temp);
                var json = File.ReadAllText(temp);
                var parsed = CatalogParser.Parse(json);

                if (parsed.Count == 0) return null;

                File.Copy(temp, AbrPaths.CatalogCacheFile, true);
                LastLoadWasOffline = false;
                return parsed;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                try { File.Delete(temp); } catch (Exception) { }
            }
        }
    }
}
