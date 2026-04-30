using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal static class UiTheme
    {
        public static readonly Color WindowBackground = Color.FromArgb(246, 243, 236);
        public static readonly Color CardBackground = Color.FromArgb(255, 252, 246);
        public static readonly Color CardBorder = Color.FromArgb(223, 216, 204);
        public static readonly Color Accent = Color.FromArgb(13, 80, 97);
        public static readonly Color AccentSoft = Color.FromArgb(219, 236, 239);
        public static readonly Color Action = Color.FromArgb(198, 92, 44);
        public static readonly Color ActionSoft = Color.FromArgb(250, 232, 222);
        public static readonly Color TextPrimary = Color.FromArgb(33, 36, 41);
        public static readonly Color TextMuted = Color.FromArgb(98, 103, 110);
        public static readonly Color Success = Color.FromArgb(48, 122, 73);
        public static readonly Color Warning = Color.FromArgb(163, 104, 18);
        public static readonly Color Danger = Color.FromArgb(163, 49, 49);

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
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Action;
            button.ForeColor = Color.White;
            button.Font = BodyFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = CardBorder;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = CardBackground;
            button.ForeColor = TextPrimary;
            button.Font = BodyFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }

        public static void StyleInput(Control control)
        {
            control.Font = BodyFont(9.75f);
            control.BackColor = Color.White;
            control.ForeColor = TextPrimary;
        }
    }

    internal sealed class GradientPanel : Panel
    {
        public Color StartColor = Color.FromArgb(20, 72, 87);
        public Color EndColor = Color.FromArgb(31, 114, 118);

        public GradientPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(ClientRectangle, StartColor, EndColor, LinearGradientMode.ForwardDiagonal))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }
    }

    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            DoubleBuffered = true;
            BackColor = UiTheme.CardBackground;
            Padding = new Padding(20);
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
