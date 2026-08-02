using System;

namespace AbrCivil.Modules.Core
{
    /// <summary>Соответствие серии AutoCAD и года Civil 3D.</summary>
    internal static class SeriesDetector
    {
        public static int YearFromAcadVersion(Version v)
        {
            if (v == null) return 0;
            if (v.Major == 24 && v.Minor == 3) return 2024;
            if (v.Major == 25 && v.Minor == 0) return 2025;
            if (v.Major == 25 && v.Minor == 1) return 2026;
            if (v.Major == 26 && v.Minor == 0) return 2027;
            return 0;
        }

        public static string SeriesString(Version v)
        {
            return v == null ? "" : "R" + v.Major + "." + v.Minor;
        }
    }
}
