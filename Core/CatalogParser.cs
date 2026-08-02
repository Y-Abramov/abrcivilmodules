using System;
using System.Collections.Generic;

#if NET48
using System.Collections;
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace AbrCivil.Modules.Core
{
    internal static class CatalogParser
    {
        public static List<ModuleEntry> Parse(string json)
        {
            var result = new List<ModuleEntry>();
            if (string.IsNullOrWhiteSpace(json)) return result;

            try
            {
#if NET48
                var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                if (root == null || !root.ContainsKey("modules")) return result;

                var modules = root["modules"] as IEnumerable;
                if (modules == null) return result;

                foreach (var raw in modules)
                {
                    var m = raw as Dictionary<string, object>;
                    if (m == null) continue;

                    var entry = new ModuleEntry
                    {
                        Name        = Str(m, "name"),
                        Title       = Str(m, "title"),
                        Version     = Str(m, "version"),
                        Description = Str(m, "description"),
                        BundleUrl   = Str(m, "bundle_url"),
                        Sha256      = Str(m, "bundle_sha256"),
                        HelpUrl     = Str(m, "help_url")
                    };

                    var compat = m.ContainsKey("compatibility") ? m["compatibility"] as IEnumerable : null;
                    if (compat != null)
                        foreach (var c in compat)
                            if (c != null) entry.Compatibility.Add(c.ToString());

                    result.Add(entry);
                }
#else
                using (var doc = JsonDocument.Parse(json))
                {
                    JsonElement modules;
                    if (!doc.RootElement.TryGetProperty("modules", out modules)) return result;

                    foreach (var m in modules.EnumerateArray())
                    {
                        var entry = new ModuleEntry
                        {
                            Name        = Str(m, "name"),
                            Title       = Str(m, "title"),
                            Version     = Str(m, "version"),
                            Description = Str(m, "description"),
                            BundleUrl   = Str(m, "bundle_url"),
                            Sha256      = Str(m, "bundle_sha256"),
                            HelpUrl     = Str(m, "help_url")
                        };

                        JsonElement compat;
                        if (m.TryGetProperty("compatibility", out compat) && compat.ValueKind == JsonValueKind.Array)
                            foreach (var c in compat.EnumerateArray())
                                entry.Compatibility.Add(c.GetString() ?? "");

                        result.Add(entry);
                    }
                }
#endif
            }
            catch (Exception)
            {
                return new List<ModuleEntry>();
            }

            return result;
        }

#if NET48
        private static string Str(Dictionary<string, object> m, string key)
        {
            object v;
            if (!m.TryGetValue(key, out v) || v == null) return "";
            return v.ToString();
        }
#else
        private static string Str(JsonElement m, string key)
        {
            JsonElement v;
            if (!m.TryGetProperty(key, out v) || v.ValueKind != JsonValueKind.String) return "";
            return v.GetString() ?? "";
        }
#endif
    }
}
