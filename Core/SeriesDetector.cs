using System;

namespace AbrCivil.Modules.Core
{
    /// <summary>Соответствие серии AutoCAD и года Civil 3D.</summary>
    internal static class SeriesDetector
    {
        /// <summary>Самая новая серия, про которую таблица знает год. Всё выше - будущие версии.</summary>
        public static readonly Version NewestKnown = new Version(26, 0);

        public static int YearFromAcadVersion(Version v)
        {
            if (v == null) return 0;
            if (v.Major == 20 && v.Minor == 0) return 2015;
            if (v.Major == 20 && v.Minor == 1) return 2016;
            if (v.Major == 21 && v.Minor == 0) return 2017;
            if (v.Major == 22 && v.Minor == 0) return 2018;
            if (v.Major == 23 && v.Minor == 0) return 2019;
            if (v.Major == 23 && v.Minor == 1) return 2020;
            if (v.Major == 24 && v.Minor == 0) return 2021;
            if (v.Major == 24 && v.Minor == 1) return 2022;
            if (v.Major == 24 && v.Minor == 2) return 2023;
            if (v.Major == 24 && v.Minor == 3) return 2024;
            if (v.Major == 25 && v.Minor == 0) return 2025;
            if (v.Major == 25 && v.Minor == 1) return 2026;
            if (v.Major == 26 && v.Minor == 0) return 2027;
            return 0;
        }

        /// <summary>
        /// Серия новее последней известной таблице. Год такой версии предсказать нельзя
        /// (шаг серии у Autodesk нерегулярный), но считать её «не найдено» неверно.
        /// </summary>
        public static bool IsNewerThanKnown(Version v)
        {
            if (v == null) return false;
            return YearFromAcadVersion(v) == 0 && new Version(v.Major, v.Minor) > NewestKnown;
        }

        public static string SeriesString(Version v)
        {
            return v == null ? "" : "R" + v.Major + "." + v.Minor;
        }
    }
}
