using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if NET48
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace AbrCivil.Modules.Core
{
    /// <summary>Снапшот непрочитанного на момент показа тоста. Передаётся в StoreForm,
    /// т.к. seen метится СРАЗУ при показе тоста - без снапшота стор открылся бы уже
    /// «прочитанным» (без чипов «Новый»). Тот же принцип, что в Robur (AbrModules/NotifyState.cs).</summary>
    internal sealed class UnreadSnapshot
    {
        public readonly HashSet<string> NewsIds     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> ModuleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Состояние уведомлений - abr_notify_state.json в DataRoot:
    /// {"mute":true,"seeded":true,"seen_news":[...],"seen_modules":[...]}.
    /// Обратная совместимость: старый файл содержал только "mute" - отсутствие
    /// остальных ключей означает пустые списки/не засеяно.</summary>
    internal static class NotifyState
    {
        private static string StateFile
        {
            get { return Path.Combine(AbrPaths.DataRoot, "abr_notify_state.json"); }
        }

        private sealed class State
        {
            public bool Mute;
            public bool Seeded;
            public HashSet<string> SeenNews    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> SeenModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsMuted()
        {
            return Load().Mute;
        }

        public static void Mute()
        {
            var s = Load();
            s.Mute = true;
            Save(s);
        }

        /// <summary>Первый запуск после появления ленты новостей/чипа «Новый»: всё
        /// существующее молча в seen, иначе весь текущий каталог и вся история новостей
        /// свалились бы «новыми» разом.</summary>
        public static void EnsureSeeded(List<ModuleEntry> modules, List<NewsItem> news)
        {
            var s = Load();
            if (s.Seeded) return;

            if (modules != null)
                foreach (var m in modules)
                    s.SeenModules.Add(m.Name);

            if (news != null)
                foreach (var n in news)
                    s.SeenNews.Add(n.Id);

            s.Seeded = true;
            Save(s);
        }

        /// <summary>Что ещё не показывалось в тосте. Вызывать ПОСЛЕ EnsureSeeded.
        /// «Новый модуль» = каталожная запись, для которой нет установленного пакета.</summary>
        public static UnreadSnapshot GetUnread(List<ModuleEntry> modules, List<InstalledBundle> installed, List<NewsItem> news)
        {
            var s    = Load();
            var snap = new UnreadSnapshot();

            if (modules != null)
                foreach (var m in modules)
                    if (ModuleStateResolver.Find(installed, m.Name) == null && !s.SeenModules.Contains(m.Name))
                        snap.ModuleNames.Add(m.Name);

            if (news != null)
                foreach (var n in news)
                    if (!s.SeenNews.Contains(n.Id))
                        snap.NewsIds.Add(n.Id);

            return snap;
        }

        /// <summary>«Прочитано при показе тоста» - решение юзера (тот же принцип, что в Robur).</summary>
        public static void MarkSeen(UnreadSnapshot snap)
        {
            if (snap == null) return;
            var s = Load();
            foreach (var name in snap.ModuleNames) s.SeenModules.Add(name);
            foreach (var id   in snap.NewsIds)     s.SeenNews.Add(id);
            Save(s);
        }

        // ─── Файл ────────────────────────────────────────────────────────────

        private static State Load()
        {
            var s = new State();
            try
            {
                if (!File.Exists(StateFile)) return s;
                var json = File.ReadAllText(StateFile);

#if NET48
                var root = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                if (root == null) return s;

                object v;
                s.Mute        = root.TryGetValue("mute", out v)   && v is bool && (bool)v;
                s.Seeded      = root.TryGetValue("seeded", out v) && v is bool && (bool)v;
                s.SeenNews    = ReadStringSet(root, "seen_news");
                s.SeenModules = ReadStringSet(root, "seen_modules");
#else
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    JsonElement v;
                    s.Mute        = root.TryGetProperty("mute", out v)   && v.ValueKind == JsonValueKind.True;
                    s.Seeded      = root.TryGetProperty("seeded", out v) && v.ValueKind == JsonValueKind.True;
                    s.SeenNews    = ReadStringSet(root, "seen_news");
                    s.SeenModules = ReadStringSet(root, "seen_modules");
                }
#endif
            }
            catch (Exception)
            {
            }
            return s;
        }

        private static void Save(State s)
        {
            try
            {
                AbrPaths.EnsureDataRoot();
                var root = new Dictionary<string, object>
                {
                    { "mute", s.Mute },
                    { "seeded", s.Seeded },
                    { "seen_news", s.SeenNews.ToList() },
                    { "seen_modules", s.SeenModules.ToList() }
                };
#if NET48
                File.WriteAllText(StateFile, new JavaScriptSerializer().Serialize(root));
#else
                File.WriteAllText(StateFile, JsonSerializer.Serialize(root));
#endif
            }
            catch (Exception)
            {
            }
        }

#if NET48
        private static HashSet<string> ReadStringSet(Dictionary<string, object> root, string key)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            object v;
            if (!root.TryGetValue(key, out v)) return set;
            var arr = v as System.Collections.ArrayList;
            if (arr != null)
                foreach (var o in arr)
                    if (o != null) set.Add(o.ToString());
            return set;
        }
#else
        private static HashSet<string> ReadStringSet(JsonElement root, string key)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            JsonElement v;
            if (!root.TryGetProperty(key, out v) || v.ValueKind != JsonValueKind.Array) return set;
            foreach (var item in v.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String) set.Add(item.GetString());
            return set;
        }
#endif
    }
}
