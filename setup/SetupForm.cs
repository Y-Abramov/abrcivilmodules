using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Abr.Civil.Sdk;
using AbrCivil.Modules.Core;

namespace AbrCivil.Setup
{
    /// <summary>Мастер установки: три страницы в одном окне.
    /// Стиль 1:1 от StoreForm - акцент #0891B2, родные кнопки без owner-draw.</summary>
    internal class SetupForm : Form
    {
        internal static readonly Color Accent    = ColorTranslator.FromHtml("#0891B2");
        internal static readonly Color PageBack  = ColorTranslator.FromHtml("#F5F7F9");
        internal static readonly Color CardBack  = Color.White;
        internal static readonly Color TextMuted = ColorTranslator.FromHtml("#5B6770");
        internal static readonly Color Warn      = ColorTranslator.FromHtml("#B8860B");
        internal static readonly Color Bad       = Color.Firebrick;
        internal static readonly Color Good      = ColorTranslator.FromHtml("#2E7D32");

        private readonly Panel _page1 = new Panel { Dock = DockStyle.Fill, BackColor = PageBack };
        private readonly Panel _page2 = new Panel { Dock = DockStyle.Fill, BackColor = PageBack, Visible = false };
        private readonly Panel _page3 = new Panel { Dock = DockStyle.Fill, BackColor = PageBack, Visible = false };

        private readonly FlowLayoutPanel _checks = new FlowLayoutPanel();
        private readonly Panel _moduleList = new Panel();
        private readonly FlowLayoutPanel _progressList = new FlowLayoutPanel();
        private readonly Label _summary = new Label();

        private Button _btnNext, _btnBack, _btnInstall, _btnRecheck, _btnClose, _btnSite;

        private readonly SetupCatalogSource _catalogSource;
        private readonly MirrorFallbackDownloader _downloader =
            new MirrorFallbackDownloader(new HttpFileDownloader());

        private EnvironmentReport _env = new EnvironmentReport();
        private List<ModuleEntry> _catalog = new List<ModuleEntry>();
        private List<ModuleChoice> _choices = new List<ModuleChoice>();
        private List<string> _shadows = new List<string>();
        private readonly Dictionary<string, CheckBox> _boxes = new Dictionary<string, CheckBox>();

        public SetupForm()
        {
            Text            = "Установка модулей ABR | CIVIL";
            ClientSize      = new Size(560, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = PageBack;
            Font            = new Font("Segoe UI", 9f);
            Icon            = AbrIcon.Create();

            _catalogSource = new SetupCatalogSource(
                _downloader, EmbeddedCatalogJson, Path.Combine(AbrPaths.DataRoot, "setup"));

            BuildPage1();
            Controls.Add(_page3);
            Controls.Add(_page2);
            Controls.Add(_page1);

            RunChecks();
        }

        /// <summary>Снимок каталога из ресурсов exe - последняя ступень фолбэка.</summary>
        private static string EmbeddedCatalogJson()
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream("AbrCivil.Setup.catalog.json"))
            {
                if (stream == null) return "";
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        /// <summary>Родные кнопки Windows, без owner-draw: правило линейки.</summary>
        internal static Button MakeButton(string text, bool primary)
        {
            var b = new Button { Text = text, AutoSize = false, Size = new Size(130, 30) };
            if (primary)
            {
                b.BackColor = Accent;
                b.ForeColor = Color.White;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
            }
            return b;
        }

        private void BuildPage1()
        {
            var title = new Label
            {
                Text = "ABR | CIVIL",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize = true,
                Location = new Point(24, 28)
            };
            var subtitle = new Label
            {
                Text = "Модули для Autodesk Civil 3D",
                Font = new Font("Segoe UI", 10f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(26, 62)
            };

            _checks.Location = new Point(24, 110);
            _checks.Size = new Size(510, 260);
            _checks.FlowDirection = FlowDirection.TopDown;
            _checks.WrapContents = false;
            _checks.AutoScroll = true;

            _btnRecheck = MakeButton("Проверить снова", false);
            _btnRecheck.Location = new Point(24, 400);
            _btnRecheck.Click += (s, e) => RunChecks();

            _btnNext = MakeButton("Далее >", true);
            _btnNext.Location = new Point(404, 400);
            _btnNext.Enabled = false;
            _btnNext.Click += (s, e) => ShowPage2();

            _page1.Controls.Add(title);
            _page1.Controls.Add(subtitle);
            _page1.Controls.Add(_checks);
            _page1.Controls.Add(_btnRecheck);
            _page1.Controls.Add(_btnNext);
        }

        /// <summary>Строка проверки: точка-маркер цветом состояния + текст.</summary>
        private void AddCheck(string text, Color color, Action action = null, string actionText = null)
        {
            var row = new Panel { Size = new Size(490, action == null ? 26 : 34), Margin = new Padding(0, 0, 0, 4) };

            var dot = new Label
            {
                Text = "*",
                ForeColor = color,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            var label = new Label
            {
                Text = text,
                ForeColor = color == Good ? Color.Black : color,
                AutoSize = false,
                Size = new Size(action == null ? 470 : 360, 24),
                Location = new Point(18, 3)
            };

            row.Controls.Add(dot);
            row.Controls.Add(label);

            if (action != null)
            {
                var btn = MakeButton(actionText, false);
                btn.Size = new Size(100, 26);
                btn.Location = new Point(386, 0);
                btn.Click += (s, e) => action();
                row.Controls.Add(btn);
            }

            _checks.Controls.Add(row);
        }

        private void RunChecks()
        {
            _checks.Controls.Clear();
            _btnNext.Enabled = false;
            _btnRecheck.Enabled = false;

            _env = new Civil3DDetector(new RegistryProbe(), RegistryProbe.IsAcadRunning).Detect();
            _shadows = ShadowScanner.Find(ShadowScanner.ProgramDataPluginsRoot);

            if (_env.Years.Count > 0)
                AddCheck("Найден Civil 3D " + string.Join(", ", _env.Years.ConvertAll(y => y.ToString()).ToArray()), Good);
            else
                AddCheck("Civil 3D не найден. Установка возможна, модули появятся после его установки.", Warn);

            if (_env.AcadRunning)
                AddCheck("Civil 3D запущен. Закройте его - файлы модулей заняты.", Bad);
            else
                AddCheck("Civil 3D закрыт", Good);

            if (_shadows.Count > 0)
                AddCheck("Старые копии в ProgramData перекроют новые (" + _shadows.Count + ")",
                         Warn, CleanShadows, "Убрать");

            AddCheck("Загрузка каталога...", TextMuted);
            LoadCatalogAsync();
        }

        private void LoadCatalogAsync()
        {
            var thread = new Thread(() =>
            {
                var loaded = _catalogSource.Load();
                var origin = _catalogSource.OriginText;

                BeginInvoke((MethodInvoker)(() =>
                {
                    _catalog = loaded;

                    // Последняя строка - заглушка «Загрузка каталога...», заменяем результатом.
                    _checks.Controls.RemoveAt(_checks.Controls.Count - 1);

                    if (loaded.Count == 0)
                        AddCheck("Каталог недоступен: нет связи ни с GitHub, ни с зеркалом.", Bad);
                    else
                        AddCheck(origin + ": модулей " + loaded.Count,
                                 _catalogSource.Origin == CatalogOrigin.Live ? Good : Warn);

                    _btnRecheck.Enabled = true;
                    _btnNext.Enabled = !_env.AcadRunning && loaded.Count > 0;
                }));
            });
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>Папка ProgramData админская: перезапускаем сами себя с повышением.</summary>
        private void CleanShadows()
        {
            try
            {
                var psi = new ProcessStartInfo(Application.ExecutablePath, Program.CleanArg)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                var proc = Process.Start(psi);
                if (proc != null) proc.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось удалить старые копии:\r\n" + ex.Message,
                    "Установка модулей", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            RunChecks();
        }

        // Заглушка: полная реализация появляется в задаче "Шаг 2 мастера".
        private void ShowPage2() { }
    }
}
