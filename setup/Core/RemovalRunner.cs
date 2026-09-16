using System;
using System.Collections.Generic;
using System.IO;

namespace AbrCivil.Modules.Core
{
    internal enum RemovalOutcome { Removed, RemovedWithLeftovers, Failed }

    internal class RemovalResult
    {
        public RemovalOutcome Outcome;
        /// <summary>First leftover path or first error line.</summary>
        public string Detail = "";
    }

    /// <summary>Removes one module and reports the result from disk state after the operation.</summary>
    internal static class RemovalRunner
    {
        private const string Manifest = "PackageContents.xml";
        private const string DisabledManifest = "PackageContents.xml.disabled";

        public static RemovalResult Remove(RemovalItem item, bool cleanCache, BundleInstaller installer, string localAppData)
        {
            try
            {
                RemoveDisabledManifest(item.Directory);
                installer.Uninstall(item.Directory);
            }
            catch (Exception ex)
            {
                return new RemovalResult { Outcome = RemovalOutcome.Failed, Detail = FirstLine(ex.Message) };
            }

            if (File.Exists(Path.Combine(item.Directory, Manifest)) ||
                File.Exists(Path.Combine(item.Directory, DisabledManifest)))
            {
                return new RemovalResult
                {
                    Outcome = RemovalOutcome.Failed,
                    Detail = "manifest was not removed: " + item.Directory
                };
            }

            var leftovers = new List<string>();
            if (Directory.Exists(item.Directory)) leftovers.Add(item.Directory);
            if (cleanCache) leftovers.AddRange(CacheMap.Clean(CacheMap.For(item.Name, localAppData)));

            return leftovers.Count == 0
                ? new RemovalResult { Outcome = RemovalOutcome.Removed }
                : new RemovalResult { Outcome = RemovalOutcome.RemovedWithLeftovers, Detail = leftovers[0] };
        }

        private static void RemoveDisabledManifest(string bundleDir)
        {
            var disabled = Path.Combine(bundleDir, DisabledManifest);
            if (!File.Exists(disabled)) return;

            try { File.Delete(disabled); }
            catch (Exception) { LockedFileOps.MoveAside(disabled); }
        }

        private static string FirstLine(string message)
        {
            if (string.IsNullOrEmpty(message)) return "unknown error";
            var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? lines[0] : message;
        }
    }
}
