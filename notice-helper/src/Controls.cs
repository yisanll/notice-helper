using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace QQNotify
{
    internal class PillButton : Control
    {
        private bool _hover;
        private bool _down;
        private float _hoverT;
        private Timer _anim;
        private Color _fill = Theme.Accent;
        private Color _hoverFill = Theme.AccentDark;
        private Color _textColor = Color.White;
        private Color _borderColor = Color.Empty;
        private Color _surface = Theme.Paper;
        private int _radius = 12;

        public bool PlayIcon;

        public PillButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Theme.Paper;
            Cursor = Cursors.Hand;
            Font = Theme.Button;
            _anim = new Timer();
            _anim.Interval = 15;
            _anim.Tick += delegate { AnimateHover(); };
        }

        public Color Fill
        {
            get { return _fill; }
            set { _fill = value; Invalidate(); }
        }

        public Color HoverFill
        {
            get { return _hoverFill; }
            set { _hoverFill = value; Invalidate(); }
        }

        public Color TextColor
        {
            get { return _textColor; }
            set { _textColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public Color SurfaceColor
        {
            get { return _surface; }
            set { _surface = value; BackColor = value; Invalidate(); }
        }

        public int Radius
        {
            get { return _radius; }
            set { _radius = value; Invalidate(); }
        }

        private void AnimateHover()
        {
            float target = _hover && Enabled ? 1f : 0f;
            float step = 0.16f;
            if (Math.Abs(_hoverT - target) <= step)
            {
                _hoverT = target;
                _anim.Stop();
            }
            else
            {
                _hoverT += target > _hoverT ? step : -step;
            }
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            _anim.Start();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _down = false;
            _anim.Start();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _down = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _down = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush surface = new SolidBrush(_surface))
            {
                g.FillRectangle(surface, ClientRectangle);
            }

            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            Color fill;
            Color textColor;
            if (!Enabled)
            {
                fill = Theme.Line;
                textColor = Theme.Muted;
            }
            else
            {
                fill = Theme.Blend(_fill, _hoverFill, _hoverT);
                if (_down) fill = Theme.Blend(fill, Color.Black, 0.07f);
                textColor = _textColor;
            }

            if (fill.A > 0)
            {
                using (GraphicsPath path = Theme.RoundedRect(r, _radius))
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
            }
            if (_borderColor != Color.Empty)
            {
                Theme.DrawRounded(g, r, _radius, _borderColor, 1f);
            }

            int iconW = PlayIcon ? 11 : 0;
            int gap = PlayIcon ? 9 : 0;
            Size textSize = TextRenderer.MeasureText(Text, Font);
            int total = iconW + gap + textSize.Width;
            int x = (Width - total) / 2;
            if (PlayIcon)
            {
                Point[] tri = new Point[]
                {
                    new Point(x, Height / 2 - 5),
                    new Point(x, Height / 2 + 5),
                    new Point(x + 9, Height / 2)
                };
                using (SolidBrush brush = new SolidBrush(textColor))
                {
                    g.FillPolygon(brush, tri);
                }
                x += iconW + gap;
            }
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(x, 0, Width - x, Height), textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _anim != null)
            {
                _anim.Dispose();
                _anim = null;
            }
            base.Dispose(disposing);
        }
    }

    internal class LinkButton : Control
    {
        private bool _hover;
        private Color _surface = Theme.Paper;
        private Color _color = Theme.Muted;

        public LinkButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Theme.Paper;
            Cursor = Cursors.Hand;
            Font = Theme.Small;
        }

        public Color SurfaceColor
        {
            get { return _surface; }
            set { _surface = value; BackColor = value; Invalidate(); }
        }

        public Color LinkColor
        {
            get { return _color; }
            set { _color = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush surface = new SolidBrush(_surface))
            {
                g.FillRectangle(surface, ClientRectangle);
            }
            Color c = _hover ? Theme.Accent : _color;
            Size sz = TextRenderer.MeasureText(Text, Font);
            int x = (Width - sz.Width) / 2;
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), c,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            if (_hover)
            {
                int y = Height / 2 + sz.Height / 2 - 2;
                using (Pen pen = new Pen(c, 1f))
                {
                    g.DrawLine(pen, x, y, x + sz.Width, y);
                }
            }
        }
    }

    internal class ToggleSwitch : Control
    {
        private bool _checked;
        private float _pos;
        private Timer _anim;
        private Color _surface = Theme.Card;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(44, 24);
            Cursor = Cursors.Hand;
            BackColor = Theme.Card;
            _anim = new Timer();
            _anim.Interval = 15;
            _anim.Tick += delegate { AnimateKnob(); };
        }

        public event EventHandler CheckedChanged;

        public Color SurfaceColor
        {
            get { return _surface; }
            set { _surface = value; BackColor = value; Invalidate(); }
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                _anim.Start();
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        private void AnimateKnob()
        {
            float target = _checked ? 1f : 0f;
            float step = 0.13f;
            if (Math.Abs(_pos - target) <= step)
            {
                _pos = target;
                _anim.Stop();
            }
            else
            {
                _pos += target > _pos ? step : -step;
            }
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush surface = new SolidBrush(_surface))
            {
                g.FillRectangle(surface, ClientRectangle);
            }
            Rectangle pill = new Rectangle(1, 1, Width - 3, Height - 3);
            Color track = Theme.Blend(Theme.Line, Theme.Accent, _pos);
            using (GraphicsPath path = Theme.RoundedRect(pill, pill.Height / 2))
            using (SolidBrush brush = new SolidBrush(track))
            {
                g.FillPath(brush, path);
            }
            int knob = Height - 10;
            int travel = Width - knob - 10;
            int x = 5 + (int)Math.Round(travel * _pos);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(28, 60, 48, 30)))
            {
                g.FillEllipse(shadow, x + 1, 6, knob, knob);
            }
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, x, 5, knob, knob);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _anim != null)
            {
                _anim.Dispose();
                _anim = null;
            }
            base.Dispose(disposing);
        }
    }

    internal class SegmentedControl : Control
    {
        private string[] _items = new string[0];
        private int _selected;
        private float _pos;
        private Timer _anim;
        private Color _surface = Theme.Card;

        public SegmentedControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Height = 36;
            Cursor = Cursors.Hand;
            BackColor = Theme.Card;
            Font = Theme.Body;
            _anim = new Timer();
            _anim.Interval = 15;
            _anim.Tick += delegate { AnimateIndicator(); };
        }

        public event EventHandler SelectedIndexChanged;

        public Color SurfaceColor
        {
            get { return _surface; }
            set { _surface = value; BackColor = value; Invalidate(); }
        }

        public string[] Items
        {
            get { return _items; }
            set { _items = value ?? new string[0]; Invalidate(); }
        }

        public int SelectedIndex
        {
            get { return _selected; }
            set
            {
                if (_items.Length == 0) return;
                int v = Math.Max(0, Math.Min(_items.Length - 1, value));
                if (_selected == v) return;
                _selected = v;
                _anim.Start();
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        private void AnimateIndicator()
        {
            float target = _selected;
            float step = 0.2f;
            if (Math.Abs(_pos - target) <= step)
            {
                _pos = target;
                _anim.Stop();
            }
            else
            {
                _pos += target > _pos ? step : -step;
            }
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            if (_items.Length > 0)
            {
                int segW = (Width - 8) / _items.Length;
                int idx = (int)((PointToClient(Cursor.Position).X - 4) / segW);
                SelectedIndex = Math.Max(0, Math.Min(_items.Length - 1, idx));
            }
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush surface = new SolidBrush(_surface))
            {
                g.FillRectangle(surface, ClientRectangle);
            }
            Rectangle track = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRounded(g, track, 10, Theme.CardSoft);
            Theme.DrawRounded(g, track, 10, Theme.Line, 1f);
            if (_items.Length == 0) return;

            int segW = (Width - 8) / _items.Length;
            int x = 4 + (int)Math.Round(segW * _pos);
            Rectangle ind = new Rectangle(x, 4, segW, Height - 8);
            Theme.FillRounded(g, ind, 8, Theme.Card);
            Theme.DrawRounded(g, ind, 8, Theme.Line, 1f);
            for (int i = 0; i < _items.Length; i++)
            {
                Rectangle itemRect = new Rectangle(4 + segW * i, 0, segW, Height);
                Color color = i == _selected ? Theme.Ink : Theme.Muted;
                TextRenderer.DrawText(g, _items[i], Font, itemRect, color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _anim != null)
            {
                _anim.Dispose();
                _anim = null;
            }
            base.Dispose(disposing);
        }
    }

    internal class Stepper : Control
    {
        private double _value = 10;
        private double _minimum = 0.01;
        private double _maximum = 3600;
        private double _step = 0.01;
        private string _suffix = "秒";
        private int _hoverZone = -1;
        private float _flash;
        private Timer _anim;
        private Color _surface = Theme.Card;
        private bool _editing;
        private string _editText = "";
        private bool _replaceOnType;

        public Stepper()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(214, 46);
            BackColor = Theme.Card;
            Font = Theme.Body;
            TabStop = true;
            _anim = new Timer();
            _anim.Interval = 15;
            _anim.Tick += delegate { AnimateFlash(); };
        }

        public event EventHandler ValueChanged;

        public Color SurfaceColor
        {
            get { return _surface; }
            set { _surface = value; BackColor = value; Invalidate(); }
        }

        public double Minimum
        {
            get { return _minimum; }
            set { _minimum = value; }
        }

        public double Maximum
        {
            get { return _maximum; }
            set { _maximum = value; }
        }

        public double Step
        {
            get { return _step; }
            set { _step = Math.Max(0.01, value); }
        }

        public string Suffix
        {
            get { return _suffix; }
            set { _suffix = value; Invalidate(); }
        }

        public double Value
        {
            get { return _value; }
            set
            {
                double v = Math.Round(value, 2);
                if (v < _minimum) v = _minimum;
                if (v > _maximum) v = _maximum;
                if (Math.Abs(v - _value) < 0.0001) return;
                _value = v;
                _flash = 1f;
                _anim.Start();
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private void AnimateFlash()
        {
            _flash -= 0.1f;
            if (_flash <= 0f)
            {
                _flash = 0f;
                _anim.Stop();
            }
            Invalidate();
        }

        private string FormatValue()
        {
            double v = Math.Round(_value, 2);
            if (Math.Abs(v - Math.Round(v)) < 0.0001) return ((int)Math.Round(v)).ToString();
            return v.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void BeginEdit()
        {
            _editing = true;
            _editText = FormatValue();
            _replaceOnType = true;
            Invalidate();
        }

        private void CommitEdit()
        {
            if (!_editing) return;
            _editing = false;
            double v;
            string text = _editText.Trim();
            if (text.Length > 0 && text != "." &&
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            {
                Value = v;
            }
            _editText = "";
            Invalidate();
        }

        private void CancelEdit()
        {
            _editing = false;
            _editText = "";
            Invalidate();
        }

        private bool AcceptEditChar(char c)
        {
            string candidate = _editText + c;
            if (candidate.Length > 8) return false;
            if (candidate.IndexOf('.') >= 0)
            {
                int dot = candidate.IndexOf('.');
                if (dot != candidate.LastIndexOf('.')) return false;
                if (candidate.Length - dot - 1 > 2) return false;
            }
            double v;
            return double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!_editing)
            {
                base.OnKeyPress(e);
                return;
            }
            char c = e.KeyChar;
            if (c == '.') c = '.';
            if (c == ',') c = '.';
            if (c == '\b')
            {
                _replaceOnType = false;
                if (_editText.Length > 0) _editText = _editText.Substring(0, _editText.Length - 1);
                Invalidate();
                e.Handled = true;
                return;
            }
            if (char.IsDigit(c) || c == '.')
            {
                if (_replaceOnType)
                {
                    _editText = c.ToString();
                    _replaceOnType = false;
                    Invalidate();
                    e.Handled = true;
                    return;
                }
                if (AcceptEditChar(c))
                {
                    _editText += c;
                    Invalidate();
                }
                e.Handled = true;
                return;
            }
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_editing)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CommitEdit();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.Escape)
                {
                    CancelEdit();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            CommitEdit();
            base.OnLostFocus(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int zone = -1;
            if (e.X <= 52) zone = 0;
            else if (e.X >= Width - 52) zone = 1;
            if (zone != _hoverZone)
            {
                _hoverZone = zone;
                Cursor = zone >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverZone = -1;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left) return;
            if (_editing) CommitEdit();
            if (e.X <= 52) Value = _value - _step;
            else if (e.X >= Width - 52) Value = _value + _step;
            else BeginEdit();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_editing) CommitEdit();
            Value = _value + (e.Delta > 0 ? _step : -_step);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush surface = new SolidBrush(_surface))
            {
                g.FillRectangle(surface, ClientRectangle);
            }
            Rectangle bg = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.FillRounded(g, bg, 12, Theme.CardSoft);
            Theme.DrawRounded(g, bg, 12, _flash > 0 ? Theme.Blend(Theme.Line, Theme.Accent, _flash * 0.7f) : Theme.Line, 1f);

            DrawRoundButton(g, 26, 0);
            DrawRoundButton(g, Width - 26, 1);

            string val = _editing && _editText.Length > 0 ? _editText : FormatValue();
            Size vs = TextRenderer.MeasureText(val, Theme.Number);
            Size ss = TextRenderer.MeasureText(_suffix, Theme.Small);
            int total = vs.Width + 5 + ss.Width;
            int x = (Width - total) / 2;
            TextRenderer.DrawText(g, val, Theme.Number,
                new Rectangle(x, 0, vs.Width + 2, Height), Theme.Ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, _suffix, Theme.Small,
                new Rectangle(x + vs.Width + 5, 0, ss.Width + 2, Height), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            if (_editing)
            {
                using (Pen pen = new Pen(Theme.Accent, 1.5f))
                {
                    g.DrawLine(pen, x, Height / 2 + 14, x + vs.Width, Height / 2 + 14);
                }
            }
        }

        private void DrawRoundButton(Graphics g, int cx, int zone)
        {
            bool hot = _hoverZone == zone;
            Rectangle circle = new Rectangle(cx - 13, Height / 2 - 13, 26, 26);
            if (hot)
            {
                using (SolidBrush brush = new SolidBrush(Theme.AccentSoft))
                {
                    g.FillEllipse(brush, circle);
                }
            }
            using (Pen pen = new Pen(hot ? Theme.Accent : Theme.InkSoft, 1.6f))
            {
                int cy = Height / 2;
                g.DrawLine(pen, cx - 5, cy, cx + 5, cy);
                if (zone == 1)
                {
                    g.DrawLine(pen, cx, cy - 5, cx, cy + 5);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _anim != null)
            {
                _anim.Dispose();
                _anim = null;
            }
            base.Dispose(disposing);
        }
    }

    internal class SimpleList : Control
    {
        public delegate void ItemDrawer(Graphics g, Rectangle bounds, int index, string text);

        private readonly List<string> _items = new List<string>();
        private ItemDrawer _drawer;
        private int _itemHeight = 28;
        private int _first;
        private int _hoverIndex = -1;
        private bool _dragging;
        private int _dragStartY;
        private int _dragStartFirst;

        public SimpleList()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            TabStop = true;
        }

        public int ItemHeight
        {
            get { return _itemHeight; }
            set { _itemHeight = Math.Max(14, value); Invalidate(); }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public void SetDrawer(ItemDrawer drawer)
        {
            _drawer = drawer;
            Invalidate();
        }

        public void Clear()
        {
            _items.Clear();
            _first = 0;
            _hoverIndex = -1;
            Invalidate();
        }

        public void SetItems(IList<string> items)
        {
            _items.Clear();
            for (int i = 0; i < items.Count; i++) _items.Add(items[i]);
            _first = 0;
            _hoverIndex = -1;
            Invalidate();
        }

        public void Add(string item)
        {
            _items.Add(item);
            ScrollToEnd();
            Invalidate();
        }

        public void Trim(int max)
        {
            while (_items.Count > max) _items.RemoveAt(0);
            if (_first >= _items.Count) _first = Math.Max(0, _items.Count - 1);
        }

        public void ScrollToEnd()
        {
            _first = Math.Max(0, _items.Count - VisibleRows());
        }

        private int VisibleRows()
        {
            return Math.Max(1, Height / Math.Max(1, _itemHeight));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Card);
            int visible = VisibleRows();
            int maxFirst = Math.Max(0, _items.Count - visible);
            if (_first > maxFirst) _first = maxFirst;
            bool showScroll = _items.Count > visible;
            int contentWidth = showScroll ? Width - 8 : Width;
            for (int row = 0; row < visible; row++)
            {
                int index = _first + row;
                if (index >= _items.Count) break;
                Rectangle bounds = new Rectangle(0, row * _itemHeight, contentWidth, _itemHeight);
                if (index == _hoverIndex)
                {
                    using (SolidBrush hover = new SolidBrush(Theme.CardSoft))
                    {
                        g.FillRectangle(hover, bounds);
                    }
                }
                if (_drawer != null) _drawer(g, bounds, index, _items[index]);
                if (index < _items.Count - 1)
                {
                    using (Pen pen = new Pen(Theme.LineSoft, 1f))
                    {
                        g.DrawLine(pen, bounds.X + 8, bounds.Bottom - 1, Width - 10, bounds.Bottom - 1);
                    }
                }
            }
            if (showScroll) DrawScrollbar(g, visible, maxFirst);
        }

        private void DrawScrollbar(Graphics g, int visible, int maxFirst)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int trackTop = 4;
            int trackHeight = Height - 8;
            int thumbHeight = Math.Max(18, (int)((float)trackHeight * visible / Math.Max(1, _items.Count)));
            int travel = Math.Max(1, trackHeight - thumbHeight);
            float ratio = maxFirst <= 0 ? 0f : (float)_first / maxFirst;
            int thumbY = trackTop + (int)Math.Round(travel * ratio);
            using (GraphicsPath path = Theme.RoundedRect(new Rectangle(Width - 5, thumbY, 3, thumbHeight), 1))
            using (SolidBrush thumb = new SolidBrush(Color.FromArgb(205, 195, 178)))
            {
                g.FillPath(thumb, path);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                int visible = VisibleRows();
                int maxFirst = Math.Max(1, _items.Count - visible);
                int trackHeight = Height - 8;
                int thumbHeight = Math.Max(18, (int)((float)trackHeight * visible / Math.Max(1, _items.Count)));
                int travel = Math.Max(1, trackHeight - thumbHeight);
                int delta = e.Y - _dragStartY;
                int newFirst = _dragStartFirst + (int)Math.Round((float)delta * maxFirst / travel);
                _first = Math.Max(0, Math.Min(maxFirst, newFirst));
                Invalidate();
                return;
            }
            int row = e.Y / Math.Max(1, _itemHeight);
            int hover = _first + row;
            if (hover >= _items.Count) hover = -1;
            if (hover != _hoverIndex)
            {
                _hoverIndex = hover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (_items.Count > VisibleRows() && e.X >= Width - 10)
            {
                _dragging = true;
                _dragStartY = e.Y;
                _dragStartFirst = _first;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int visible = VisibleRows();
            int maxFirst = Math.Max(0, _items.Count - visible);
            _first -= e.Delta / 120;
            if (_first < 0) _first = 0;
            if (_first > maxFirst) _first = maxFirst;
            Invalidate();
        }
    }

    internal class WindowButton : Control
    {
        private bool _hover;
        private bool _isClose;
        private Color _surface = Theme.Paper;

        public WindowButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(30, 30);
            Cursor = Cursors.Hand;
            BackColor = Theme.Paper;
        }

        public bool IsClose
        {
            get { return _isClose; }
            set { _isClose = value; Invalidate(); }
        }

        public Color SurfaceColor
        {
            get { return _surface; }
            set { _surface = value; BackColor = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush surface = new SolidBrush(_surface))
            {
                g.FillRectangle(surface, ClientRectangle);
            }
            if (_hover)
            {
                Theme.FillRounded(g, new Rectangle(0, 0, Width - 1, Height - 1), 8, Theme.CardSoft);
            }
            using (Pen pen = new Pen(Theme.InkSoft, 1.6f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int cx = Width / 2;
                int cy = Height / 2;
                if (_isClose)
                {
                    g.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
                    g.DrawLine(pen, cx + 5, cy - 5, cx - 5, cy + 5);
                }
                else
                {
                    g.DrawLine(pen, cx - 6, cy, cx + 6, cy);
                }
            }
        }
    }

    internal class LightColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return Theme.AccentSoft; } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.AccentSoft; } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.AccentSoft; } }
        public override Color ToolStripDropDownBackground { get { return Theme.Card; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Card; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Card; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Card; } }
        public override Color MenuBorder { get { return Theme.Line; } }
        public override Color MenuItemBorder { get { return Theme.Accent; } }
        public override Color SeparatorDark { get { return Theme.Line; } }
        public override Color SeparatorLight { get { return Theme.LineSoft; } }
    }
}
