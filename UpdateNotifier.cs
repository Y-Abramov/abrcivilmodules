using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using AbrCivil.Modules.Core;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace AbrCivil.Modules
{
    /// <summary>
    /// Фоновая проверка событий при каждом старте Civil 3D: обновления + новые модули +
    /// новости. Тост показывается, пока есть события - обновления напоминают каждый
    /// запуск, новости и «новые модули» метятся прочитанными ПРИ ПОКАЗЕ тоста (тот же
    /// принцип, что в Robur - см. AbrModules/UpdateNotifier.cs). «Не напоминать» глушит
    /// навсегда через NotifyState.
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

            List<NewsItem> news = null;
            try { news = NewsFeed.Fetch(new HttpFileDownloader()); }
            catch (Exception) { }

            var installed = InstalledScanner.Scan(AbrPaths.PluginsRoot);
            int hostYear = SeriesDetector.YearFromAcadVersion(AcApp.Version);

            NotifyState.EnsureSeeded(modules, news);

            int updates = 0;
            foreach (var m in modules)
                if (ModuleStateResolver.Resolve(m, installed, hostYear) == ModuleState.UpdateAvailable)
                    updates++;

            var unread = NotifyState.GetUnread(modules, installed, news);

            int total = updates + unread.ModuleNames.Count + unread.NewsIds.Count;
            if (total == 0) return;

            // «Прочитано при показе» - метим ДО показа, снапшот несёт непрочитанное
            // в StoreForm, чтобы чип «Новый» дожил до открытия окна.
            NotifyState.MarkSeen(unread);

            var message = BuildMessage(updates, unread.ModuleNames.Count, unread.NewsIds.Count);
            var tab = unread.NewsIds.Count > 0 ? StoreTab.News : StoreTab.Modules;
            ShowToast(message, tab, unread);
        }

        private static string BuildMessage(int updates, int newModules, int news)
        {
            var parts = new List<string>();
            if (updates > 0)    parts.Add("Обновления: " + updates);
            if (newModules > 0) parts.Add("Новые модули: " + newModules);
            if (news > 0)       parts.Add("Новости: " + news);
            return string.Join(" · ", parts);
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
        private static void ShowToast(string message, StoreTab tab, UnreadSnapshot unread)
        {
            var t = new Thread(() =>
            {
                try
                {
                    using (var form = new UpdateToastForm(message, () => OpenStore(tab, unread), NotifyState.Mute))
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

        private static void OpenStore(StoreTab tab, UnreadSnapshot unread)
        {
            using (var form = new StoreForm(tab, unread))
                form.ShowDialog();
        }
    }
}
