using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QQNotify
{
    internal class MainForm : Form
    {
        private const int W = 920;
        private const int H = 800;
        private const int HK_START = 1;
        private const int HK_STOP = 2;
        private const int HK_MINI = 3;

        private AppSettings _settings;
        private SendEngine _engine;
        private MiniForm _mini;
        private Image _avatar;
        private List<string> _lines = new List<string>();
        private List<string> _txtLines = new List<string>();
        private string _txtFileName = "";
        private IntPtr _targetHwnd = IntPtr.Zero;
        private uint _targetPid;
        private string _targetTitle = "";
        private EngineState _state = EngineState.Idle;
        private bool _selecting;
        private bool _startInMini;
        private bool _hotkeysOk = true;
        private float _pulse;
        private float _progressShown;
        private float _progressTarget;
        private Timer _animTimer;

        private Label _lblState;
        private Label _lblTargetInfo;
        private Label _lblSent;
        private Label _lblInterval;
        private Label _lblFileInfo;
        private Label _lblRange;
        private Label _lblStatus;
        private Label _lblEmptyPreview;
        private Label _lblEmptyLog;
        private PillButton _btnStart;
        private PillButton _btnStop;
        private PillButton _btnImport;
        private PillButton _btnSelect;
        private LinkButton _btnAvatar;
        private LinkButton _btnClearLog;
        private LinkButton _btnDataDir;
        private WindowButton _btnMini;
        private WindowButton _btnClose;
        private Stepper _stepper;
        private ToggleSwitch _swLoop;
        private SegmentedControl _segKey;
        private ToggleSwitch _swSanZiJing;
        private Label _lblSanZiJing;
        private SimpleList _lstPreview;
        private SimpleList _lstLog;
        private PillButton[] _presets;

        public MainForm(string[] args)
        {
            _settings = AppSettings.Load();
            _engine = new SendEngine();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--mini") _startInMini = true;
            }
            _animTimer = new Timer();
            _animTimer.Interval = 30;
            _animTimer.Tick += delegate { OnAnimationTick(); };

            BuildUi();
            LoadAvatar();
            BindEngine();
            LoadLastScript();
            ApplySettingsToUi();
        }

        // ---------------- UI 构建 ----------------

        private void BuildUi()
        {
            Text = "通知助手";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(W, H);
            BackColor = Theme.Paper;
            Font = Theme.Body;
            DoubleBuffered = true;
            KeyPreview = true;
            Opacity = 0;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            _btnMini = new WindowButton();
            _btnMini.SetBounds(828, 22, 30, 30);
            _btnMini.Click += delegate { EnterMiniMode(); };
            Controls.Add(_btnMini);

            _btnClose = new WindowButton();
            _btnClose.IsClose = true;
            _btnClose.SetBounds(864, 22, 30, 30);
            _btnClose.Click += delegate { Close(); };
            Controls.Add(_btnClose);

            _btnAvatar = new LinkButton();
            _btnAvatar.Text = "更换头像";
            _btnAvatar.SurfaceColor = Theme.Paper;
            _btnAvatar.LinkColor = Theme.Muted;
            _btnAvatar.Font = Theme.Small;
            _btnAvatar.SetBounds(102, 218, 80, 22);
            _btnAvatar.Click += delegate { ChooseAvatar(); };
            Controls.Add(_btnAvatar);

            _btnDataDir = new LinkButton();
            _btnDataDir.Text = "打开数据目录";
            _btnDataDir.SurfaceColor = Theme.Paper;
            _btnDataDir.LinkColor = Theme.Muted;
            _btnDataDir.Font = Theme.Tiny;
            _btnDataDir.SetBounds(26, 664, 216, 24);
            _btnDataDir.Click += delegate
            {
                try
                {
                    Directory.CreateDirectory(AppSettings.BaseDir);
                    System.Diagnostics.Process.Start("explorer.exe", AppSettings.BaseDir);
                }
                catch
                {
                }
            };
            Controls.Add(_btnDataDir);

            _lblState = MakeLabel("待机", Theme.State, Theme.Ink, Theme.Paper, 44, 278, 190, 30);
            _lblTargetInfo = MakeLabel("目标：未选择", Theme.Small, Theme.InkSoft, Theme.Paper, 26, 318, 206, 34);
            _lblTargetInfo.AutoEllipsis = true;
            _lblSent = MakeLabel("已发送 0 / 0 条", Theme.Small, Theme.Muted, Theme.Paper, 26, 360, 206, 20);
            _lblInterval = MakeLabel("间隔 10 秒", Theme.Small, Theme.Muted, Theme.Paper, 26, 382, 206, 20);
            Controls.Add(_lblState);
            Controls.Add(_lblTargetInfo);
            Controls.Add(_lblSent);
            Controls.Add(_lblInterval);

            _btnImport = MakeGhostButton("导入 TXT", 780, 104, 96, 32, Theme.Card);
            _btnImport.Click += delegate { ImportScript(); };
            Controls.Add(_btnImport);

            _lblFileInfo = MakeLabel("未导入文件 · 支持 TXT，一行一条", Theme.Small, Theme.Muted, Theme.Card,
                310, 134, 452, 18);
            _lblFileInfo.AutoEllipsis = true;
            Controls.Add(_lblFileInfo);

            _swSanZiJing = new ToggleSwitch();
            _swSanZiJing.SurfaceColor = Theme.Card;
            _swSanZiJing.SetBounds(596, 104, 44, 24);
            _swSanZiJing.CheckedChanged += delegate
            {
                _settings.SanZiJing = _swSanZiJing.Checked;
                _settings.Save();
                ApplySanZiJingMode(_swSanZiJing.Checked);
                SetStatus(_swSanZiJing.Checked
                    ? "三字经模式：发送「是不是 / 对不对」"
                    : "已切回 TXT 模式");
            };
            Controls.Add(_swSanZiJing);

            _lblSanZiJing = MakeLabel("三字经模式", Theme.Small, Theme.InkSoft, Theme.Card,
                646, 104, 92, 24);
            Controls.Add(_lblSanZiJing);

            _lstPreview = new SimpleList();
            _lstPreview.ItemHeight = 28;
            _lstPreview.SetBounds(310, 158, 564, 94);
            _lstPreview.SetDrawer(DrawPreviewLine);
            Controls.Add(_lstPreview);

            _lblEmptyPreview = MakeLabel("还没有通知，点右上角「导入 TXT」添加", Theme.Small, Theme.Muted,
                Theme.Card, 310, 170, 564, 64);
            _lblEmptyPreview.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_lblEmptyPreview);
            _lblEmptyPreview.BringToFront();

            _lblTargetValue = MakeLabel("未选择", Theme.Body, Theme.Muted, Theme.Card, 406, 338, 330, 24);
            _lblTargetValue.AutoEllipsis = true;
            Controls.Add(_lblTargetValue);

            _btnSelect = MakeGhostButton("选择窗口", 760, 332, 116, 30, Theme.Card);
            _btnSelect.Click += delegate { SelectTarget(); };
            Controls.Add(_btnSelect);

            _stepper = new Stepper();
            _stepper.Value = 10;
            _stepper.Minimum = 0.01;
            _stepper.Maximum = 3600;
            _stepper.Step = 0.01;
            _stepper.Suffix = "秒";
            _stepper.SurfaceColor = Theme.Card;
            _stepper.SetBounds(406, 374, 214, 46);
            _stepper.ValueChanged += delegate
            {
                _settings.IntervalSeconds = _stepper.Value;
                _settings.Save();
                UpdateIntervalUi();
            };
            Controls.Add(_stepper);

            _presets = new PillButton[5];
            string[] presetText = new string[] { "0.1s", "0.25s", "0.5s", "1s", "5s" };
            double[] presetValue = new double[] { 0.1, 0.25, 0.5, 1, 5 };
            for (int i = 0; i < presetText.Length; i++)
            {
                int index = i;
                PillButton b = new PillButton();
                b.Text = presetText[i];
                b.SurfaceColor = Theme.Card;
                b.Fill = Color.Transparent;
                b.HoverFill = Theme.AccentSoft;
                b.TextColor = Theme.Muted;
                b.Font = Theme.Tiny;
                b.Radius = 8;
                b.SetBounds(640 + i * 46, 386, 42, 24);
                b.Click += delegate { _stepper.Value = presetValue[index]; };
                _presets[i] = b;
                Controls.Add(b);
            }

            _lblRange = MakeLabel("0.01 ~ 3600 秒 · 按设定周期发送", Theme.Tiny, Theme.Muted, Theme.Card,
                640, 414, 236, 18);
            Controls.Add(_lblRange);

            _segKey = new SegmentedControl();
            _segKey.Items = new string[] { "Enter", "Ctrl+Enter" };
            _segKey.SelectedIndex = 0;
            _segKey.SurfaceColor = Theme.Card;
            _segKey.Font = Theme.Small;
            _segKey.SetBounds(406, 440, 220, 36);
            _segKey.SelectedIndexChanged += delegate
            {
                _settings.CtrlEnter = _segKey.SelectedIndex == 1;
                _settings.Save();
            };
            Controls.Add(_segKey);

            _swLoop = new ToggleSwitch();
            _swLoop.SurfaceColor = Theme.Card;
            _swLoop.SetBounds(406, 500, 44, 24);
            _swLoop.CheckedChanged += delegate
            {
                _settings.Loop = _swLoop.Checked;
                _settings.Save();
            };
            Controls.Add(_swLoop);

            _btnStart = new PillButton();
            _btnStart.Text = "开始发送";
            _btnStart.PlayIcon = true;
            _btnStart.SurfaceColor = Theme.Paper;
            _btnStart.Fill = Theme.Accent;
            _btnStart.HoverFill = Theme.AccentDark;
            _btnStart.TextColor = Color.White;
            _btnStart.Radius = 14;
            _btnStart.SetBounds(290, 548, 180, 52);
            _btnStart.Click += delegate { StartOrPause(); };
            Controls.Add(_btnStart);

            _btnStop = new PillButton();
            _btnStop.Text = "停止";
            _btnStop.SurfaceColor = Theme.Paper;
            _btnStop.Fill = Color.Transparent;
            _btnStop.HoverFill = Theme.CardSoft;
            _btnStop.TextColor = Theme.InkSoft;
            _btnStop.BorderColor = Theme.Line;
            _btnStop.Radius = 14;
            _btnStop.SetBounds(482, 548, 100, 52);
            _btnStop.Enabled = false;
            _btnStop.Click += delegate { StopSending(); };
            Controls.Add(_btnStop);

            _lblStatus = MakeLabel("先导入 TXT，再选择 QQ 或微信窗口", Theme.Small, Theme.Muted, Theme.Paper,
                594, 562, 300, 24);
            _lblStatus.TextAlign = ContentAlignment.MiddleRight;
            _lblStatus.AutoEllipsis = true;
            Controls.Add(_lblStatus);

            _lstLog = new SimpleList();
            _lstLog.ItemHeight = 22;
            _lstLog.SetBounds(310, 668, 564, 92);
            _lstLog.SetDrawer(DrawLogLine);
            Controls.Add(_lstLog);

            _lblEmptyLog = MakeLabel("发送记录会显示在这里", Theme.Small, Theme.Muted,
                Theme.Card, 310, 680, 564, 64);
            _lblEmptyLog.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_lblEmptyLog);
            _lblEmptyLog.BringToFront();

            _btnClearLog = new LinkButton();
            _btnClearLog.Text = "清空";
            _btnClearLog.SurfaceColor = Theme.Card;
            _btnClearLog.LinkColor = Theme.Muted;
            _btnClearLog.Font = Theme.Tiny;
            _btnClearLog.SetBounds(804, 642, 60, 20);
            _btnClearLog.Click += delegate
            {
                _lstLog.Clear();
                _lblEmptyLog.Visible = true;
            };
            Controls.Add(_btnClearLog);
        }

        private Label MakeLabel(string text, Font font, Color color, Color back, int x, int y, int w, int h)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = font;
            l.ForeColor = color;
            l.BackColor = back;
            l.AutoSize = false;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.UseMnemonic = false;
            l.SetBounds(x, y, w, h);
            return l;
        }

        private PillButton MakeGhostButton(string text, int x, int y, int w, int h, Color surface)
        {
            PillButton b = new PillButton();
            b.Text = text;
            b.SurfaceColor = surface;
            b.Fill = Color.Transparent;
            b.HoverFill = Theme.AccentSoft;
            b.TextColor = Theme.InkSoft;
            b.BorderColor = Theme.Line;
            b.Radius = 9;
            b.Font = Theme.Small;
            b.SetBounds(x, y, w, h);
            return b;
        }

        private Label _lblTargetValue;

        // ---------------- 绘制 ----------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush bg = new SolidBrush(Theme.Paper))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            DrawHeader(g);
            DrawCards(g);
            DrawLeftRail(g);
            DrawSettingsText(g);
            DrawProgress(g);
        }

        private void DrawHeader(Graphics g)
        {
            Rectangle logo = new Rectangle(26, 20, 34, 34);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(22, 120, 70, 45)))
            {
                g.FillEllipse(shadow, 28, 52, 30, 6);
            }
            Theme.FillRounded(g, logo, 10, Theme.Accent);
            Theme.DrawBell(g, new Rectangle(34, 27, 18, 18), Color.White);

            TextRenderer.DrawText(g, "通知助手", Theme.Title, new Rectangle(72, 21, 320, 32),
                Theme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            Size titleSize = TextRenderer.MeasureText("通知助手", Theme.Title);
            Rectangle pill = new Rectangle(72 + titleSize.Width + 8, 27, 72, 20);
            Theme.FillRounded(g, pill, 10, Theme.AccentSoft);
            TextRenderer.DrawText(g, "QQ · 微信", Theme.Tiny, pill, Theme.AccentDark,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using (Pen pen = new Pen(Theme.LineSoft, 1f))
            {
                g.DrawLine(pen, 26, 78, W - 26, 78);
            }
        }

        private void DrawCards(Graphics g)
        {
            Theme.DrawCard(g, new Rectangle(290, 92, 604, 176), 16);
            Theme.DrawCard(g, new Rectangle(290, 280, 604, 252), 16);
            Theme.DrawCard(g, new Rectangle(290, 628, 604, 148), 16);

            DrawSectionTitle(g, "通知内容", 310, 110);
            DrawSectionTitle(g, "发送设置", 310, 298);
            DrawSectionTitle(g, "运行日志", 310, 644);
            DrawTinyRight(g, "自动保存", 874, 300);
        }

        private void DrawSectionTitle(Graphics g, string text, int x, int y)
        {
            TextRenderer.DrawText(g, text, Theme.Section, new Rectangle(x, y, 300, 24), Theme.Ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawTinyRight(Graphics g, string text, int right, int y)
        {
            TextRenderer.DrawText(g, text, Theme.Tiny, new Rectangle(right - 120, y, 120, 18), Theme.Muted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawLeftRail(Graphics g)
        {
            using (Pen divider = new Pen(Theme.LineSoft, 1f))
            {
                g.DrawLine(divider, 266, 98, 266, 736);
            }

            using (SolidBrush halo = new SolidBrush(Theme.AccentSoft))
            {
                g.FillEllipse(halo, 80, 90, 124, 124);
            }
            if (_state == EngineState.Running || _state == EngineState.Countdown)
            {
                float a = 0.5f + 0.5f * (float)Math.Sin(_pulse);
                using (Pen ring = new Pen(Color.FromArgb((int)(95 * a), Theme.Accent), 2f))
                {
                    g.DrawEllipse(ring, 76, 86, 132, 132);
                }
            }
            Theme.DrawAvatar(g, new Rectangle(86, 96, 112, 112), _avatar, Color.White, 4);

            TextRenderer.DrawText(g, "当前状态", Theme.Tiny, new Rectangle(26, 254, 120, 18), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            Color sc = StateColor();
            if (_state == EngineState.Running || _state == EngineState.Countdown)
            {
                float a = 0.5f + 0.5f * (float)Math.Sin(_pulse);
                using (Pen ring = new Pen(Color.FromArgb((int)(80 * a), sc), 2f))
                {
                    g.DrawEllipse(ring, 21, 285, 19, 19);
                }
            }
            Theme.DrawDot(g, 26, 290, 9, sc);

            TextRenderer.DrawText(g, "快捷键", Theme.Tiny, new Rectangle(26, 474, 120, 18), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            DrawKeyCap(g, "F8", 26, 498);
            DrawKeyCap(g, "F9", 26, 524);
            DrawKeyCap(g, "F7", 26, 550);
            TextRenderer.DrawText(g, "开始 / 暂停", Theme.Small, new Rectangle(66, 498, 180, 20), Theme.InkSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "停止发送", Theme.Small, new Rectangle(66, 524, 180, 20), Theme.InkSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "显示 / 迷你窗口", Theme.Small, new Rectangle(66, 550, 180, 20), Theme.InkSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(g, "发送时请保持 QQ / 微信窗口在前台，", Theme.Tiny,
                new Rectangle(26, 616, 218, 18), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "切换窗口会自动暂停。", Theme.Tiny,
                new Rectangle(26, 634, 218, 18), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawKeyCap(Graphics g, string text, int x, int y)
        {
            Rectangle cap = new Rectangle(x, y, 30, 20);
            Theme.FillRounded(g, cap, 6, Theme.Card);
            Theme.DrawRounded(g, cap, 6, Theme.Line, 1f);
            TextRenderer.DrawText(g, text, Theme.KeyCap, cap, Theme.InkSoft,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private void DrawSettingsText(Graphics g)
        {
            TextRenderer.DrawText(g, "目标窗口", Theme.Body, new Rectangle(310, 338, 90, 24), Theme.InkSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "发送间隔", Theme.Body, new Rectangle(310, 386, 90, 24), Theme.InkSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "发送按键", Theme.Body, new Rectangle(310, 448, 90, 24), Theme.InkSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "循环发送", Theme.Body, new Rectangle(310, 500, 90, 24), Theme.InkSoft,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "与聊天软件设置一致", Theme.Tiny, new Rectangle(644, 448, 230, 20), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, "发完最后一条后从头开始，每次上限 200 条", Theme.Tiny,
                new Rectangle(464, 500, 400, 20), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using (Pen pen = new Pen(Theme.LineSoft, 1f))
            {
                g.DrawLine(pen, 310, 372, 874, 372);
                g.DrawLine(pen, 310, 434, 874, 434);
                g.DrawLine(pen, 310, 486, 874, 486);
            }
        }

        private void DrawProgress(Graphics g)
        {
            Rectangle track = new Rectangle(290, 612, 604, 3);
            Theme.FillRounded(g, track, 2, Theme.LineSoft);
            if (_progressShown > 0.001f)
            {
                int w = (int)Math.Round(604 * _progressShown);
                if (w < 3) w = 3;
                Theme.FillRounded(g, new Rectangle(290, 612, w, 3), 2, Theme.Accent);
            }
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

        private void OnAnimationTick()
        {
            bool full = false;
            if (Opacity < 1f)
            {
                Opacity = Math.Min(1f, Opacity + 0.10f);
                full = true;
            }
            if (_state == EngineState.Running || _state == EngineState.Countdown)
            {
                _pulse += 0.13f;
                Invalidate(new Rectangle(18, 270, 232, 70));
                Invalidate(new Rectangle(70, 80, 150, 150));
            }
            if (Math.Abs(_progressShown - _progressTarget) > 0.002f)
            {
                _progressShown += (_progressTarget - _progressShown) * 0.20f;
                if (Math.Abs(_progressShown - _progressTarget) < 0.002f) _progressShown = _progressTarget;
                Invalidate(new Rectangle(286, 606, 612, 14));
            }
            if (full) Invalidate();
        }

        // ---------------- 列表绘制 ----------------

        private void DrawPreviewLine(Graphics g, Rectangle bounds, int index, string text)
        {
            TextRenderer.DrawText(g, (index + 1).ToString("00"), Theme.Tiny,
                new Rectangle(bounds.X + 10, bounds.Y, 28, bounds.Height), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, text, Theme.Body,
                new Rectangle(bounds.X + 42, bounds.Y, bounds.Width - 52, bounds.Height), Theme.Ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }

        private void DrawLogLine(Graphics g, Rectangle bounds, int index, string text)
        {
            Color color = Theme.InkSoft;
            if (text.IndexOf("已发送", StringComparison.Ordinal) >= 0) color = Theme.Ink;
            if (text.IndexOf("跳过", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("暂停", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("达到", StringComparison.Ordinal) >= 0) color = Theme.Warn;
            if (text.IndexOf("异常", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("错误", StringComparison.Ordinal) >= 0) color = Theme.Danger;
            TextRenderer.DrawText(g, text, Theme.Small,
                new Rectangle(bounds.X + 10, bounds.Y, bounds.Width - 20, bounds.Height), color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // ---------------- 数据与设置 ----------------

        private void LoadAvatar()
        {
            try
            {
                string path = _settings.ResolveAvatarPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    _avatar = LoadImageCopy(path);
                }
            }
            catch
            {
            }
        }

        private static Image LoadImageCopy(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image img = Image.FromStream(fs))
            {
                return new Bitmap(img);
            }
        }

        private void LoadLastScript()
        {
            if (string.IsNullOrEmpty(_settings.ScriptPath) || !File.Exists(_settings.ScriptPath)) return;
            try
            {
                List<string> lines = ScriptStore.Load(_settings.ScriptPath);
                if (lines.Count > 0)
                {
                    _txtLines = lines;
                    _txtFileName = Path.GetFileName(_settings.ScriptPath);
                    _lines = new List<string>(lines);
                    UpdateFileInfo();
                    RefreshPreview();
                }
            }
            catch
            {
            }
        }

        private void ApplySettingsToUi()
        {
            _stepper.Value = _settings.IntervalSeconds;
            _swLoop.Checked = _settings.Loop;
            _segKey.SelectedIndex = _settings.CtrlEnter ? 1 : 0;
            _swSanZiJing.Checked = _settings.SanZiJing;
            ApplySanZiJingMode(_settings.SanZiJing);
            UpdateIntervalUi();
            _lblState.Text = "待机";
        }

        private void ApplySanZiJingMode(bool on)
        {
            _lines = on ? BuildSanZiJingLines() : new List<string>(_txtLines);
            if (_btnImport != null) _btnImport.Visible = !on;
            UpdateFileInfo();
            RefreshPreview();
        }

        private static List<string> BuildSanZiJingLines()
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                lines.Add(i % 2 == 0 ? "是不是" : "对不对");
            }
            return lines;
        }

        private void UpdateIntervalUi()
        {
            _lblInterval.Text = "间隔 " + FormatSeconds(_stepper.Value);
            if (_stepper.Value < 1.0)
            {
                _lblRange.Text = "低于 1 秒，自动压缩每条等待";
                _lblRange.ForeColor = Theme.Warn;
            }
            else
            {
                _lblRange.Text = "0.01 ~ 3600 秒 · 按设定周期发送";
                _lblRange.ForeColor = Theme.Muted;
            }
        }

        private static string FormatSeconds(double seconds)
        {
            if (Math.Abs(seconds - Math.Round(seconds)) < 0.0001)
            {
                return ((int)Math.Round(seconds)) + " 秒";
            }
            return seconds.ToString("0.00") + " 秒";
        }

        private void RefreshPreview()
        {
            _lstPreview.SetItems(_lines);
            _lblEmptyPreview.Visible = _lines.Count == 0;
            if (_engine.State == EngineState.Idle)
            {
                _lblSent.Text = string.Format("已发送 0 / {0} 条", _lines.Count);
            }
        }

        private void UpdateFileInfo()
        {
            if (_swSanZiJing != null && _swSanZiJing.Checked)
            {
                _lblFileInfo.Text = "三字经模式 · 是不是 / 对不对 交替 · 共 " + _lines.Count + " 条";
                return;
            }
            if (_lines.Count == 0)
            {
                _lblFileInfo.Text = "未导入文件 · 支持 TXT，一行一条";
            }
            else
            {
                _lblFileInfo.Text = Theme.Truncate(_txtFileName, 30) + " · 共 " + _lines.Count + " 条";
            }
        }

        // ---------------- 事件绑定 ----------------

        private void BindEngine()
        {
            _engine.StateChanged += OnEngineStateChanged;
            _engine.ProgressChanged += OnEngineProgress;
            _engine.StatusMessage += OnEngineStatus;
            _engine.LogMessage += OnEngineLog;
        }

        private void UnbindEngine()
        {
            _engine.StateChanged -= OnEngineStateChanged;
            _engine.ProgressChanged -= OnEngineProgress;
            _engine.StatusMessage -= OnEngineStatus;
            _engine.LogMessage -= OnEngineLog;
        }

        private void OnEngineStateChanged(EngineState state)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<EngineState>(OnEngineStateChanged), state);
                return;
            }
            _state = state;
            Color stateColor;
            switch (state)
            {
                case EngineState.Idle:
                    stateColor = Theme.Muted;
                    _lblState.Text = "待机";
                    _btnStart.Text = "开始发送";
                    _btnStart.PlayIcon = true;
                    _btnStart.Fill = Theme.Accent;
                    _btnStart.HoverFill = Theme.AccentDark;
                    _btnStart.TextColor = Color.White;
                    _btnStart.BorderColor = Color.Empty;
                    _btnStart.Enabled = true;
                    _btnStop.Enabled = false;
                    break;
                case EngineState.Countdown:
                    stateColor = Theme.Accent;
                    _lblState.Text = "准备中";
                    _btnStart.Text = "准备中";
                    _btnStart.PlayIcon = false;
                    _btnStart.Fill = Theme.AccentSoft;
                    _btnStart.HoverFill = Theme.AccentSoft;
                    _btnStart.TextColor = Theme.AccentDark;
                    _btnStart.BorderColor = Color.Empty;
                    _btnStart.Enabled = false;
                    _btnStop.Enabled = true;
                    break;
                case EngineState.Running:
                    stateColor = Theme.Success;
                    _lblState.Text = "运行中";
                    _btnStart.Text = "暂停";
                    _btnStart.PlayIcon = false;
                    _btnStart.Fill = Color.Transparent;
                    _btnStart.HoverFill = Theme.CardSoft;
                    _btnStart.TextColor = Theme.InkSoft;
                    _btnStart.BorderColor = Theme.Line;
                    _btnStart.Enabled = true;
                    _btnStop.Enabled = true;
                    break;
                default:
                    stateColor = Theme.Warn;
                    _lblState.Text = "已暂停";
                    _btnStart.Text = "继续";
                    _btnStart.PlayIcon = true;
                    _btnStart.Fill = Theme.Accent;
                    _btnStart.HoverFill = Theme.AccentDark;
                    _btnStart.TextColor = Color.White;
                    _btnStart.BorderColor = Color.Empty;
                    _btnStart.Enabled = true;
                    _btnStop.Enabled = true;
                    break;
            }
            _lblState.ForeColor = stateColor == Theme.Muted ? Theme.Ink : stateColor;
            if (_mini != null) _mini.SetState(state);
            Invalidate(new Rectangle(18, 270, 232, 70));
            Invalidate(new Rectangle(70, 80, 150, 150));
        }

        private void OnEngineProgress(int sent, int total)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int>(OnEngineProgress), sent, total);
                return;
            }
            _lblSent.Text = string.Format("已发送 {0} / {1} 条", sent, total);
            _progressTarget = total > 0 ? (float)sent / total : 0f;
        }

        private void OnEngineStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnEngineStatus), text);
                return;
            }
            _lblStatus.Text = text;
        }

        private void OnEngineLog(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnEngineLog), text);
                return;
            }
            AppendLog(text);
        }

        private void SetStatus(string text)
        {
            _lblStatus.Text = text;
        }

        private void AppendLog(string text)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "   " + text;
            _lstLog.Add(line);
            _lstLog.Trim(200);
            _lblEmptyLog.Visible = false;
        }

        // ---------------- 操作 ----------------

        private void ImportScript()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
                dlg.Title = "选择通知内容（一行一条）";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                ImportFile(dlg.FileName);
            }
        }

        private void ImportFile(string path)
        {
            try
            {
                List<string> lines = ScriptStore.Load(path);
                if (lines.Count == 0)
                {
                    MessageBox.Show(this, "文件里没有有效内容。\n请每行写一条通知，空行会自动忽略。",
                        "没有内容", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _txtLines = lines;
                _txtFileName = Path.GetFileName(path);
                if (!_swSanZiJing.Checked) _lines = lines;
                _settings.ScriptPath = path;
                _settings.Save();
                UpdateFileInfo();
                RefreshPreview();
                SetStatus(_swSanZiJing.Checked
                    ? "已保存 " + _txtLines.Count + " 条，关闭三字经模式后生效"
                    : "已导入 " + _txtLines.Count + " 条通知");
                Logger.Write("导入脚本：" + path + "，共 " + _txtLines.Count + " 条");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "读取文件失败：" + ex.Message, "导入失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SelectTarget()
        {
            if (_engine.IsBusy)
            {
                SetStatus("正在发送，请先停止再更改目标窗口");
                return;
            }
            if (_selecting) return;
            _selecting = true;
            _btnSelect.Enabled = false;
            try
            {
                for (int i = 5; i >= 1; i--)
                {
                    SetStatus(string.Format("请切换到 QQ 或微信聊天窗口，{0} 秒后自动锁定…", i));
                    await Task.Delay(1000);
                }
                IntPtr hwnd = Native.GetForegroundWindow();
                if (hwnd == IntPtr.Zero || hwnd == Handle)
                {
                    SetStatus("没有捕获到窗口，请重试");
                    return;
                }
                uint pid;
                Native.GetWindowThreadProcessId(hwnd, out pid);
                string process = Native.GetProcessName(pid);
                if (!ForegroundGuard.IsChatProcess(process))
                {
                    string msg = string.IsNullOrEmpty(process) ? "未知程序" : process;
                    SetStatus("当前前台窗口不是 QQ / 微信（" + msg + "），请重试");
                    MessageBox.Show(this,
                        "没有捕获到 QQ / 微信窗口（当前前台是：" + msg + "）。\n\n请先打开 QQ 或微信聊天窗口并切换到前台，" +
                        "再点“选择窗口”。", "未能锁定窗口", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string title = Native.GetWindowTitle(hwnd);
                if (string.IsNullOrEmpty(title)) title = "QQ / 微信聊天窗口";
                _targetHwnd = hwnd;
                _targetPid = pid;
                _targetTitle = title;
                _lblTargetValue.Text = Theme.Truncate(title, 28);
                _lblTargetValue.ForeColor = Theme.Ink;
                _lblTargetInfo.Text = "目标：" + Theme.Truncate(title, 22);
                _settings.LastTargetTitle = title;
                _settings.Save();
                SetStatus("已锁定窗口：" + Theme.Truncate(title, 26));
                Logger.Write("锁定窗口：" + title + " (PID " + pid + ")");
            }
            finally
            {
                _selecting = false;
                _btnSelect.Enabled = true;
            }
        }

        private void StartOrPause()
        {
            if (_engine.State == EngineState.Idle) StartSending();
            else _engine.TogglePause();
        }

        private void StartSending()
        {
            if (_lines.Count == 0)
            {
                MessageBox.Show(this, "请先导入 TXT 文件（一行一条通知）。", "没有通知内容",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_targetHwnd == IntPtr.Zero || !Native.IsWindow(_targetHwnd))
            {
                MessageBox.Show(this, "请先点击“选择窗口”，锁定 QQ 或微信聊天窗口。", "没有目标窗口",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_targetPid == 0 || Native.GetProcessName(_targetPid) == "")
            {
                SetStatus("目标聊天窗口似乎已关闭，请重新选择窗口");
                return;
            }

            _settings.IntervalSeconds = _stepper.Value;
            _settings.Loop = _swLoop.Checked;
            _settings.CtrlEnter = _segKey.SelectedIndex == 1;
            _settings.Save();

            if (Native.IsIconic(_targetHwnd)) Native.ShowWindow(_targetHwnd, 9);
            Native.SetForegroundWindow(_targetHwnd);

            _lstLog.Clear();
            _lblEmptyLog.Visible = true;
            _progressShown = 0f;
            _progressTarget = 0f;
            _lblSent.Text = "已发送 0 / " + _lines.Count + " 条";
            SetStatus("即将开始，请保持聊天窗口在前台");
            _engine.Start(_lines, _targetHwnd, _targetPid, _stepper.Value, _swLoop.Checked,
                _segKey.SelectedIndex == 1);
        }

        private void StopSending()
        {
            if (_engine.IsBusy)
            {
                _engine.Stop();
                SetStatus("已停止");
            }
        }

        private void ChooseAvatar()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "图片文件(*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
                dlg.Title = "选择头像图片";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    Image img = LoadImageCopy(dlg.FileName);
                    if (img == null) return;
                    Directory.CreateDirectory(AppSettings.DataDir);
                    string dest = Path.Combine(AppSettings.DataDir, "avatar.png");
                    img.Save(dest, System.Drawing.Imaging.ImageFormat.Png);
                    if (_avatar != null) _avatar.Dispose();
                    _avatar = img;
                    _settings.AvatarPath = Path.Combine("data", "avatar.png");
                    _settings.Save();
                    Invalidate(new Rectangle(76, 86, 132, 132));
                    if (_mini != null) _mini.SetAvatar(_avatar);
                    SetStatus("头像已更新");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "头像读取失败：" + ex.Message, "头像",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // ---------------- 迷你模式 ----------------

        public void EnterMiniMode()
        {
            if (_mini == null) _mini = new MiniForm(this);
            _mini.SetAvatar(_avatar);
            _mini.SetState(_engine.State);
            _mini.ShowAt(_settings.MiniX, _settings.MiniY);
            Hide();
        }

        public void RestoreFromMini()
        {
            if (_mini != null)
            {
                _settings.MiniX = _mini.Left;
                _settings.MiniY = _mini.Top;
                _settings.Save();
                _mini.Hide();
            }
            Opacity = 1f;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            Invalidate();
        }

        public void ToggleStartPauseFromMini()
        {
            StartOrPause();
        }

        public void StopFromMini()
        {
            StopSending();
        }

        public void SaveMiniPosition(int x, int y)
        {
            _settings.MiniX = x;
            _settings.MiniY = y;
            _settings.Save();
        }

        public void ExitFromMini()
        {
            if (_mini != null) _mini.Hide();
            Close();
        }

        public EngineState CurrentState
        {
            get { return _engine.State; }
        }

        // ---------------- 窗口与热键 ----------------

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Native.timeBeginPeriod(1);
            ApplyRoundedRegion();
            RegisterHotkeys();
            _animTimer.Start();
            if (_startInMini) BeginInvoke(new Action(EnterMiniMode));
        }

        private void ApplyRoundedRegion()
        {
            IntPtr rgn = Native.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 20, 20);
            if (rgn != IntPtr.Zero) Native.SetWindowRgn(Handle, rgn, true);
        }

        private void RegisterHotkeys()
        {
            bool ok = true;
            ok = Native.RegisterHotKey(Handle, HK_START, 0, Native.VK_F8) && ok;
            ok = Native.RegisterHotKey(Handle, HK_STOP, 0, Native.VK_F9) && ok;
            ok = Native.RegisterHotKey(Handle, HK_MINI, 0, Native.VK_F7) && ok;
            _hotkeysOk = ok;
            if (!ok) Logger.Write("部分全局热键注册失败（可能被其他软件占用）");
        }

        private void UnregisterHotkeys()
        {
            Native.UnregisterHotKey(Handle, HK_START);
            Native.UnregisterHotKey(Handle, HK_STOP);
            Native.UnregisterHotKey(Handle, HK_MINI);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HK_START) StartOrPause();
                else if (id == HK_STOP) StopSending();
                else if (id == HK_MINI)
                {
                    if (Visible) EnterMiniMode();
                    else RestoreFromMini();
                }
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.Y < 78)
            {
                Native.DragWindow(Handle);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_engine.IsBusy)
            {
                DialogResult r = MessageBox.Show(this, "正在发送通知，确定要退出吗？", "退出",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            UnbindEngine();
            _engine.Stop();
            if (_mini != null)
            {
                _settings.MiniX = _mini.Left;
                _settings.MiniY = _mini.Top;
                _mini.Dispose();
                _mini = null;
            }
            _settings.Save();
            UnregisterHotkeys();
            Native.timeEndPeriod(1);
            _animTimer.Stop();
            _animTimer.Dispose();
            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }
    }
}
