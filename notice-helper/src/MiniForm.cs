using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace QQNotify
{
    internal class MiniForm : Form
    {
        private const int SizePx = 104;
        private const int AvatarPx = 84;

        private readonly MainForm _main;
        private Image _avatar;
        private EngineState _state = EngineState.Idle;
        private Bitmap _surface;
        private bool _dragging;
        private bool _moved;
        private Point _dragStart;
        private Point _formStart;
        private ToolTip _tip;
        private Timer _pulseTimer;
        private float _phase;

        public MiniForm(MainForm main)
        {
            _main = main;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(SizePx, SizePx);
            BackColor = Color.Magenta;
            DoubleBuffered = true;

            BuildMenu();

            _tip = new ToolTip();
            _tip.SetToolTip(this, "单击打开主界面 · 拖动移动 · 右键菜单");

            _pulseTimer = new Timer();
            _pulseTimer.Interval = 50;
            _pulseTimer.Tick += delegate
            {
                _phase += 0.24f;
                Render();
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private void BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.BackColor = Theme.Card;
            menu.ForeColor = Theme.Ink;
            menu.ShowImageMargin = false;
            menu.Renderer = new ToolStripProfessionalRenderer(new LightColorTable());
            menu.Items.Add("打开主界面", null, delegate { _main.RestoreFromMini(); });
            menu.Items.Add("开始 / 暂停", null, delegate { _main.ToggleStartPauseFromMini(); });
            menu.Items.Add("停止发送", null, delegate { _main.StopFromMini(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出", null, delegate { _main.ExitFromMini(); });
            ContextMenuStrip = menu;
        }

        public void SetAvatar(Image avatar)
        {
            _avatar = avatar;
            Render();
        }

        public void SetState(EngineState state)
        {
            _state = state;
            if (state == EngineState.Running || state == EngineState.Countdown)
            {
                _pulseTimer.Start();
            }
            else
            {
                _pulseTimer.Stop();
                _phase = 0f;
            }
            Render();
        }

        public void ShowAt(int x, int y)
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            if (x < wa.Left || y < wa.Top || x > wa.Right - SizePx || y > wa.Bottom - SizePx)
            {
                x = wa.Right - SizePx - 24;
                y = wa.Bottom - SizePx - 24;
            }
            Location = new Point(x, y);
            Render();
            Show();
            TopMost = true;
        }

        private Color StateColor()
        {
            switch (_state)
            {
                case EngineState.Running: return Theme.Success;
                case EngineState.Countdown: return Theme.Accent;
                case EngineState.Paused: return Theme.Warn;
                default: return Theme.Muted;
            }
        }

        private void Render()
        {
            if (!IsHandleCreated) return;
            if (_surface == null)
            {
                _surface = new Bitmap(SizePx, SizePx, PixelFormat.Format32bppArgb);
            }
            using (Graphics g = Graphics.FromImage(_surface))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                for (int i = 8; i >= 1; i--)
                {
                    using (SolidBrush shadow = new SolidBrush(Color.FromArgb(4, 60, 48, 30)))
                    {
                        g.FillEllipse(shadow, 8 - i / 2, 11 - i / 2, 88 + i, 88 + i);
                    }
                }

                Color sc = StateColor();
                if (_state == EngineState.Running || _state == EngineState.Countdown)
                {
                    float a = 0.5f + 0.5f * (float)Math.Sin(_phase);
                    using (Pen ring = new Pen(Color.FromArgb((int)(90 * a), sc), 2f))
                    {
                        g.DrawEllipse(ring, 3, 3, SizePx - 7, SizePx - 7);
                    }
                    using (Pen ring = new Pen(Color.FromArgb((int)(40 * a), sc), 5f))
                    {
                        g.DrawEllipse(ring, 6, 6, SizePx - 13, SizePx - 13);
                    }
                }

                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    g.FillEllipse(white, 6, 6, 92, 92);
                }
                Theme.DrawAvatar(g, new Rectangle(10, 10, AvatarPx, AvatarPx), _avatar, Color.White, 2);

                using (SolidBrush border = new SolidBrush(Color.White))
                {
                    g.FillEllipse(border, 68, 68, 26, 26);
                }
                Theme.DrawDot(g, 73, 73, 16, sc);
            }
            ApplyLayered();
        }

        private void ApplyLayered()
        {
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            IntPtr memDc = Native.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            try
            {
                hBitmap = _surface.GetHbitmap(Color.FromArgb(0));
                oldBitmap = Native.SelectObject(memDc, hBitmap);
                Native.SIZE size = new Native.SIZE(SizePx, SizePx);
                Native.POINT ptSrc = new Native.POINT(0, 0);
                Native.POINT ptDst = new Native.POINT(Left, Top);
                Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
                blend.BlendOp = Native.AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = Native.AC_SRC_ALPHA;
                Native.UpdateLayeredWindow(Handle, screenDc, ref ptDst, ref size, memDc, ref ptSrc, 0,
                    ref blend, Native.ULW_ALPHA);
            }
            finally
            {
                Native.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    Native.SelectObject(memDc, oldBitmap);
                    Native.DeleteObject(hBitmap);
                }
                Native.DeleteDC(memDc);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Render();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _moved = false;
                _dragStart = Cursor.Position;
                _formStart = Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            Point now = Cursor.Position;
            int dx = now.X - _dragStart.X;
            int dy = now.Y - _dragStart.Y;
            if (!_moved && (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)) _moved = true;
            if (_moved)
            {
                Location = new Point(_formStart.X + dx, _formStart.Y + dy);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            _dragging = false;
            if (_moved)
            {
                _main.SaveMiniPosition(Left, Top);
            }
            else
            {
                _main.RestoreFromMini();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pulseTimer != null)
                {
                    _pulseTimer.Stop();
                    _pulseTimer.Dispose();
                    _pulseTimer = null;
                }
                if (_surface != null)
                {
                    _surface.Dispose();
                    _surface = null;
                }
                if (_tip != null)
                {
                    _tip.Dispose();
                    _tip = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
