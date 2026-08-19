using System;
using System.Collections.Generic;

namespace AbrCivil.Modules.Core
{
    internal enum ModuleState
    {
        NotInstalled,
        Installed,
        UpdateAvailable,
        Incompatible,
        NotInCatalog,
        Disabled
    }

    internal static class ModuleStateResolver
    {
        public static ModuleState Resolve(ModuleEntry entry, List<InstalledBundle> installed, int hostYear)
        {
            if (entry == null) return ModuleState.NotInCatalog;

            if (entry.Compatibility.Count > 0 && !entry.Compatibility.Contains(hostYear.ToString()))
                return ModuleState.Incompatible;

            var found = Find(installed, entry.Name);
            if (found == null) return ModuleState.NotInstalled;
            if (found.IsDisabled) return ModuleState.Disabled;

            return VersionUtil.IsNewer(entry.Version, found.Version)
                ? ModuleState.UpdateAvailable
                : ModuleState.Installed;
        }

        public static InstalledBundle Find(List<InstalledBundle> installed, string name)
        {
            if (installed == null) return null;
            foreach (var b in installed)
                if (string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)) return b;
            return null;
        }
    }
}
