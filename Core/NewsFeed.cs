using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

#if NET48
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace AbrCivil.Modules.Core
{
    /// <summary>Одна новость из news.json.</summary>
    internal sealed class NewsItem
    {
        public string Id = "";
        public string Date = "";
        public string Title = "";
        public string Body = "";
        public string Url = "";

        public string DisplayDate
        {
            get
            {
                DateTime dt;
                return DateTime.TryParse(Date, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out dt)
                    ? dt.ToString("dd.MM.yyyy")
                    : Date;
            }
        }
    }

    /// <summary>
    /// Лента новостей ABR - общий фид сайта abrmove.ru, тот же URL и формат, что читает
    /// Robur-линия (AbrModules/NewsFeed.cs). Юзер сознательно не стал разграничивать
    /// новости Robur/civil3d - фид встраивается как есть, без фильтрации по продукту.
    /// Отсутствие news.json (404) - не ошибка, просто нет новостей.
    /// </summary>
    internal static class NewsFeed
    {
        public const string NewsUrl = "https://abrmove.ru/news.json";

        /// <summary>null - сеть недоступна и кэша тоже нет. Никогда не бросает.</summary>
        public static List<NewsItem> Fetch(IFileDownloader downloader)
        {
            AbrPaths.EnsureDataRoot();
            var cacheFile = Path.Combine(AbrPaths.DataRoot, "news_cache.json");
            var temp = Path.Combine(AbrPaths.DataRoot, "news_download.json");

            try
            {
                downloader.Download(NewsUrl, temp);
                var json = File.ReadAllText(temp);
                var items = Parse(json);
                if (items != null)
                {
                    File.Copy(temp, cacheFile, true);
                    return items;
                }
            }
            catch (Exception)
            {
                // Сеть недоступна или новостей нет - пробуем кэш ниже.
            }
            finally
            {
                try { File.Delete(temp); } catch (Exception) { }
            }

            if (File.Exists(cacheFile))
                return Parse(File.ReadAllText(cacheFile));

            return null;
        }

        /// <summary>Новый формат сайта: голый массив [{title,date,summary,link}].
        /// Старый формат (обратная совместимость): {"news":[{id,date,title,body,url}]}.</summary>
        public static List<NewsItem> Parse(string json)
        {
            try
            {
#if NET48
                var ser = new JavaScriptSerializer();
                System.Collections.ArrayList arr;

                var trimmed = (json ?? "").TrimStart();
                if (trimmed.StartsWith("["))
                {
                    arr = ser.Deserialize<System.Collections.ArrayList>(json);
                }
                else
                {
                    var root = ser.Deserialize<Dictionary<string, object>>(json);
                    arr = root != null && root.ContainsKey("news")
                        ? root["news"] as System.Collections.ArrayList
                        : null;
                }
                if (arr == null) return null;

                var list = new List<NewsItem>();
                foreach (var o in arr)
                {
                    var d = o as Dictionary<string, object>;
                    if (d == null) continue;

                    var title = Str(d, "title");
                    if (string.IsNullOrEmpty(title)) continue;

                    var link = Str(d, "link");
                    if (string.IsNullOrEmpty(link)) link = Str(d, "url");

                    var id = Str(d, "id");
                    if (string.IsNullOrEmpty(id)) id = !string.IsNullOrEmpty(link) ? link : title;

                    list.Add(new NewsItem
                    {
                        Id    = id,
                        Date  = Str(d, "date"),
                        Title = title,
                        Body  = !string.IsNullOrEmpty(Str(d, "summary")) ? Str(d, "summary") : Str(d, "body"),
                        Url   = link
                    });
                }
                return list.OrderByDescending(n => n.Date).ToList();
#else
                var trimmed = (json ?? "").TrimStart();
                var list = new List<NewsItem>();

                using (var doc = JsonDocument.Parse(json))
                {
                    JsonElement arr;
                    if (trimmed.StartsWith("["))
                    {
                        arr = doc.RootElement;
                    }
                    else if (!doc.RootElement.TryGetProperty("news", out arr))
                    {
                        return null;
                    }

                    foreach (var d in arr.EnumerateArray())
                    {
                        var title = Str(d, "title");
                        if (string.IsNullOrEmpty(title)) continue;

                        var link = Str(d, "link");
                        if (string.IsNullOrEmpty(link)) link = Str(d, "url");

                        var id = Str(d, "id");
                        if (string.IsNullOrEmpty(id)) id = !string.IsNullOrEmpty(link) ? link : title;

                        var summary = Str(d, "summary");

                        list.Add(new NewsItem
                        {
                            Id    = id,
                            Date  = Str(d, "date"),
                            Title = title,
                            Body  = !string.IsNullOrEmpty(summary) ? summary : Str(d, "body"),
                            Url   = link
                        });
                    }
                }
                return list.OrderByDescending(n => n.Date).ToList();
#endif
            }
            catch (Exception)
            {
                return null;
            }
        }

#if NET48
        private static string Str(Dictionary<string, object> m, string key)
        {
            object v;
            return m.TryGetValue(key, out v) && v != null ? v.ToString() : "";
        }
#else
        private static string Str(JsonElement e, string key)
        {
            JsonElement v;
            return e.TryGetProperty(key, out v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "") : "";
        }
#endif
    }
}
