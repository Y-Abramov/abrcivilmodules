using System.IO;
using AbrCivil.Modules.Core;
using Xunit;

public class LockedFileOpsTests
{
    [Fact]
    public void CopyOver_replaces_free_file()
    {
        var dir = NewDir();
        var src = Path.Combine(dir, "src.txt");
        var dst = Path.Combine(dir, "dst.txt");
        File.WriteAllText(src, "новое");
        File.WriteAllText(dst, "старое");

        LockedFileOps.CopyOver(src, dst);

        Assert.Equal("новое", File.ReadAllText(dst));
        Assert.Empty(Directory.GetFiles(dir, "*.old*"));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void CopyOver_moves_locked_file_to_old()
    {
        var dir = NewDir();
        var src = Path.Combine(dir, "src.txt");
        var dst = Path.Combine(dir, "dst.txt");
        File.WriteAllText(src, "новое");
        File.WriteAllText(dst, "старое");

        // FileShare.Delete обязателен: именно так Windows держит ЗАГРУЖЕННУЮ DLL -
        // перезапись запрещена, переименование разрешено. Без Delete в шаринге
        // File.Move тоже упадёт, и тест проверял бы несуществующий сценарий.
        using (File.Open(dst, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
        {
            LockedFileOps.CopyOver(src, dst);
        }

        Assert.Equal("новое", File.ReadAllText(dst));
        Assert.Single(Directory.GetFiles(dir, "*.old*"));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void CleanupOldFiles_removes_only_old()
    {
        var dir = NewDir();
        File.WriteAllText(Path.Combine(dir, "a.dll"), "x");
        File.WriteAllText(Path.Combine(dir, "a.dll.old"), "x");
        File.WriteAllText(Path.Combine(dir, "b.dll.old2"), "x");

        LockedFileOps.CleanupOldFiles(dir);

        Assert.Single(Directory.GetFiles(dir));
        Directory.Delete(dir, true);
    }

    private static string NewDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }
}
