using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace QQNotify
{
    internal static class Theme
    {
        public static readonly Color Paper = Color.FromArgb(245, 241, 233);
        public static readonly Color Card = Color.FromArgb(255, 253, 249);
        public static readonly Color CardSoft = Color.FromArgb(250, 246, 239);
        public static readonly Color Ink = Color.FromArgb(43, 39, 33);
        public static readonly Color InkSoft = Color.FromArgb(88, 81, 71);
        public static readonly Color Muted = Color.FromArgb(142, 133, 119);
        public static readonly Color Line = Color.FromArgb(228, 220, 206);
        public static readonly Color LineSoft = Color.FromArgb(238, 232, 222);
        public static readonly Color Accent = Color.FromArgb(193, 95, 62);
        public static readonly Color AccentDark = Color.FromArgb(163, 76, 48);
        public static readonly Color AccentSoft = Color.FromArgb(246, 230, 219);
        public static readonly Color Success = Color.FromArgb(76, 122, 93);
        public static readonly Color Warn = Color.FromArgb(176, 122, 42);
        public static readonly Color Danger = Color.FromArgb(162, 74, 62);

        public static readonly Font Title = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold);
        public static readonly Font Subtitle = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular);
        public static readonly Font Section = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold);
        public static readonly Font Body = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular);
        public static readonly Font BodyBold = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
        public static readonly Font Small = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular);
        public static readonly Font Tiny = new Font("Microsoft YaHei UI", 8f, FontStyle.Regular);
        public static readonly Font Button = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
        public static readonly Font State = new Font("Microsoft YaHei UI", 20f, FontStyle.Bold);
        public static readonly Font Number = new Font("Segoe UI Light", 21f, FontStyle.Regular);
        public static readonly Font KeyCap = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);

        public static Color Blend(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius < 1) radius = 1;
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, Rectangle r, int radius, Color color)
        {
            using (GraphicsPath path = RoundedRect(r, radius))
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRounded(Graphics g, Rectangle r, int radius, Color color, float width)
        {
            using (GraphicsPath path = RoundedRect(r, radius))
            using (Pen pen = new Pen(color, width))
            {
                g.DrawPath(pen, path);
            }
        }

        public static void DrawCard(Graphics g, Rectangle r, int radius)
        {
            for (int i = 4; i >= 1; i--)
            {
                Rectangle sr = new Rectangle(r.X - i / 2, r.Y + i, r.Width + i, r.Height + i / 2);
                using (GraphicsPath sp = RoundedRect(sr, radius))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(5, 70, 55, 35)))
                {
                    g.FillPath(sb, sp);
                }
            }
            using (GraphicsPath path = RoundedRect(r, radius))
            {
                using (SolidBrush brush = new SolidBrush(Card))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(Line, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        public static void DrawDot(Graphics g, int x, int y, int size, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, x, y, size, size);
            }
        }

        public static void DrawAvatar(Graphics g, Rectangle bounds, Image image, Color ringColor, int ringWidth)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                GraphicsState state = g.Save();
                g.SetClip(path);
                if (image != null)
                {
                    float scale = Math.Max((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
                    int w = (int)Math.Ceiling(image.Width * scale);
                    int h = (int)Math.Ceiling(image.Height * scale);
                    Rectangle dest = new Rectangle(
                        bounds.X + (bounds.Width - w) / 2,
                        bounds.Y + (bounds.Height - h) / 2, w, h);
                    g.DrawImage(image, dest);
                }
                else
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(bounds, Color.FromArgb(247, 230, 218), Color.FromArgb(238, 210, 191), 90f))
                    {
                        g.FillEllipse(brush, bounds);
                    }
                    DrawUserGlyph(g, bounds);
                }
                g.Restore(state);
                using (Pen pen = new Pen(ringColor, ringWidth))
                {
                    g.DrawEllipse(pen, new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1));
                }
            }
        }

        private static void DrawUserGlyph(Graphics g, Rectangle bounds)
        {
            int cx = bounds.X + bounds.Width / 2;
            int headR = Math.Max(5, (int)(bounds.Width * 0.135f));
            int headCy = bounds.Y + (int)(bounds.Height * 0.37f);
            int shoulderW = (int)(bounds.Width * 0.46f);
            int shoulderH = (int)(bounds.Height * 0.30f);
            int shoulderTop = bounds.Y + (int)(bounds.Height * 0.60f);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(185, AccentDark)))
            {
                g.FillEllipse(brush, cx - headR, headCy - headR, headR * 2, headR * 2);
                Rectangle shoulders = new Rectangle(cx - shoulderW / 2, shoulderTop, shoulderW, shoulderH * 2);
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(shoulders, 180f, 180f);
                    path.CloseFigure();
                    g.FillPath(brush, path);
                }
            }
        }

        public static void DrawBell(Graphics g, Rectangle bounds, Color color)
        {
            float s = bounds.Width / 24f;
            GraphicsState state = g.Save();
            g.TranslateTransform(bounds.X, bounds.Y);
            g.ScaleTransform(s, s);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, 10f, 0.5f, 4f, 4f);
                g.FillEllipse(brush, 4.5f, 4f, 15f, 14f);
                g.FillRectangle(brush, 4.5f, 10.5f, 15f, 7f);
                g.FillRectangle(brush, 2.5f, 16.8f, 19f, 2.6f);
                g.FillEllipse(brush, 10f, 20.2f, 4f, 4f);
            }
            g.Restore(state);
        }

        public static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length <= max) return text;
            return text.Substring(0, Math.Max(1, max - 1)) + "…";
        }
    }
}
