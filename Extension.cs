using System;
using System.IO;
using Abr.Civil.Sdk;
using AbrCivil.Modules.Core;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(AbrCivil.Modules.ModulesExtension))]
[assembly: CommandClass(typeof(AbrCivil.Modules.ModulesExtension))]

namespace AbrCivil.Modules
{
    public class ModulesExtension : IExtensionApplication
    {
        public void Initialize()
        {
            // Только лёгкое: дочистка *.old и лента. Ни сети, ни диалогов.
            SafeCleanup();
            AbrRibbon.WhenReady(BuildRibbon);
        }

        public void Terminate() { }

        private static void SafeCleanup()
        {
            try
            {
                var root = AbrPaths.PluginsRoot;
                if (!Directory.Exists(root)) return;

                var installer = new BundleInstaller(root, Path.Combine(AbrPaths.DataRoot, "tmp"), new HttpFileDownloader());
                installer.CleanupAll(Directory.GetDirectories(root, "*.bundle"));
            }
            catch (System.Exception)
            {
                // Дочистка не должна мешать загрузке модуля.
            }
        }

        private static void BuildRibbon()
        {
            var tab = AbrRibbon.EnsureTab();
            if (tab == null) return;

            var panel = AbrRibbon.EnsurePanel(tab, "ABR_CIVIL_MODULES_PANEL", "Модули");
            panel.Source.Items.Add(AbrRibbon.MakeButton("Библиотека\nмодулей", "ABRSTORE", "abr_store"));
            panel.Source.Items.Add(AbrRibbon.MakeButton("О модуле", "ABRABOUT", "abr_about"));
            panel.Source.Items.Add(AbrRibbon.MakeButton("Сайт", "ABRSITE", "abr_website"));
        }

        [CommandMethod("ABRSTORE")]
        public void OpenStore()
        {
            using (var form = new StoreForm())
                form.ShowDialog();
        }

        [CommandMethod("ABRABOUT")]
        public void About()
        {
            using (var dialog = new AboutDialog())
                dialog.ShowDialog();
        }

        [CommandMethod("ABRSITE")]
        public void Site()
        {
            // UseShellExecute обязателен: на .NET 8 Process.Start(url) без него бросает Win32Exception.
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(AbrBrand.WebsiteUrl) { UseShellExecute = true });
            }
            catch (System.Exception) { }
        }
    }
}
