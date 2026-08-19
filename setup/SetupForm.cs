using System.Drawing;
using System.Windows.Forms;
using Abr.Civil.Sdk;

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
    }
}
