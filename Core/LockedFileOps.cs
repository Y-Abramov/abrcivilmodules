using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AbrCivil.Modules.Core
{
    /// <summary>Файл держит загруженная сборка, и его не удалось ни заменить, ни увести в *.old.</summary>
    internal class FileLockedException : Exception
    {
        public FileLockedException(string path, Exception inner)
            : base("Файл занят запущенным Civil 3D и не может быть заменён:\r\n" + path +
                   "\r\n\r\nЗакройте Civil 3D и повторите операцию.", inner)
        {
        }
    }

    /// <summary>
    /// Windows запрещает удалить или перезаписать загруженную DLL, но разрешает переименовать её
    /// в пределах тома - на этом построен фолбэк в *.old. Паттерн перенесён из линейки Robur.
    /// </summary>
    internal static class LockedFileOps
    {
        private const string OldSuffix = ".old";
        private static readonly Regex OldFileRegex = new Regex(@"\.old\d*$", RegexOptions.IgnoreCase);

        public static void CopyOver(string src, string dst)
        {
            var dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            try
            {
                File.Copy(src, dst, true);
                return;
            }
            catch (IOException)
            {
                // Занят - уводим в *.old и копируем заново.
            }
            catch (UnauthorizedAccessException)
            {
            }

            MoveAside(dst);
            try
            {
                File.Copy(src, dst, true);
            }
            catch (Exception ex)
            {
                throw new FileLockedException(dst, ex);
            }
        }

        /// <summary>Переименовать занятый файл в свободное имя *.old / *.oldN.</summary>
        public static void MoveAside(string path)
        {
            if (!File.Exists(path)) return;

            for (int i = 0; i < 100; i++)
            {
                var candidate = path + OldSuffix + (i == 0 ? "" : i.ToString());
                if (File.Exists(candidate)) continue;

                try
                {
                    File.Move(path, candidate);
                    return;
                }
                catch (IOException)
                {
                }
            }
            throw new FileLockedException(path, null);
        }

        public static void CleanupOldFiles(string dir)
        {
            if (!Directory.Exists(dir)) return;

            foreach (var f in Directory.GetFiles(dir, "*" + OldSuffix + "*", SearchOption.AllDirectories))
            {
                if (!OldFileRegex.IsMatch(f)) continue;
                try { File.Delete(f); } catch (Exception) { }
            }
        }
    }
}
