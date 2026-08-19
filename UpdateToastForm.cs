using System;
using System.Drawing;
using System.Windows.Forms;

namespace AbrCivil.Modules
{
    /// <summary>
    /// Лёгкий тост снизу-справа - обновления модулей ABR | CIVIL. Живёт на собственном
    /// STA-потоке (Application.Run в UpdateNotifier.ShowToast). Тот же светлый стиль
    /// и акцент #0891B2, что у StoreForm.
    /// </summary>
    internal sealed class UpdateToastForm : Form
    {
        private const int LifetimeMs = 8000;

        private static readonly Color Accent    = ColorTranslator.FromHtml("#0891B2");
        private static readonly Color TextMain  = ColorTranslator.FromHtml("#1D2029");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#5D6373");
        private static readonly Color BorderCol = ColorTranslator.FromHtml("#DFE1E8");
        private static readonly Color LinkGray  = ColorTranslator.FromHtml("#8A8F9E");

        private readonly Action _onOpen;
        private readonly Action _onMute;
        private readonly Timer  _lifeTimer;

        public UpdateToastForm(string message, Action onOpen, Action onMute)
        {
            _onOpen = onOpen;
            _onMute = onMute;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            TopMost         = true;
            BackColor       = Color.White;

            var head = new Label
            {
                Text      = "Библиотека модулей",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Accent,
                Dock      = DockStyle.Top,
                Height    = 26,
                Padding   = new Padding(10, 6, 0, 0)
            };

            var body = new Label
            {
                Text      = message,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                Dock      = DockStyle.Fill,
                Padding   = new Padding(10, 0, 10, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 24 };
            var open = new LinkLabel
            {
                Text         = "Открыть",
                Font         = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                LinkColor    = Accent,
                AutoSize     = true,
                Location     = new Point(10, 2),
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            var mute = new LinkLabel
            {
                Text         = "Не напоминать",
                Font         = new Font("Segoe UI", 8f),
                LinkColor    = LinkGray,
                AutoSize     = true,
                Anchor       = AnchorStyles.Top | AnchorStyles.Right,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            bottom.Controls.Add(open);
            bottom.Controls.Add(mute);
            bottom.Resize += (s, e) => mute.Left = bottom.Width - mute.Width - 10;

            Controls.Add(body);
            Controls.Add(head);
            Controls.Add(bottom);

            Cursor      = Cursors.Hand;
            body.Cursor = Cursors.Hand;
            head.Cursor = Cursors.Hand;

            Click            += OnBodyClick;
            head.Click       += OnBodyClick;
            body.Click       += OnBodyClick;
            open.LinkClicked += (s, e) => OnBodyClick(s, EventArgs.Empty);
            mute.LinkClicked += OnMuteClick;

            _lifeTimer = new Timer { Interval = LifetimeMs };
            _lifeTimer.Tick += (s, e) => Close();
            Shown += (s, e) => _lifeTimer.Start();
        }

        /// <summary>Не красть фокус у Civil 3D при показе.</summary>
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                float scale;
                using (var g = CreateGraphics())
                    scale = g.DpiX / 96f;
                Size = new Size((int)(340 * scale), (int)(94 * scale));
                var wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Right - Width - 12, wa.Bottom - Height - 12);
            }
            catch (Exception)
            {
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            try
            {
                ControlPaint.DrawBorder(e.Graphics, ClientRectangle, BorderCol, ButtonBorderStyle.Solid);
            }
            catch (Exception)
            {
            }
        }

        private void OnBodyClick(object sender, EventArgs e)
        {
            _lifeTimer.Stop();
            Hide();
            try { _onOpen(); } catch (Exception) { }
            Close();
        }

        private void OnMuteClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _lifeTimer.Stop();
            try { _onMute(); } catch (Exception) { }
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _lifeTimer != null) _lifeTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
