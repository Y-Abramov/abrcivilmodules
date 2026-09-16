using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AbrCivil.Modules.Core;

namespace AbrCivil.Setup
{
    /// <summary>
    /// Страница "Удаление модулей": вторая ветка с шага 1. Каталог не нужен - список строится
    /// по диску (RemovalPlanner), итог каждой строки - по состоянию диска (RemovalRunner).
    /// </summary>
    internal partial class SetupForm
    {
        private readonly Panel _page4 = new Panel { Dock = DockStyle.Fill, BackColor = PageBack, Visible = false };

        private List<RemovalItem> _removalItems = new List<RemovalItem>();
        private readonly Dictionary<string, CheckBox> _removeBoxes = new Dictionary<string, CheckBox>();
        private readonly Dictionary<string, Label> _removeStates = new Dictionary<string, Label>();
        private readonly ToolTip _removeTips = new ToolTip();

        private Label _libraryNote, _shadowNote, _removeSummary;
        private CheckBox _cleanCache;
        private Button _shadowButton, _btnRemoveAll, _btnRemoveNone, _btnRemoveBack, _btnRemove, _btnRemoveHome, _btnRemoveClose;

        private bool _removing;
        private bool _removalDone;

        private void ShowRemovalPage()
        {
            _removalItems = RemovalPlanner.Build(InstalledScanner.Scan(AbrPaths.PluginsRoot), _catalog);
            BuildRemovalPage();

            _page1.Visible = false;
            _page2.Visible = false;
            _page3.Visible = false;
            _page4.Visible = true;
        }

        private void BuildRemovalPage()
        {
            _page4.Controls.Clear();
            _removeBoxes.Clear();
            _removeStates.Clear();
            _removing = false;
            _removalDone = false;

            var title = new Label
            {
                Text = "Удаление модулей",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 20)
            };

            var list = new Panel
            {
                Location = new Point(24, 60),
                Size = new Size(510, 228),
                BackColor = CardBack,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            int y = 8;
            foreach (var item in _removalItems)
            {
                var box = new CheckBox
                {
                    Text = item.Title,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Checked = false,
                    AutoSize = false,
                    Size = new Size(300, 20),
                    Location = new Point(10, y)
                };
                box.CheckedChanged += (s, e) => UpdateRemovalControls();

                var state = new Label
                {
                    Text = item.IsDisabled ? item.Version + " отключён" : item.Version,
                    ForeColor = TextMuted,
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoEllipsis = true,
                    AutoSize = false,
                    Size = new Size(170, 20),
                    Location = new Point(316, y)
                };

                list.Controls.Add(box);
                list.Controls.Add(state);
                _removeBoxes[item.Directory] = box;
                _removeStates[item.Directory] = state;

                y += 28;
            }

            _libraryNote = new Label
            {
                Text = "! Без Библиотеки не будет обновлений. Остальные модули работают.",
                ForeColor = Warn,
                AutoSize = false,
                Size = new Size(510, 18),
                Location = new Point(24, 294)
            };

            _shadowNote = new Label
            {
                ForeColor = Warn,
                AutoSize = false,
                Size = new Size(400, 26),
                Location = new Point(24, 318),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _shadowButton = MakeButton("Убрать", false);
            _shadowButton.Size = new Size(100, 26);
            _shadowButton.Location = new Point(434, 318);
            _shadowButton.Click += (s, e) =>
            {
                if (TryCleanShadows())
                    _shadows = ShadowScanner.Find(ShadowScanner.ProgramDataPluginsRoot);
                UpdateRemovalControls();
            };

            _cleanCache = new CheckBox
            {
                Text = "Очистить кэш загрузок удаляемых модулей",
                Checked = true,
                AutoSize = false,
                Size = new Size(510, 22),
                Location = new Point(24, 352)
            };

            _removeSummary = new Label
            {
                AutoSize = false,
                Size = new Size(510, 38),
                Location = new Point(24, 350),
                Visible = false
            };

            _btnRemoveAll = MakeButton("Все", false);
            _btnRemoveAll.Size = new Size(80, 28);
            _btnRemoveAll.Location = new Point(24, 390);
            _btnRemoveAll.Click += (s, e) => SetAllRemoval(true);

            _btnRemoveNone = MakeButton("Ничего", false);
            _btnRemoveNone.Size = new Size(80, 28);
            _btnRemoveNone.Location = new Point(110, 390);
            _btnRemoveNone.Click += (s, e) => SetAllRemoval(false);

            _btnRemoveBack = MakeButton("< Назад", false);
            _btnRemoveBack.Location = new Point(270, 390);
            _btnRemoveBack.Click += (s, e) =>
            {
                _page4.Visible = false;
                _page1.Visible = true;
            };

            _btnRemove = MakeButton("Удалить", true);
            _btnRemove.Location = new Point(404, 390);
            _btnRemove.Click += (s, e) => StartRemoval();

            _btnRemoveHome = MakeButton("< К началу", false);
            _btnRemoveHome.Location = new Point(270, 390);
            _btnRemoveHome.Visible = false;
            _btnRemoveHome.Click += (s, e) =>
            {
                _page4.Visible = false;
                RunChecks();
                _page1.Visible = true;
            };

            _btnRemoveClose = MakeButton("Закрыть", true);
            _btnRemoveClose.Location = new Point(404, 390);
            _btnRemoveClose.Visible = false;
            _btnRemoveClose.Click += (s, e) => Close();

            _page4.Controls.Add(title);
            _page4.Controls.Add(list);
            _page4.Controls.Add(_libraryNote);
            _page4.Controls.Add(_shadowNote);
            _page4.Controls.Add(_shadowButton);
            _page4.Controls.Add(_cleanCache);
            _page4.Controls.Add(_removeSummary);
            _page4.Controls.Add(_btnRemoveAll);
            _page4.Controls.Add(_btnRemoveNone);
            _page4.Controls.Add(_btnRemoveBack);
            _page4.Controls.Add(_btnRemove);
            _page4.Controls.Add(_btnRemoveHome);
            _page4.Controls.Add(_btnRemoveClose);

            UpdateRemovalControls();
        }

        private List<RemovalItem> SelectedForRemoval()
        {
            var result = new List<RemovalItem>();
            foreach (var item in _removalItems)
            {
                CheckBox box;
                if (_removeBoxes.TryGetValue(item.Directory, out box) && box.Checked) result.Add(item);
            }
            return result;
        }

        private void SetAllRemoval(bool value)
        {
            foreach (var box in _removeBoxes.Values) box.Checked = value;
            UpdateRemovalControls();
        }

        private void UpdateRemovalControls()
        {
            if (_btnRemove == null) return;

            var selected = SelectedForRemoval();
            var libraryChecked = selected.Exists(i => i.IsLibrary);
            var otherLeft = _removalItems.Exists(i => !i.IsLibrary && !selected.Contains(i));

            _libraryNote.Visible = !_removalDone && libraryChecked && otherLeft;

            _shadowNote.Text = "! В ProgramData старые копии (" + _shadows.Count + "), Civil 3D их загрузит";
            _shadowNote.Visible = _shadows.Count > 0;
            _shadowButton.Visible = _shadows.Count > 0;
            _shadowButton.Enabled = !_removing;

            _btnRemove.Enabled = !_removing && !_removalDone && selected.Count > 0;
        }

        private void StartRemoval()
        {
            var selected = SelectedForRemoval();
            if (selected.Count == 0) return;

            if (RegistryProbe.IsAcadRunning())
            {
                MessageBox.Show(this, "Civil 3D запущен. Закройте его и повторите.",
                    "Удаление модулей", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(this,
                "Удалить модулей: " + selected.Count + "? Настройки и пресеты сохранятся.",
                "Удаление модулей", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _removing = true;
            ControlBox = false;
            foreach (var box in _removeBoxes.Values) box.Enabled = false;
            _cleanCache.Enabled = false;
            _btnRemoveAll.Enabled = false;
            _btnRemoveNone.Enabled = false;
            _btnRemoveBack.Enabled = false;
            UpdateRemovalControls();

            var cleanCache = _cleanCache.Checked;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var installer = new BundleInstaller(
                AbrPaths.PluginsRoot, Path.Combine(AbrPaths.DataRoot, "tmp"), _downloader);

            var thread = new Thread(() =>
            {
                int removed = 0;
                var failures = new List<string>();

                foreach (var item in selected)
                {
                    SetRemovalRow(item, "удаление...", TextMuted, null);
                    var result = RemovalRunner.Remove(item, cleanCache, installer, localAppData);

                    switch (result.Outcome)
                    {
                        case RemovalOutcome.Removed:
                            SetRemovalRow(item, "удалён", Good, null);
                            removed++;
                            break;

                        case RemovalOutcome.RemovedWithLeftovers:
                            SetRemovalRow(item, "удалён, остались файлы", Warn, result.Detail);
                            removed++;
                            break;

                        default:
                            SetRemovalRow(item, "ошибка: " + result.Detail, Bad, result.Detail);
                            failures.Add(item.Title);
                            break;
                    }
                }

                var total = selected.Count;
                BeginInvoke((MethodInvoker)(() => FinishRemoval(removed, total, failures)));
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void SetRemovalRow(RemovalItem item, string text, Color color, string tip)
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                Label row;
                if (!_removeStates.TryGetValue(item.Directory, out row)) return;
                row.Text = text;
                row.ForeColor = color;
                _removeTips.SetToolTip(row, tip ?? "");
            }));
        }

        private void FinishRemoval(int removed, int total, List<string> failures)
        {
            _removing = false;
            _removalDone = true;
            ControlBox = true;

            var text = "Удалено " + removed + " из " + total + ".";
            if (failures.Count > 0) text += " Не удалось: " + string.Join(", ", failures.ToArray()) + ".";
            if (removed > 0) text += "\r\nИзменения вступят в силу при следующем запуске Civil 3D.";

            _removeSummary.ForeColor = failures.Count > 0 ? Bad : Good;
            _removeSummary.Text = text;
            _removeSummary.Visible = true;
            _cleanCache.Visible = false;

            _btnRemoveAll.Visible = false;
            _btnRemoveNone.Visible = false;
            _btnRemoveBack.Visible = false;
            _btnRemove.Visible = false;
            _btnRemoveHome.Visible = true;
            _btnRemoveClose.Visible = true;

            UpdateRemovalControls();
        }
    }
}
