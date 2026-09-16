using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Abr.Civil.Sdk;

namespace AbrCivil.Setup
{
    /// <summary>Технические требования - текст из ресурса requirements.txt, правится без кода.</summary>
    internal class RequirementsForm : Form
    {
        private const string ResourceName = "AbrCivil.Setup.requirements.txt";

        public RequirementsForm()
        {
            Text            = "Требования";
            ClientSize      = new Size(480, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = SetupForm.PageBack;
            Font            = new Font("Segoe UI", 9f);
            Icon            = AbrIcon.Create();

            var card = new Panel
            {
                Location  = new Point(16, 16),
                Size      = new Size(448, 340),
                BackColor = SetupForm.CardBack,
                Padding   = new Padding(10)
            };

            var text = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                BackColor   = SetupForm.CardBack,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                DetectUrls  = false,
                Text        = LoadText()
            };
            card.Controls.Add(text);

            var close = SetupForm.MakeButton("Закрыть", true);
            close.Location = new Point(334, 372);
            close.DialogResult = DialogResult.OK;

            Controls.Add(card);
            Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;
        }

        /// <summary>Нет ресурса - говорим об этом в окне, а не показываем пустое поле.</summary>
        private static string LoadText()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
            {
                if (stream == null) return "Текст требований не найден в сборке.";
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}
