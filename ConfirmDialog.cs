using System.Drawing;
using System.Windows.Forms;
using Abr.Civil.Sdk;

namespace AbrCivil.Modules
{
    /// <summary>Диалог подтверждения перед установкой/обновлением/удалением/деактивацией -
    /// клик по кнопке карточки не должен выполняться вслепую.</summary>
    internal sealed class ConfirmDialog : Form
    {
        private static readonly Color Accent   = ColorTranslator.FromHtml("#0891B2");
        private static readonly Color TextMuted = ColorTranslator.FromHtml("#5B6770");

        public ConfirmDialog(string title, string message, string okText)
        {
            Text            = title;
            ClientSize      = new Size(420, 160);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            Font            = new Font("Segoe UI", 9f);
            Icon            = AbrIcon.Create();

            var body = new Label
            {
                Text      = message,
                ForeColor = TextMuted,
                Location  = new Point(16, 16),
                Size      = new Size(388, 96),
                TextAlign = ContentAlignment.TopLeft
            };
            Controls.Add(body);

            var ok = MakeButton(okText, true);
            ok.Location = new Point(212, 118);
            ok.DialogResult = DialogResult.OK;

            var cancel = MakeButton("Отмена", false);
            cancel.Location = new Point(322, 118);
            cancel.DialogResult = DialogResult.Cancel;

            Controls.Add(ok);
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private static Button MakeButton(string text, bool primary)
        {
            var b = new Button { Text = text, AutoSize = false, Size = new Size(90, 30) };
            if (primary)
            {
                b.BackColor = Accent;
                b.ForeColor = Color.White;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
            }
            return b;
        }

        /// <summary>true - юзер подтвердил.</summary>
        public static bool Ask(IWin32Window owner, string title, string message, string okText)
        {
            using (var dlg = new ConfirmDialog(title, message, okText))
                return dlg.ShowDialog(owner) == DialogResult.OK;
        }
    }
}
