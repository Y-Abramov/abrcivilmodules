using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using AbrCivil.Modules.Core;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace AbrCivil.Modules
{
    /// <summary>
    /// Фоновая проверка обновлений при каждом старте Civil 3D. Тост показывается,
    /// пока есть обновления - не однократно на версию (тот же принцип, что в Robur).
    /// «Не напоминать» глушит навсегда через NotifyState, обновления не отслеживаются
    /// «прочитано/непрочитано» - только счётчик.
    /// </summary>
    internal static class UpdateNotifier
    {
        private const int DocPollMs   = 2000;
        private const int SettleMs    = 3000;

        public static void Start()
        {
            try
            {
                // Выделенный поток: ожидание документа не должно занимать пул задач Civil 3D.
                var t = new Thread(() => { try { Run(); } catch (Exception) { } });
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception)
            {
            }
        }

        private static void Run()
        {
            if (NotifyState.IsMuted()) return;

            WaitForDocument();

            List<ModuleEntry> modules;
            try
            {
                modules = new CatalogClient(new HttpFileDownloader()).Load();
            }
            catch (Exception)
            {
                return;
            }
            if (modules == null || modules.Count == 0) return;

            var installed = InstalledScanner.Scan(AbrPaths.PluginsRoot);
            int hostYear = SeriesDetector.YearFromAcadVersion(AcApp.Version);

            int updates = 0;
            foreach (var m in modules)
                if (ModuleStateResolver.Resolve(m, installed, hostYear) == ModuleState.UpdateAvailable)
                    updates++;

            if (updates == 0) return;

            ShowToast("Обновления: " + updates);
        }

        /// <summary>
        /// Ждёт активный документ (в Robur ждали открытия проекта - у Civil 3D
        /// прямого аналога нет, ближайшее равнозначное состояние - открытый чертёж).
        /// Таймаута нет намеренно: поток фоновый, ничему не мешает.
        /// </summary>
        private static void WaitForDocument()
        {
            while (true)
            {
                try
                {
                    if (AcApp.DocumentManager.MdiActiveDocument != null) break;
                }
                catch (Exception)
                {
                }
                Thread.Sleep(DocPollMs);
            }
            Thread.Sleep(SettleMs);
        }

        /// <summary>Тост на собственном STA-потоке со своим message loop - поток Civil 3D не нужен.</summary>
        private static void ShowToast(string message)
        {
            var t = new Thread(() =>
            {
                try
                {
                    using (var form = new UpdateToastForm(message, OpenStore, NotifyState.Mute))
                        Application.Run(form);
                }
                catch (Exception)
                {
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        private static void OpenStore()
        {
            using (var form = new StoreForm())
                form.ShowDialog();
        }
    }
}
