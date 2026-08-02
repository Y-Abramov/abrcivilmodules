using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace AbrCivil.Modules.Core
{
    /// <summary>
    /// Установка, обновление и удаление .bundle в ApplicationPlugins.
    /// Прав администратора не требует: всё внутри профиля пользователя.
    /// </summary>
    internal class BundleInstaller
    {
        private readonly string _pluginsRoot;
        private readonly string _tempRoot;
        private readonly IFileDownloader _downloader;

        public BundleInstaller(string pluginsRoot, string tempRoot, IFileDownloader downloader)
        {
            _pluginsRoot = pluginsRoot;
            _tempRoot = tempRoot;
            _downloader = downloader;
        }

        /// <summary>Скачать, проверить хеш и разложить бандл. expectedSha256 пустой - проверка пропускается.</summary>
        public string Install(string moduleName, string url, string expectedSha256)
        {
            Directory.CreateDirectory(_tempRoot);
            var archive = Path.Combine(_tempRoot, moduleName + ".zip");

            _downloader.Download(url, archive);

            try
            {
                if (!string.IsNullOrWhiteSpace(expectedSha256))
                {
                    var actual = ComputeSha256(archive);
                    if (!string.Equals(actual, expectedSha256.Replace("-", "").Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Контрольная сумма не совпала, установка отменена.\r\n" +
                            "Ожидалось: " + expectedSha256 + "\r\nПолучено:  " + actual);
                    }
                }

                return InstallFromArchive(archive);
            }
            finally
            {
                try { File.Delete(archive); } catch (Exception) { }
            }
        }

        /// <summary>Установка из локального архива («Из файла…»).</summary>
        public string InstallFromArchive(string archive)
        {
            var staging = Path.Combine(_tempRoot, "staging_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(staging);
                ZipFile.ExtractToDirectory(archive, staging);

                var source = FindBundleRoot(staging);
                if (source == null)
                    throw new InvalidOperationException("В архиве не найден PackageContents.xml - это не бандл ABR.");

                var target = Path.Combine(_pluginsRoot, new DirectoryInfo(source).Name);
                ApplyFiles(source, target);
                return target;
            }
            finally
            {
                try { Directory.Delete(staging, true); } catch (Exception) { }
            }
        }

        /// <summary>Папка с PackageContents.xml: либо корень архива, либо единственная папка внутри.</summary>
        private static string FindBundleRoot(string staging)
        {
            if (File.Exists(Path.Combine(staging, "PackageContents.xml"))) return staging;

            foreach (var dir in Directory.GetDirectories(staging))
                if (File.Exists(Path.Combine(dir, "PackageContents.xml"))) return dir;

            return null;
        }

        private static void ApplyFiles(string source, string target)
        {
            Directory.CreateDirectory(target);
            LockedFileOps.CleanupOldFiles(target);

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);
                LockedFileOps.CopyOver(file, Path.Combine(target, rel));
            }

            // Файлы прошлой версии, которых нет в новой (кроме *.old - их чистит следующий старт).
            foreach (var existing in Directory.GetFiles(target, "*", SearchOption.AllDirectories))
            {
                var rel = existing.Substring(target.Length).TrimStart(Path.DirectorySeparatorChar);
                if (rel.IndexOf(".old", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (File.Exists(Path.Combine(source, rel))) continue;

                try { File.Delete(existing); }
                catch (Exception) { LockedFileOps.MoveAside(existing); }
            }
        }

        /// <summary>
        /// Удаление. Если файлы заняты - сначала убирается PackageContents.xml:
        /// без манифеста бандл не грузится, остальное подчистится при следующем старте.
        /// </summary>
        public void Uninstall(string bundleDir)
        {
            if (!Directory.Exists(bundleDir)) return;

            var manifest = Path.Combine(bundleDir, "PackageContents.xml");
            if (File.Exists(manifest))
            {
                try { File.Delete(manifest); }
                catch (Exception) { LockedFileOps.MoveAside(manifest); }
            }

            try
            {
                Directory.Delete(bundleDir, true);
                return;
            }
            catch (Exception)
            {
                // Занятые DLL - уводим в *.old, папка исчезнет при следующем старте.
            }

            foreach (var file in Directory.GetFiles(bundleDir, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(file); }
                catch (Exception) { try { LockedFileOps.MoveAside(file); } catch (Exception) { } }
            }
        }

        public static string ComputeSha256(string file)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(file))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        /// <summary>Дочистка *.old во всех бандлах ABR - вызывается из Initialize при старте.</summary>
        public void CleanupAll(params string[] ownedBundleDirs)
        {
            foreach (var dir in ownedBundleDirs.Where(Directory.Exists))
            {
                LockedFileOps.CleanupOldFiles(dir);
                if (!File.Exists(Path.Combine(dir, "PackageContents.xml")) &&
                    Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length == 0)
                {
                    try { Directory.Delete(dir, true); } catch (Exception) { }
                }
            }
        }
    }
}
