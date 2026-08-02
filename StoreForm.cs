using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        private readonly FlowLayoutPanel _cards  = new FlowLayoutPanel();
        private readonly Label _statusLeft       = new Label();
        private readonly Label _statusRight      = new Label();
        private readonly Panel _restartBar       = new Panel();

        private readonly CatalogClient _catalog  = new CatalogClient(new HttpFileDownloader());
        private readonly BundleInstaller _installer =
            new BundleInstaller(AbrPaths.PluginsRoot, Path.Combine(AbrPaths.DataRoot, "tmp"), new HttpFileDownloader());

        private readonly int _hostYear;

        public StoreForm()
        {
            Text            = "Библиотека модулей";   // префикс «ABR | » - только в AboutDialog
            ClientSize      = new Size(880, 600);
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

            Controls.Add(_cards);
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
            var card = new Panel { Size = new Size(400, 150), BackColor = CardBack, Margin = new Padding(8) };

            card.Controls.Add(new Label
            {
                Text = entry.Title,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(14, 12),
                AutoSize = true
            });

            card.Controls.Add(new Label
            {
                Text = "Версия " + entry.Version + (installed == null ? "" : "   установлена " + installed.Version),
                ForeColor = TextMuted,
                Location = new Point(14, 38),
                AutoSize = true
            });

            card.Controls.Add(new Label
            {
                Text = entry.Description,
                ForeColor = TextMuted,
                Location = new Point(14, 62),
                Size = new Size(372, 40)
            });

            card.Controls.Add(new Label
            {
                Text = StateChip(state),
                ForeColor = state == ModuleState.Incompatible ? Color.Firebrick : Accent,
                Location = new Point(14, 112),
                AutoSize = true
            });

            var action = MakeButton(ActionText(state), true);
            action.Location = new Point(246, 106);
            action.Enabled = state != ModuleState.Incompatible;
            action.Click += (s, e) => RunAction(entry, state, installed);
            card.Controls.Add(action);

            return card;
        }

        private string StateChip(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Installed:       return "Установлен";
                case ModuleState.UpdateAvailable: return "Есть обновление";
                case ModuleState.Incompatible:    return "Несовместим с Civil 3D " + _hostYear;
                case ModuleState.NotInCatalog:    return "Не в каталоге";
                default:                          return "Не установлен";
            }
        }

        private static string ActionText(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Installed:       return "Удалить";
                case ModuleState.UpdateAvailable: return "Обновить";
                default:                          return "Установить";
            }
        }

        private void RunAction(ModuleEntry entry, ModuleState state, InstalledBundle installed)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                if (state == ModuleState.Installed && installed != null)
                    _installer.Uninstall(installed.Directory);
                else
                    _installer.Install(entry.Name, entry.BundleUrl, entry.Sha256);

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

                try
                {
                    _installer.InstallFromArchive(dialog.FileName);
                    _restartBar.Visible = true;
                    Reload();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Библиотека модулей",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
