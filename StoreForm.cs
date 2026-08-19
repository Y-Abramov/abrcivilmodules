using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Abr.Civil.Sdk;
using AbrCivil.Modules.Core;

namespace AbrCivil.Modules
{
    internal class StoreForm : Form
    {
        private static readonly Color Accent     = ColorTranslator.FromHtml("#0891B2");
        private static readonly Color CardBack   = Color.White;
        private static readonly Color PageBack   = ColorTranslator.FromHtml("#F5F7F9");
        private static readonly Color TextMuted  = ColorTranslator.FromHtml("#5B6770");

        private readonly TabControl _tabs        = new TabControl();
        private readonly FlowLayoutPanel _cards  = new FlowLayoutPanel();
        private readonly Label _statusLeft       = new Label();
        private readonly Label _statusRight      = new Label();
        private readonly Panel _restartBar       = new Panel();

        private readonly CatalogClient _catalog  = new CatalogClient(new HttpFileDownloader());
        private readonly BundleInstaller _installer =
            new BundleInstaller(AbrPaths.PluginsRoot, Path.Combine(AbrPaths.DataRoot, "tmp"), new HttpFileDownloader());

        private readonly int _hostYear;

        /// <summary>Карточки, у которых юзер раскрыл полное описание - переживает Reload
        /// в пределах одного открытия окна (пересоздаётся при следующем открытии Стора).</summary>
        private readonly HashSet<string> _expanded = new HashSet<string>();

        public StoreForm()
        {
            Text            = "Библиотека модулей";   // префикс «ABR | » - только в AboutDialog
            ClientSize      = new Size(880, 620);
            StartPosition   = FormStartPosition.CenterParent;
            MinimumSize     = new Size(700, 480);
            BackColor       = PageBack;
            Font            = new Font("Segoe UI", 9f);
            Icon            = AbrIcon.Create();

            _hostYear = SeriesDetector.YearFromAcadVersion(
                Autodesk.AutoCAD.ApplicationServices.Core.Application.Version);

            BuildLayout();
            Shown += (s, e) => Reload();
        }

        private void BuildLayout()
        {
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = CardBack };
            var btnFromFile = MakeButton("Из файла...", false);
            btnFromFile.Location = new Point(12, 9);
            btnFromFile.Click += (s, e) => InstallFromFile();
            toolbar.Controls.Add(btnFromFile);

            var btnRefresh = MakeButton("Обновить список", false);
            btnRefresh.Location = new Point(btnFromFile.Right + 8, 9);
            btnRefresh.Click += (s, e) => Reload();
            toolbar.Controls.Add(btnRefresh);

            _restartBar.Dock = DockStyle.Top;
            _restartBar.Height = 32;
            _restartBar.BackColor = ColorTranslator.FromHtml("#FFF4CE");
            _restartBar.Visible = false;
            _restartBar.Controls.Add(new Label
            {
                Text = "Нужен перезапуск Civil 3D - изменения вступят в силу после него.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            });

            var status = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = CardBack };
            _statusLeft.SetBounds(12, 10, 520, 18);
            _statusLeft.AutoEllipsis = true;
            _statusLeft.ForeColor = TextMuted;
            _statusRight.SetBounds(560, 10, 300, 18);
            _statusRight.AutoEllipsis = true;
            _statusRight.TextAlign = ContentAlignment.MiddleRight;
            _statusRight.ForeColor = TextMuted;
            _statusRight.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            status.Controls.Add(_statusLeft);
            status.Controls.Add(_statusRight);

            _cards.Dock = DockStyle.Fill;
            _cards.AutoScroll = true;
            _cards.Padding = new Padding(12);
            _cards.BackColor = PageBack;

            var tabModules = new TabPage("Модули") { BackColor = PageBack };
            tabModules.Controls.Add(_cards);

            var tabNews = new TabPage("Новости") { BackColor = PageBack };
            tabNews.Controls.Add(new Label
            {
                Text      = "Новости модулей ABR | CIVIL пока не подключены.",
                Dock      = DockStyle.Top,
                Height    = 40,
                Padding   = new Padding(16, 16, 16, 0),
                ForeColor = TextMuted
            });

            _tabs.Dock = DockStyle.Fill;
            _tabs.TabPages.Add(tabModules);
            _tabs.TabPages.Add(tabNews);

            Controls.Add(_tabs);
            Controls.Add(_restartBar);
            Controls.Add(toolbar);
            Controls.Add(status);
        }

        /// <summary>Родные кнопки Windows, без owner-draw: правило линейки.</summary>
        private static Button MakeButton(string text, bool primary)
        {
            var b = new Button { Text = text, AutoSize = false, Size = new Size(140, 30) };
            if (primary)
            {
                b.BackColor = Accent;
                b.ForeColor = Color.White;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
            }
            return b;
        }

        private void Reload()
        {
            _cards.Controls.Clear();
            Cursor = Cursors.WaitCursor;
            try
            {
                var entries   = _catalog.Load();
                var installed = InstalledScanner.Scan(AbrPaths.PluginsRoot);

                foreach (var entry in entries)
                    _cards.Controls.Add(BuildCard(entry, ModuleStateResolver.Resolve(entry, installed, _hostYear),
                                                  ModuleStateResolver.Find(installed, entry.Name)));

                _statusLeft.Text = _catalog.LastLoadWasOffline
                    ? "Каталог недоступен, показан сохранённый список"
                    : "Каталог загружен";
                _statusRight.Text = "Модулей: " + entries.Count + "   Civil 3D " + (_hostYear == 0 ? "?" : _hostYear.ToString());
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private Control BuildCard(ModuleEntry entry, ModuleState state, InstalledBundle installed)
        {
            var card = new Panel { Width = 400, BackColor = CardBack, Margin = new Padding(8) };
            PopulateCard(card, entry, state, installed);
            return card;
        }

        private void PopulateCard(Panel card, ModuleEntry entry, ModuleState state, InstalledBundle installed)
        {
            card.Controls.Clear();
            int y = 12;

            card.Controls.Add(new Label
            {
                Text = entry.Title,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(14, y),
                AutoSize = true
            });
            y += 26;

            card.Controls.Add(new Label
            {
                Text = "Версия " + entry.Version + (installed == null ? "" : "   установлена " + installed.Version),
                ForeColor = TextMuted,
                Location = new Point(14, y),
                AutoSize = true
            });
            y += 24;

            bool expandable = entry.Description.Length > 90;
            bool expanded = expandable && _expanded.Contains(entry.Name);
            int descHeight = expanded ? 76 : 40;

            card.Controls.Add(new Label
            {
                Text = entry.Description,
                ForeColor = TextMuted,
                Location = new Point(14, y),
                Size = new Size(372, descHeight),
                AutoEllipsis = !expanded
            });
            y += descHeight;

            if (expandable)
            {
                var toggle = new LinkLabel
                {
                    Text = expanded ? "Свернуть ▲" : "Подробнее ▼",
                    Font = new Font("Segoe UI", 8f),
                    LinkColor = Accent,
                    AutoSize = true,
                    Location = new Point(14, y)
                };
                toggle.LinkClicked += (s, e) =>
                {
                    if (expanded) _expanded.Remove(entry.Name); else _expanded.Add(entry.Name);
                    PopulateCard(card, entry, state, installed);
                };
                card.Controls.Add(toggle);
                y += 18;
            }

            y += 4;
            card.Controls.Add(new Label
            {
                Text = StateChip(state),
                ForeColor = ChipColor(state),
                Location = new Point(14, y),
                AutoSize = true
            });

            var downloads = new Label
            {
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8f),
                Location = new Point(246, y),
                Size = new Size(140, 16),
                TextAlign = ContentAlignment.MiddleRight,
                Text = ""
            };
            card.Controls.Add(downloads);
            FetchDownloadCount(entry, downloads);
            y += 22;

            string secondaryText = SecondaryText(state);
            if (secondaryText != null)
            {
                var secondary = new LinkLabel
                {
                    Text = secondaryText,
                    Font = new Font("Segoe UI", 8f),
                    LinkColor = TextMuted,
                    AutoSize = true,
                    Location = new Point(14, y)
                };
                secondary.LinkClicked += (s, e) => RunSecondaryAction(entry, state, installed);
                card.Controls.Add(secondary);
                y += 20;
            }

            y += 6;

            var help = MakeGhostButton("Инструкция");
            help.Location = new Point(14, y);
            help.Click += (s, e) => OpenHelp(entry);
            card.Controls.Add(help);

            var action = MakeButton(ActionText(state), true);
            action.Size = new Size(140, 28);
            action.Location = new Point(246, y);
            action.Enabled = state != ModuleState.Incompatible;
            action.Click += (s, e) => RunAction(entry, state, installed);
            card.Controls.Add(action);

            var moreVersions = new ContextMenuStrip();
            moreVersions.Items.Add("Другие версии...", null, (s, e) => ShowVersionPicker(entry, installed));
            card.ContextMenuStrip = moreVersions;

            y += 34;
            card.Height = y;
        }

        private static Button MakeGhostButton(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = false,
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Accent,
                BackColor = Color.White
            };
            b.FlatAppearance.BorderColor = Accent;
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        private void OpenHelp(ModuleEntry entry)
        {
            var url = string.IsNullOrWhiteSpace(entry.HelpUrl) ? AbrBrand.WebsiteUrl : entry.HelpUrl;
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception) { }
        }

        private void FetchDownloadCount(ModuleEntry entry, Label target)
        {
            string owner, repo;
            if (!GitHubReleases.TryParseRepo(entry.BundleUrl, out owner, out repo)) return;

            var t = new Thread(() =>
            {
                int count;
                try { count = GitHubReleases.FetchDownloadCount(owner, repo, entry.Version); }
                catch (Exception) { return; }

                try
                {
                    if (target.IsDisposed) return;
                    target.Invoke((MethodInvoker)(() =>
                    {
                        if (!target.IsDisposed) target.Text = "Скачиваний: " + count;
                    }));
                }
                catch (Exception) { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void ShowVersionPicker(ModuleEntry entry, InstalledBundle installed)
        {
            string owner, repo;
            if (!GitHubReleases.TryParseRepo(entry.BundleUrl, out owner, out repo))
            {
                MessageBox.Show(this, "Не удалось определить репозиторий модуля.", "Библиотека модулей",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<ReleaseInfo> releases;
            Cursor = Cursors.WaitCursor;
            try { releases = GitHubReleases.FetchAll(owner, repo); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось получить список версий:\r\n" + ex.Message, "Библиотека модулей",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally { Cursor = Cursors.Default; }

            if (releases.Count == 0)
            {
                MessageBox.Show(this, "Релизы не найдены.", "Библиотека модулей",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var picker = new VersionPickerDialog(entry.Title, releases))
            {
                if (picker.ShowDialog(this) != DialogResult.OK || picker.Selected == null) return;

                if (!ConfirmDialog.Ask(this, "Библиотека модулей",
                        "Установить версию " + picker.Selected.TagName + " модуля «" + entry.Title + "»?\r\n\r\n" +
                        "Контрольная сумма для сторонней версии не проверяется.", "Установить"))
                    return;

                try
                {
                    Cursor = Cursors.WaitCursor;
                    _installer.Install(entry.Name, picker.Selected.AssetUrl, "");
                    _restartBar.Visible = true;
                    Reload();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Библиотека модулей",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally { Cursor = Cursors.Default; }
            }
        }

        private static string SecondaryText(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Installed:
                case ModuleState.UpdateAvailable:
                    return "Отключить";
                case ModuleState.Disabled:
                    return "Удалить";
                default:
                    return null;
            }
        }

        private void RunSecondaryAction(ModuleEntry entry, ModuleState state, InstalledBundle installed)
        {
            if (installed == null) return;

            if (state == ModuleState.Disabled)
            {
                if (!ConfirmDialog.Ask(this, "Библиотека модулей",
                        "Удалить модуль «" + entry.Title + "» полностью?\r\n\r\nФайлы и настройки будут удалены.",
                        "Удалить"))
                    return;

                RunInstallerAction(() => _installer.Uninstall(installed.Directory));
                return;
            }

            if (!ConfirmDialog.Ask(this, "Библиотека модулей",
                    "Отключить модуль «" + entry.Title + "»?\r\n\r\n" +
                    "Файлы и настройки останутся на диске, модуль перестанет загружаться " +
                    "после перезапуска Civil 3D. В любой момент можно включить обратно.",
                    "Отключить"))
                return;

            RunInstallerAction(() => BundleInstaller.SetEnabled(installed.Directory, false));
        }

        private string StateChip(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Installed:       return "Установлен";
                case ModuleState.UpdateAvailable: return "Есть обновление";
                case ModuleState.Incompatible:    return "Несовместим с Civil 3D " + _hostYear;
                case ModuleState.NotInCatalog:    return "Не в каталоге";
                case ModuleState.Disabled:        return "Отключен";
                default:                          return "Не установлен";
            }
        }

        private Color ChipColor(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Incompatible: return Color.Firebrick;
                case ModuleState.Disabled:     return TextMuted;
                default:                       return Accent;
            }
        }

        private static string ActionText(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Installed:       return "Удалить";
                case ModuleState.UpdateAvailable: return "Обновить";
                case ModuleState.Disabled:        return "Активировать";
                default:                          return "Установить";
            }
        }

        private void RunAction(ModuleEntry entry, ModuleState state, InstalledBundle installed)
        {
            if (state == ModuleState.Disabled && installed != null)
            {
                if (!ConfirmDialog.Ask(this, "Библиотека модулей",
                        "Включить модуль «" + entry.Title + "» обратно?\r\n\r\n" +
                        "Начнёт загружаться после перезапуска Civil 3D.", "Активировать"))
                    return;

                RunInstallerAction(() => BundleInstaller.SetEnabled(installed.Directory, true));
                return;
            }

            bool uninstalling = state == ModuleState.Installed && installed != null;
            string verb   = uninstalling ? "Удалить" : (state == ModuleState.UpdateAvailable ? "Обновить" : "Установить");
            string message = uninstalling
                ? "Удалить модуль «" + entry.Title + "»?\r\n\r\nФайлы и настройки будут удалены."
                : (state == ModuleState.UpdateAvailable
                    ? "Обновить модуль «" + entry.Title + "» до версии " + entry.Version + "?"
                    : "Установить модуль «" + entry.Title + "» версии " + entry.Version + "?");

            if (!ConfirmDialog.Ask(this, "Библиотека модулей", message, verb)) return;

            RunInstallerAction(() =>
            {
                if (uninstalling) _installer.Uninstall(installed.Directory);
                else _installer.Install(entry.Name, entry.BundleUrl, entry.Sha256);
            });
        }

        private void RunInstallerAction(Action action)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                action();
                _restartBar.Visible = true;
                Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Библиотека модулей",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void InstallFromFile()
        {
            using (var dialog = new OpenFileDialog { Filter = "Пакет модуля (*.zip;*.bundle)|*.zip;*.bundle" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                if (!ConfirmDialog.Ask(this, "Библиотека модулей",
                        "Установить модуль из файла:\r\n" + dialog.FileName + "?", "Установить"))
                    return;

                RunInstallerAction(() => _installer.InstallFromArchive(dialog.FileName));
            }
        }
    }
}
