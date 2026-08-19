using System;
using System.Windows.Forms;

namespace AbrCivil.Setup
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var form = new SetupForm())
                Application.Run(form);

            return 0;
        }
    }
}
