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

        private const int CardWidth  = 400;
        private const int ColumnWidth = CardWidth + 16;

        private readonly TableLayoutPanel _cards = new TableLayoutPanel();
        private readonly Label _statusLeft       = new Label();
        private readonly Label _statusRight      = new Label();
        private readonly Panel _restartBar       = new Panel();

        private readonly FlowLayoutPanel _newsList = new FlowLayoutPanel();

        private Panel _pageModules;
        private Panel _pageNews;
        private Panel _tabModulesBtn;
        private Panel _tabNewsBtn;
        private Label _tabModulesLbl;
        private Label _tabNewsLbl;
        private Panel _tabModulesUnderline;
        private Panel _tabNewsUnderline;

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
            Shown += (s, e) => { Reload(); LoadNews(); };
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

            // Фиксированная сетка 2 колонки: юзер не хочет, чтобы карточки "прыгали"
            // (меняли число колонок) при изменении размера окна - в отличие от Robur-стора,
            // где сетка сама сжималась до 1 колонки. Ширина колонок задана Absolute,
            // не Percent: при нехватке места появляется горизонтальный скролл, карточки
            // не растягиваются и не переливаются.
            _cards.Dock = DockStyle.Fill;
            _cards.AutoScroll = true;
            _cards.Padding = new Padding(12);
            _cards.BackColor = PageBack;
            _cards.ColumnCount = 2;
            _cards.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ColumnWidth));
            _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ColumnWidth));

            // Вкладки - свои (панель+подпись+полоска снизу), не TabControl: у родного
            // компонента серый 3D-хром, который выбивался из плоского стиля Стора.
            // Тот же приём, что в Robur-сторе (AbrModules/StoreDialog.cs MakeTab).
            var tabStrip = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = CardBack };
            _tabModulesBtn = MakeTab("Модули",  out _tabModulesLbl, out _tabModulesUnderline);
            _tabNewsBtn    = MakeTab("Новости", out _tabNewsLbl,    out _tabNewsUnderline);
            _tabModulesBtn.Left = 12;
            _tabNewsBtn.Left    = _tabModulesBtn.Right;
            _tabModulesBtn.Click    += (s, e) => SwitchTab(true);
            _tabModulesLbl.Click    += (s, e) => SwitchTab(true);
            _tabNewsBtn.Click       += (s, e) => SwitchTab(false);
            _tabNewsLbl.Click       += (s, e) => SwitchTab(false);
            tabStrip.Controls.Add(_tabModulesBtn);
            tabStrip.Controls.Add(_tabNewsBtn);
            tabStrip.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ColorTranslator.FromHtml("#E1E4E8") });

            _pageModules = new Panel { Dock = DockStyle.Fill, BackColor = PageBack };
            _pageModules.Controls.Add(_cards);

            _pageNews = new Panel { Dock = DockStyle.Fill, BackColor = PageBack, Visible = false };
            BuildNewsPage(_pageNews);

            var pages = new Panel { Dock = DockStyle.Fill };
            pages.Controls.Add(_pageModules);
            pages.Controls.Add(_pageNews);

            Controls.Add(pages);
            Controls.Add(tabStrip);
            Controls.Add(_restartBar);
            Controls.Add(toolbar);
            Controls.Add(status);

            SwitchTab(true);
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

        /// <summary>Вкладка: панель с лейблом и 2px-полосой акцента снизу - тот же приём,
        /// что в Robur-сторе (родной TabControl даёт серый 3D-хром, плоскому стилю не подходит).</summary>
        private Panel MakeTab(string text, out Label lbl, out Panel underline)
        {
            var tab = new Panel { Width = 96, Height = 33, Top = 0, BackColor = CardBack, Cursor = Cursors.Hand };
            lbl = new Label
            {
                Text      = text,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = TextMuted,
                Cursor    = Cursors.Hand
            };
            underline = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = CardBack };
            tab.Controls.Add(lbl);
            tab.Controls.Add(underline);
            return tab;
        }

        private void SwitchTab(bool modules)
        {
            _pageModules.Visible = modules;
            _pageNews.Visible    = !modules;

            _tabModulesLbl.ForeColor = modules ? Accent : TextMuted;
            _tabModulesLbl.Font      = new Font(Font, modules ? FontStyle.Bold : FontStyle.Regular);
            _tabModulesUnderline.BackColor = modules ? Accent : CardBack;

            _tabNewsLbl.ForeColor = !modules ? Accent : TextMuted;
            _tabNewsLbl.Font      = new Font(Font, !modules ? FontStyle.Bold : FontStyle.Regular);
            _tabNewsUnderline.BackColor = !modules ? Accent : CardBack;
        }

        /// <summary>Тот же фид, что читает линейка Robur (общий news.json на abrmove.ru) -
        /// юзер явно решил не разграничивать новости по продукту, встраивается как есть.</summary>
        private void BuildNewsPage(Panel page)
        {
            _newsList.Dock = DockStyle.Fill;
            _newsList.AutoScroll = true;
            _newsList.WrapContents = false;
            _newsList.FlowDirection = FlowDirection.TopDown;
            _newsList.Padding = new Padding(12);
            _newsList.BackColor = PageBack;
            _newsList.Controls.Add(new Label
            {
                Text = "Загрузка...", AutoSize = true, ForeColor = TextMuted, Margin = new Padding(6)
            });
            page.Controls.Add(_newsList);
        }

        private void LoadNews()
        {
            var t = new Thread(() =>
            {
                List<NewsItem> items;
                try { items = NewsFeed.Fetch(new HttpFileDownloader()); }
                catch (Exception) { items = null; }

                try
                {
                    if (_newsList.IsDisposed) return;
                    _newsList.Invoke((MethodInvoker)(() => RebuildNews(items)));
                }
                catch (Exception) { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void RebuildNews(List<NewsItem> items)
        {
            _newsList.SuspendLayout();
            _newsList.Controls.Clear();

            if (items == null || items.Count == 0)
            {
                _newsList.Controls.Add(new Label
                {
                    Text      = items == null ? "Новости недоступны" : "Новостей пока нет",
                    AutoSize  = true,
                    ForeColor = TextMuted,
                    Margin    = new Padding(6)
                });
                _newsList.ResumeLayout();
                return;
            }

            foreach (var n in items)
                _newsList.Controls.Add(CreateNewsCard(n));

            _newsList.ResumeLayout();
        }

        private Control CreateNewsCard(NewsItem n)
        {
            const int width = 2 * ColumnWidth - 12;

            var card = new Panel
            {
                Width = width,
                BackColor = CardBack,
                Margin = new Padding(4, 4, 4, 8),
                BorderStyle = BorderStyle.FixedSingle
            };

            var date = new Label
            {
                Text = n.DisplayDate,
                Font = new Font("Consolas", 8f),
                ForeColor = TextMuted,
                Location = new Point(14, 10),
                AutoSize = true
            };
            card.Controls.Add(date);

            var title = new Label
            {
                Text = n.Title,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(14, 28),
                Size = new Size(width - 28, 20),
                AutoEllipsis = true
            };
            card.Controls.Add(title);

            int y = 52;
            if (!string.IsNullOrEmpty(n.Body))
            {
                var bodySize = TextRenderer.MeasureText(n.Body, Font,
                    new Size(width - 28, 0), TextFormatFlags.WordBreak);
                var body = new Label
                {
                    Text = n.Body,
                    ForeColor = TextMuted,
                    Location = new Point(14, y),
                    Size = new Size(width - 28, bodySize.Height)
                };
                card.Controls.Add(body);
                y += bodySize.Height + 8;
            }

            if (!string.IsNullOrEmpty(n.Url))
            {
                var link = new LinkLabel
                {
                    Text = "Подробнее →",
                    Font = new Font("Segoe UI", 8.5f),
                    LinkColor = Accent,
                    AutoSize = true,
                    Location = new Point(14, y)
                };
                var url = n.Url;
                link.LinkClicked += (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch (Exception) { }
                };
                card.Controls.Add(link);
                y += 20;
            }

            card.Height = y + 10;
            return card;
        }

        private void Reload()
        {
            _cards.Controls.Clear();
            _cards.RowStyles.Clear();
            _cards.RowCount = 0;
            Cursor = Cursors.WaitCursor;
            try
            {
                var entries   = _catalog.Load();
                var installed = InstalledScanner.Scan(AbrPaths.PluginsRoot);

                int index = 0;
                foreach (var entry in entries)
                {
                    var card = BuildCard(entry, ModuleStateResolver.Resolve(entry, installed, _hostYear),
                                         ModuleStateResolver.Find(installed, entry.Name));
                    _cards.Controls.Add(card, index % 2, index / 2);
                    index++;
                }

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
            var card = new Panel
            {
                Width       = CardWidth,
                BackColor   = CardBack,
                Margin      = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle
            };
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

            // Порог сворачивания - от РЕАЛЬНОЙ высоты текста в боксе, а не число символов:
            // у большинства описаний текст и так помещается в отведённые строки, символьный
            // порог давал "Подробнее" там, где разворачивать было нечего.
            const int descWidth = 372;
            var lineHeight = TextRenderer.MeasureText("Ай", Font).Height;
            int collapsedHeight = lineHeight * 3;

            var fullSize = TextRenderer.MeasureText(entry.Description, Font,
                new Size(descWidth, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

            bool expandable = fullSize.Height > collapsedHeight + 2;
            bool expanded = expandable && _expanded.Contains(entry.Name);
            int descHeight = expandable && !expanded ? collapsedHeight : Math.Min(fullSize.Height, lineHeight * 8);

            card.Controls.Add(new Label
            {
                Text = entry.Description,
                ForeColor = TextMuted,
                Location = new Point(14, y),
                Size = new Size(descWidth, descHeight),
                AutoEllipsis = expandable && !expanded
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
