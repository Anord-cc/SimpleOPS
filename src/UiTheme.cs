using System.Drawing;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal static class UiTheme
    {
        public static readonly Color WindowBackground = Color.FromArgb(8, 8, 8);
        public static readonly Color RailBackground = Color.FromArgb(14, 14, 14);
        public static readonly Color SurfaceBackground = Color.FromArgb(18, 18, 18);
        public static readonly Color CardBackground = Color.FromArgb(24, 24, 24);
        public static readonly Color CardBorder = Color.FromArgb(62, 62, 62);
        public static readonly Color InputBackground = Color.FromArgb(12, 12, 12);
        public static readonly Color InputBorder = Color.FromArgb(72, 72, 72);
        public static readonly Color TextPrimary = Color.White;
        public static readonly Color TextMuted = Color.FromArgb(189, 189, 189);
        public static readonly Color TextSoft = Color.FromArgb(136, 136, 136);
        public static readonly Color InverseText = Color.Black;

        public static Font TitleFont(float size)
        {
            return new Font("Bahnschrift SemiBold", size, FontStyle.Regular, GraphicsUnit.Point);
        }

        public static Font BodyFont(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }

        public static void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.White;
            button.BackColor = Color.White;
            button.ForeColor = InverseText;
            button.Font = BodyFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = CardBorder;
            button.BackColor = CardBackground;
            button.ForeColor = TextPrimary;
            button.Font = BodyFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        public static void StyleNavButton(Button button, bool active)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Font = BodyFont(10f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Padding = new Padding(16, 0, 0, 0);

            if (active)
            {
                button.BackColor = Color.White;
                button.ForeColor = InverseText;
            }
            else
            {
                button.BackColor = RailBackground;
                button.ForeColor = TextMuted;
            }
        }

        public static void StyleInput(Control control)
        {
            control.Font = BodyFont(9.75f);
            control.BackColor = InputBackground;
            control.ForeColor = TextPrimary;
        }
    }

    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            DoubleBuffered = true;
            BackColor = UiTheme.CardBackground;
            Padding = new Padding(18);
            Margin = new Padding(0, 0, 0, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(UiTheme.CardBorder))
            {
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
    }
}
