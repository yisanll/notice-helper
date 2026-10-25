using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QQNotify
{
    internal enum EngineState
    {
        Idle,
        Countdown,
        Running,
        Paused
    }

    internal class SendEngine
    {
        public EngineState State { get; private set; }
        public event Action<EngineState> StateChanged;
        public event Action<int, int> ProgressChanged;
        public event Action<string> StatusMessage;
        public event Action<string> LogMessage;

        private List<string> _lines = new List<string>();
        private CancellationTokenSource _cts;
        private bool _paused;
        private int _index;
        private int _sent;
        private bool _loop;
        private bool _ctrlEnter;
        private IntPtr _target;
        private uint _targetPid;
        private double _interval;
        private int _maxSends = MessageRules.MaxSendsPerRun;

        public SendEngine()
        {
            State = EngineState.Idle;
        }

        public bool IsBusy
        {
            get { return State != EngineState.Idle; }
        }

        public async void Start(List<string> lines, IntPtr target, uint targetPid, double intervalSeconds,
            bool loop, bool ctrlEnter)
        {
            if (State != EngineState.Idle) return;

            _lines = lines;
            _target = target;
            _targetPid = targetPid;
            _interval = Math.Max(0.01, intervalSeconds);
            _loop = loop;
            _ctrlEnter = ctrlEnter;
            _paused = false;
            _index = 0;
            _sent = 0;
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            try
            {
                await RunAsync(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                RaiseStatus("发送异常：" + ex.Message);
                RaiseLog("异常：" + ex.Message);
                Logger.Write("ERROR " + ex);
            }
            finally
            {
                _cts = null;
                _paused = false;
                SetState(EngineState.Idle);
            }
        }

        public void Stop()
        {
            _paused = false;
            if (_cts != null) _cts.Cancel();
        }

        public void TogglePause()
        {
            if (State == EngineState.Idle || State == EngineState.Countdown) return;
            _paused = !_paused;
            SetState(_paused ? EngineState.Paused : EngineState.Running);
            RaiseStatus(_paused ? "已暂停，按 F8 继续" : "继续发送");
        }

        private async Task RunAsync(CancellationToken ct)
        {
            SetState(EngineState.Countdown);
            RaiseStatus("发送即将开始，请在 3 秒内切换到聊天窗口");
            for (int i = 3; i > 0; i--)
            {
                ct.ThrowIfCancellationRequested();
                RaiseStatus(string.Format("{0} 秒后开始…", i));
                await Task.Delay(1000, ct);
            }

            SetState(EngineState.Running);
            bool fastInject = _interval < 1.0;
            Stopwatch clock = Stopwatch.StartNew();
            long lastSendAt = -1;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (_paused)
                {
                    await Task.Delay(200, ct);
                    continue;
                }
                if (State != EngineState.Running) SetState(EngineState.Running);

                if (!ForegroundGuard.IsForeground(_target, _targetPid))
                {
                    _paused = true;
                    SetState(EngineState.Paused);
                    RaiseStatus("聊天窗口不在前台，已暂停。切回后按 F8 继续");
                    RaiseLog("已暂停：聊天窗口不在前台");
                    continue;
                }

                if (_sent >= _maxSends)
                {
                    RaiseStatus(string.Format("本次已达 {0} 条上限，自动停止", _maxSends));
                    RaiseLog("达到单次发送上限，自动停止");
                    break;
                }

                int intervalMs = (int)Math.Round(_interval * 1000.0);
                if (intervalMs < 10) intervalMs = 10;

                // 按“两条消息开始时间之间的周期”等待，间隔由用户设置决定。
                if (lastSendAt >= 0)
                {
                    long sinceMs = clock.ElapsedMilliseconds - lastSendAt;
                    int waitMs = intervalMs - (int)sinceMs;
                    if (waitMs > 0)
                    {
                        RaiseStatus(string.Format("{0} 后发送下一条", FormatInterval(_interval)));
                        await WaitMsAsync(ct, waitMs);
                        if (ct.IsCancellationRequested) break;
                        if (_paused) continue;
                        if (!ForegroundGuard.IsForeground(_target, _targetPid))
                        {
                            _paused = true;
                            SetState(EngineState.Paused);
                            RaiseStatus("聊天窗口不在前台，已暂停。切回后按 F8 继续");
                            RaiseLog("已暂停：聊天窗口不在前台");
                            continue;
                        }
                    }
                }

                string text = _lines[_index];
                string reason;
                if (!MessageRules.Validate(text, out reason))
                {
                    RaiseLog(string.Format("跳过第 {0} 条：{1}", _index + 1, reason));
                    RaiseStatus("跳过一条：" + reason);
                }
                else
                {
                    lastSendAt = clock.ElapsedMilliseconds;
                    await TextInjector.SendAsync(text, _ctrlEnter, fastInject);
                    _sent++;
                    RaiseLog(string.Format("已发送 #{0}  {1}", _sent, Theme.Truncate(text, 24)));
                    if (ProgressChanged != null) ProgressChanged(_sent, _lines.Count);
                }

                _index++;
                if (_index >= _lines.Count)
                {
                    if (!_loop)
                    {
                        RaiseStatus("通知已全部发送完成");
                        RaiseLog("全部发送完成，共 " + _sent + " 条");
                        break;
                    }
                    _index = 0;
                }
            }
        }

        private async Task WaitMsAsync(CancellationToken ct, int ms)
        {
            while (ms > 0)
            {
                ct.ThrowIfCancellationRequested();
                if (_paused) return;
                int step = ms > 80 ? 80 : ms;
                await Task.Delay(step, ct);
                if (_paused) return;
                ms -= step;
            }
        }

        private static string FormatInterval(double seconds)
        {
            if (Math.Abs(seconds - Math.Round(seconds)) < 0.0001)
            {
                return ((int)Math.Round(seconds)) + " 秒";
            }
            return seconds.ToString("0.00") + " 秒";
        }

        private void SetState(EngineState state)
        {
            if (State == state) return;
            State = state;
            if (StateChanged != null) StateChanged(state);
        }

        private void RaiseStatus(string text)
        {
            if (StatusMessage != null) StatusMessage(text);
        }

        private void RaiseLog(string text)
        {
            Logger.Write(text);
            if (LogMessage != null) LogMessage(text);
        }
    }
}
