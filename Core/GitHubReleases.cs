using System;
using System.Collections.Generic;
using System.Net;

#if NET48
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace AbrCivil.Modules.Core
{
    internal class ReleaseInfo
    {
        public string TagName = "";
        public string AssetUrl = "";
        public string AssetName = "";
        public int Downloads;
    }

    /// <summary>GitHub Releases API - счётчик скачиваний и список версий для отката.
    /// Каждый модуль линии живёт в СВОЁМ репозитории (в отличие от Robur, где все модули
    /// были в одном моно-репо и счётчик приходилось фильтровать по префиксу ассета) -
    /// здесь достаточно владельца/имени репо, разобранных из bundle_url.</summary>
    internal static class GitHubReleases
    {
        public static bool TryParseRepo(string bundleUrl, out string owner, out string repo)
        {
            owner = ""; repo = "";
            if (string.IsNullOrEmpty(bundleUrl)) return false;

            const string marker = "github.com/";
            int i = bundleUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return false;

            var rest = bundleUrl.Substring(i + marker.Length);
            var parts = rest.Split('/');
            if (parts.Length < 2) return false;

            owner = parts[0];
            repo = parts[1];
            return !string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo);
        }

        /// <summary>Сумма download_count ассетов релиза с данным тегом (v{version}).</summary>
        public static int FetchDownloadCount(string owner, string repo, string version)
        {
            var json = Get("https://api.github.com/repos/" + owner + "/" + repo + "/releases/tags/v" + version);
            return SumDownloads(json);
        }

        /// <summary>Все релизы репозитория, самый новый первым - для отката на прошлую версию.</summary>
        public static List<ReleaseInfo> FetchAll(string owner, string repo)
        {
            var json = Get("https://api.github.com/repos/" + owner + "/" + repo + "/releases");
            return ParseReleases(json);
        }

        private static string Get(string url)
        {
#if NET48
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "AbrCivilModules";
            request.Accept = "application/vnd.github+json";
            request.Timeout = 15000;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new System.IO.StreamReader(stream))
                return reader.ReadToEnd();
        }

#if NET48
        private static int SumDownloads(string json)
        {
            var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (root == null || !root.ContainsKey("assets")) return 0;

            int total = 0;
            foreach (var raw in (System.Collections.IEnumerable)root["assets"])
            {
                var a = raw as Dictionary<string, object>;
                if (a != null && a.ContainsKey("download_count"))
                    total += Convert.ToInt32(a["download_count"]);
            }
            return total;
        }

        private static List<ReleaseInfo> ParseReleases(string json)
        {
            var result = new List<ReleaseInfo>();
            var array = new JavaScriptSerializer().DeserializeObject(json) as System.Collections.ArrayList;
            if (array == null) return result;

            foreach (var raw in array)
            {
                var r = raw as Dictionary<string, object>;
                if (r == null) continue;

                var info = new ReleaseInfo { TagName = Str(r, "tag_name") };

                var assets = r.ContainsKey("assets") ? r["assets"] as System.Collections.IEnumerable : null;
                if (assets != null)
                    foreach (var rawAsset in assets)
                    {
                        var a = rawAsset as Dictionary<string, object>;
                        if (a == null) continue;
                        info.AssetUrl = Str(a, "browser_download_url");
                        info.AssetName = Str(a, "name");
                        info.Downloads = a.ContainsKey("download_count") ? Convert.ToInt32(a["download_count"]) : 0;
                        break;
                    }

                if (!string.IsNullOrEmpty(info.AssetUrl)) result.Add(info);
            }
            return result;
        }

        private static string Str(Dictionary<string, object> m, string key)
        {
            object v;
            return m.TryGetValue(key, out v) && v != null ? v.ToString() : "";
        }
#else
        private static int SumDownloads(string json)
        {
            using (var doc = JsonDocument.Parse(json))
            {
                JsonElement assets;
                if (!doc.RootElement.TryGetProperty("assets", out assets)) return 0;

                int total = 0;
                foreach (var a in assets.EnumerateArray())
                {
                    JsonElement dc;
                    if (a.TryGetProperty("download_count", out dc)) total += dc.GetInt32();
                }
                return total;
            }
        }

        private static List<ReleaseInfo> ParseReleases(string json)
        {
            var result = new List<ReleaseInfo>();
            using (var doc = JsonDocument.Parse(json))
            {
                foreach (var r in doc.RootElement.EnumerateArray())
                {
                    var info = new ReleaseInfo { TagName = Str(r, "tag_name") };

                    JsonElement assets;
                    if (r.TryGetProperty("assets", out assets))
                        foreach (var a in assets.EnumerateArray())
                        {
                            info.AssetUrl = Str(a, "browser_download_url");
                            info.AssetName = Str(a, "name");
                            JsonElement dc;
                            info.Downloads = a.TryGetProperty("download_count", out dc) ? dc.GetInt32() : 0;
                            break;
                        }

                    if (!string.IsNullOrEmpty(info.AssetUrl)) result.Add(info);
                }
            }
            return result;
        }

        private static string Str(JsonElement e, string key)
        {
            JsonElement v;
            return e.TryGetProperty(key, out v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()) : "";
        }
#endif
    }
}
