using System;
using System.Collections.Generic;
using System.IO;

#if NET48
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace AbrCivil.Modules.Core
{
    /// <summary>Состояние фонового тоста обновлений - только флаг «не напоминать».</summary>
    internal static class NotifyState
    {
        private static string StateFile
        {
            get { return Path.Combine(AbrPaths.DataRoot, "abr_notify_state.json"); }
        }

        public static bool IsMuted()
        {
            try
            {
                if (!File.Exists(StateFile)) return false;
                var json = File.ReadAllText(StateFile);

#if NET48
                var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                object v;
                return root != null && root.TryGetValue("mute", out v) && v is bool && (bool)v;
#else
                using (var doc = JsonDocument.Parse(json))
                {
                    JsonElement v;
                    return doc.RootElement.TryGetProperty("mute", out v) &&
                           v.ValueKind == JsonValueKind.True;
                }
#endif
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Mute()
        {
            try
            {
                AbrPaths.EnsureDataRoot();
                File.WriteAllText(StateFile, "{\"mute\":true}");
            }
            catch (Exception)
            {
            }
        }
    }
}
