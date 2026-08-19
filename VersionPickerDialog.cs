using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Abr.Civil.Sdk;
using AbrCivil.Modules.Core;

namespace AbrCivil.Modules
{
    /// <summary>Список релизов модуля с GitHub - выбор версии для отката/переустановки.</summary>
    internal sealed class VersionPickerDialog : Form
    {
        private static readonly Color Accent = ColorTranslator.FromHtml("#0891B2");

        private readonly ListBox _list = new ListBox();
        private readonly List<ReleaseInfo> _releases;

        public ReleaseInfo Selected { get; private set; }

        public VersionPickerDialog(string title, List<ReleaseInfo> releases)
        {
            _releases = releases;

            Text            = "Другие версии - " + title;
            ClientSize      = new Size(360, 320);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            Font            = new Font("Segoe UI", 9f);
            Icon            = AbrIcon.Create();

            _list.SetBounds(12, 12, 336, 250);
            _list.IntegralHeight = false;
            foreach (var r in releases)
                _list.Items.Add(r.TagName + "   (" + r.Downloads + " скачиваний)");
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;
            Controls.Add(_list);

            var ok = new Button { Text = "Установить", AutoSize = false, Size = new Size(110, 30) };
            ok.BackColor = Accent;
            ok.ForeColor = Color.White;
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderSize = 0;
            ok.Location = new Point(126, 274);
            ok.Click += (s, e) =>
            {
                if (_list.SelectedIndex < 0) return;
                Selected = _releases[_list.SelectedIndex];
                DialogResult = DialogResult.OK;
                Close();
            };

            var cancel = new Button { Text = "Отмена", AutoSize = false, Size = new Size(110, 30) };
            cancel.Location = new Point(238, 274);
            cancel.DialogResult = DialogResult.Cancel;

            Controls.Add(ok);
            Controls.Add(cancel);
            CancelButton = cancel;
        }
    }
}
