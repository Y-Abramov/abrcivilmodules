using System;
using System.IO;
using System.Net;

namespace AbrCivil.Modules.Core
{
    internal interface IFileDownloader
    {
        /// <summary>Скачать url в targetFile, вернуть путь к файлу.</summary>
        string Download(string url, string targetFile);
    }

    internal class HttpFileDownloader : IFileDownloader
    {
        public string Download(string url, string targetFile)
        {
            var dir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

#if NET48
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "AbrCivilModules";
            request.Timeout = 60000;

            using (var response = request.GetResponse())
            using (var input = response.GetResponseStream())
            using (var output = File.Create(targetFile))
            {
                if (input == null) throw new IOException("Пустой ответ сервера: " + url);
                input.CopyTo(output);
            }
            return targetFile;
        }
    }
}
