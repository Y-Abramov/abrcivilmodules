using System;
using System.Collections.Generic;
using System.IO;

namespace AbrCivil.Modules.Core
{
    internal enum CatalogOrigin
    {
        Live,
        Mirror,
        Embedded
    }

    /// <summary>
    /// Каталог для утилиты установки: живой GitHub, зеркало, встроенный в exe снимок.
    /// От CatalogClient отличается третьей ступенью: у свежей машины офлайн-кеша Стора нет,
    /// зато у exe всегда есть собственный ресурс.
    /// </summary>
    internal class SetupCatalogSource
    {
        private readonly IFileDownloader _downloader;
        private readonly Func<string> _embeddedJson;
        private readonly string _tempDir;

        public SetupCatalogSource(IFileDownloader downloader, Func<string> embeddedJson, string tempDir)
        {
            _downloader   = downloader;
            _embeddedJson = embeddedJson;
            _tempDir      = tempDir;
        }

        public CatalogOrigin Origin { get; private set; }

        /// <summary>Текст для строки статуса в окне мастера.</summary>
        public string OriginText
        {
            get
            {
                switch (Origin)
                {
                    case CatalogOrigin.Live:   return "Каталог обновлён";
                    case CatalogOrigin.Mirror: return "Каталог получен с резервного зеркала";
                    default:                   return "Нет связи, используется встроенный каталог";
                }
            }
        }

        public List<ModuleEntry> Load()
        {
            var live = TryLoad(CatalogClient.CatalogUrl);
            if (live != null)
            {
                Origin = CatalogOrigin.Live;
                return live;
            }

            var mirror = TryLoad(CatalogClient.CatalogBackupUrl);
            if (mirror != null)
            {
                Origin = CatalogOrigin.Mirror;
                return mirror;
            }

            Origin = CatalogOrigin.Embedded;
            return CatalogParser.Parse(_embeddedJson());
        }

        /// <summary>null - источник недоступен или отдал не каталог, пробуем следующий.</summary>
        private List<ModuleEntry> TryLoad(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            var temp = Path.Combine(_tempDir, "setup_catalog.json");
            try
            {
                Directory.CreateDirectory(_tempDir);
                _downloader.Download(url, temp);

                var parsed = CatalogParser.Parse(File.ReadAllText(temp));
                return parsed.Count == 0 ? null : parsed;
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
