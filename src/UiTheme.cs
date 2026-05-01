// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System.Drawing;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal static class UiTheme
    {
        public static readonly Color WindowBackground = Color.FromArgb(6, 6, 6);
        public static readonly Color RailBackground = Color.FromArgb(10, 10, 10);
        public static readonly Color RailBorder = Color.FromArgb(34, 34, 34);
        public static readonly Color SurfaceBackground = Color.FromArgb(14, 14, 14);
        public static readonly Color CardBackground = Color.FromArgb(19, 19, 19);
        public static readonly Color RaisedCardBackground = Color.FromArgb(23, 23, 23);
        public static readonly Color CardBorder = Color.FromArgb(52, 52, 52);
        public static readonly Color InputBackground = Color.FromArgb(11, 11, 11);
        public static readonly Color InputBorder = Color.FromArgb(74, 74, 74);
        public static readonly Color ButtonHover = Color.FromArgb(28, 28, 28);
        public static readonly Color ButtonDown = Color.FromArgb(34, 34, 34);
        public static readonly Color TextPrimary = Color.White;
        public static readonly Color TextMuted = Color.FromArgb(196, 196, 196);
        public static readonly Color TextSoft = Color.FromArgb(142, 142, 142);
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
            button.FlatAppearance.MouseOverBackColor = Color.White;
            button.FlatAppearance.MouseDownBackColor = Color.White;
            button.BackColor = Color.White;
            button.ForeColor = InverseText;
            button.Font = BodyFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.TabStop = false;
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = CardBorder;
            button.FlatAppearance.MouseOverBackColor = ButtonHover;
            button.FlatAppearance.MouseDownBackColor = ButtonDown;
            button.BackColor = RaisedCardBackground;
            button.ForeColor = TextPrimary;
            button.Font = BodyFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.TabStop = false;
        }

        public static void StyleNavButton(Button button, bool active)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = active ? Color.White : ButtonHover;
            button.FlatAppearance.MouseDownBackColor = active ? Color.White : ButtonDown;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Font = BodyFont(10.25f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Padding = new Padding(18, 0, 0, 0);
            button.Margin = new Padding(0, 0, 0, 10);
            button.Height = 46;
            button.TabStop = false;

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

        public static Panel CreateRailDivider()
        {
            return new Panel
            {
                Dock = DockStyle.Right,
                Width = 1,
                BackColor = RailBorder
            };
        }
    }

    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            DoubleBuffered = true;
            BackColor = UiTheme.CardBackground;
            Padding = new Padding(22, 20, 22, 20);
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
