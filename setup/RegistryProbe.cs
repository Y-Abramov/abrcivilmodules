using System;
using Microsoft.Win32;
using AbrCivil.Modules.Core;

namespace AbrCivil.Setup
{
    /// <summary>Реальный реестр: 64-битное представление HKLM (Civil 3D пишет туда).</summary>
    internal class RegistryProbe : IRegistryProbe
    {
        public string[] GetSubKeyNames(string path)
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(path))
                    return key == null ? new string[0] : key.GetSubKeyNames();
            }
            catch (Exception)
            {
                return new string[0];
            }
        }

        public string GetValue(string path, string name)
        {
            try
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = hklm.OpenSubKey(path))
                {
                    if (key == null) return null;
                    var value = key.GetValue(name);
                    return value == null ? null : value.ToString();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Запущенный AutoCAD/Civil 3D: файлы бандла могут быть заняты,
        /// и новые модули всё равно подхватятся только после перезапуска.</summary>
        public static bool IsAcadRunning()
        {
            try
            {
                return System.Diagnostics.Process.GetProcessesByName("acad").Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
