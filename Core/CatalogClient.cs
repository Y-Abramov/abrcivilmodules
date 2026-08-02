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
        public const string CatalogBackupUrl = "";

        private readonly IFileDownloader _downloader;

        public CatalogClient(IFileDownloader downloader) { _downloader = downloader; }

        public bool LastLoadWasOffline { get; private set; }

        public List<ModuleEntry> Load()
        {
            AbrPaths.EnsureDataRoot();
            var temp = Path.Combine(AbrPaths.DataRoot, "catalog_download.json");

            try
            {
                _downloader.Download(CatalogUrl, temp);
                var json = File.ReadAllText(temp);
                var parsed = CatalogParser.Parse(json);

                if (parsed.Count > 0)
                {
                    File.Copy(temp, AbrPaths.CatalogCacheFile, true);
                    LastLoadWasOffline = false;
                    return parsed;
                }
            }
            catch (Exception)
            {
                // Сеть недоступна - ниже офлайн-фолбэк.
            }
            finally
            {
                try { File.Delete(temp); } catch (Exception) { }
            }

            LastLoadWasOffline = true;
            if (File.Exists(AbrPaths.CatalogCacheFile))
                return CatalogParser.Parse(File.ReadAllText(AbrPaths.CatalogCacheFile));

            return new List<ModuleEntry>();
        }
    }
}
