using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace AbrCivil.Modules.Core
{
    /// <summary>
    /// Источник истины о установленном - файловая система: ApplicationPlugins\*.bundle\PackageContents.xml.
    /// Ни packages.json, ни реестра, ни прав администратора.
    /// </summary>
    internal static class InstalledScanner
    {
        public static List<InstalledBundle> Scan(string pluginsRoot)
        {
            var result = new List<InstalledBundle>();
            if (string.IsNullOrEmpty(pluginsRoot) || !Directory.Exists(pluginsRoot)) return result;

            foreach (var dir in Directory.GetDirectories(pluginsRoot, "*.bundle"))
            {
                var manifest = Path.Combine(dir, "PackageContents.xml");
                bool disabled = false;

                if (!File.Exists(manifest))
                {
                    // Отключённый модуль: манифест уведён в .disabled (BundleInstaller.SetEnabled),
                    // Civil 3D его не находит и не грузит, но модуль остаётся «видимым» в Сторе.
                    manifest = Path.Combine(dir, "PackageContents.xml.disabled");
                    if (!File.Exists(manifest)) continue;
                    disabled = true;
                }

                try
                {
                    var root = XDocument.Load(manifest).Root;
                    if (root == null) continue;

                    result.Add(new InstalledBundle
                    {
                        Name       = Attr(root, "Name"),
                        Version    = Attr(root, "AppVersion"),
                        Directory  = dir,
                        IsDisabled = disabled
                    });
                }
                catch (Exception)
                {
                    // Битый манифест чужого бандла не должен ронять окно.
                }
            }
            return result;
        }

        private static string Attr(XElement e, string name)
        {
            var a = e.Attribute(name);
            return a == null ? "" : a.Value;
        }
    }
}
