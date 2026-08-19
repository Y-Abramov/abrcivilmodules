using System;
using System.Linq;
using System.Windows.Forms;
using AbrCivil.Modules.Core;

namespace AbrCivil.Setup
{
    internal static class Program
    {
        /// <summary>Единственный режим, требующий прав администратора: удаление
        /// перекрывающих копий из ProgramData. Окно не показывается.</summary>
        public const string CleanArg = "--clean-programdata";

        [STAThread]
        private static int Main(string[] args)
        {
            if (args != null && args.Any(a => string.Equals(a, CleanArg, StringComparison.OrdinalIgnoreCase)))
                return CleanProgramData();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var form = new SetupForm())
                Application.Run(form);

            return 0;
        }

        private static int CleanProgramData()
        {
            var found  = ShadowScanner.Find(ShadowScanner.ProgramDataPluginsRoot);
            var failed = ShadowScanner.Remove(found);
            return failed.Count == 0 ? 0 : 1;
        }
    }
}
