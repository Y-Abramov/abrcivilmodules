using System;
using System.Collections.Generic;
using System.Globalization;

namespace AbrCivil.Modules.Core
{
    /// <summary>
    /// Разбор списка compatibility из catalog.json. Список - это годы, проверенные живьём,
    /// а не потолок совместимости: манифесты бандлов объявляют net8-блок без верхней границы
    /// серии, поэтому Civil 3D новее последнего проверенного года установку не запрещает.
    /// Запрет остаётся только для годов ниже списка и дыр внутри него.
    /// </summary>
    internal static class CompatibilityRange
    {
        /// <summary>Пустой список, неизвестный год хоста или год не ниже проверенных - ставим.</summary>
        public static bool Allows(List<string> compatibility, int hostYear)
        {
            if (compatibility == null || compatibility.Count == 0) return true;
            if (hostYear <= 0) return true;
            if (compatibility.Contains(hostYear.ToString(CultureInfo.InvariantCulture))) return true;

            return IsNewerThanListed(compatibility, hostYear);
        }

        /// <summary>Год хоста выше последнего проверенного - совместимость заявлена, но живьём не проверялась.</summary>
        public static bool IsNewerThanListed(List<string> compatibility, int hostYear)
        {
            var max = MaxYear(compatibility);
            return max > 0 && hostYear > max;
        }

        /// <summary>Годы списка по возрастанию. Нечисловые записи отбрасываются.</summary>
        public static List<int> Years(List<string> compatibility)
        {
            var years = new List<int>();
            if (compatibility == null) return years;

            foreach (var value in compatibility)
            {
                int year;
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out year)) continue;
                if (!years.Contains(year)) years.Add(year);
            }

            years.Sort();
            return years;
        }

        public static int MaxYear(List<string> compatibility)
        {
            var years = Years(compatibility);
            return years.Count == 0 ? 0 : years[years.Count - 1];
        }
    }
}
