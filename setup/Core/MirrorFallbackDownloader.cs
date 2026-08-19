using System;

namespace AbrCivil.Modules.Core
{
    /// <summary>
    /// Основной источник недоступен (блокировка GitHub, сбой CDN) - повтор с зеркала.
    /// Адрес зеркала собирается как «база + имя файла»: в catalog.json полей для зеркала нет,
    /// новый модуль попадает под схему автоматически.
    /// </summary>
    internal class MirrorFallbackDownloader : IFileDownloader
    {
        public const string MirrorBase = "https://storage.yandexcloud.net/abrmove-modules/bundle/";

        private readonly IFileDownloader _inner;
        private readonly string _mirrorBase;

        public MirrorFallbackDownloader(IFileDownloader inner) : this(inner, MirrorBase) { }

        public MirrorFallbackDownloader(IFileDownloader inner, string mirrorBase)
        {
            _inner = inner;
            _mirrorBase = string.IsNullOrEmpty(mirrorBase) ? MirrorBase : mirrorBase;
        }

        /// <summary>Последняя загрузка пришла с зеркала - для строки статуса в окне.</summary>
        public bool LastDownloadUsedMirror { get; private set; }

        public string Download(string url, string targetFile)
        {
            LastDownloadUsedMirror = false;
            try
            {
                return _inner.Download(url, targetFile);
            }
            catch (Exception)
            {
                var mirrored = MirrorUrl(url);
                if (string.Equals(mirrored, url, StringComparison.OrdinalIgnoreCase)) throw;

                var result = _inner.Download(mirrored, targetFile);
                LastDownloadUsedMirror = true;
                return result;
            }
        }

        public string MirrorUrl(string url)
        {
            var name = FileNameOf(url);
            if (string.IsNullOrEmpty(name)) return url;
            return _mirrorBase.TrimEnd('/') + "/" + name;
        }

        private static string FileNameOf(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";

            var cut = url.Split('?', '#')[0].TrimEnd('/');
            var slash = cut.LastIndexOf('/');
            return slash < 0 ? cut : cut.Substring(slash + 1);
        }
    }
}
