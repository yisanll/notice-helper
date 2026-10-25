using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QQNotify
{
    internal static class Logger
    {
        private static readonly object Sync = new object();

        public static void Write(string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(AppSettings.LogDir);
                    string file = Path.Combine(AppSettings.LogDir, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine;
                    File.AppendAllText(file, line, new UTF8Encoding(false));
                }
            }
            catch
            {
            }
        }
    }

    internal static class ForegroundGuard
    {
        public static bool IsChatProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;
            string p = processName.Trim().ToLowerInvariant();
            return p == "qq" || p == "qqnt" || p == "wechat" || p == "weixin";
        }

        public static bool IsForeground(IntPtr hwnd, uint pid)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd)) return false;
            if (Native.GetForegroundWindow() != hwnd) return false;
            uint currentPid;
            Native.GetWindowThreadProcessId(hwnd, out currentPid);
            return currentPid == pid;
        }
    }

    internal static class TextInjector
    {
        /// <summary>把文本通过剪贴板粘贴进当前输入框，再按发送键。调用方需保证目标窗口在前台。</summary>
        public static async Task SendAsync(string text, bool ctrlEnter, bool fast)
        {
            if (fast)
            {
                bool copiedFast = false;
                for (int i = 0; i < 3 && !copiedFast; i++)
                {
                    try
                    {
                        Clipboard.SetText(text);
                        copiedFast = true;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(20);
                    }
                }
                if (!copiedFast) throw new InvalidOperationException("剪贴板被占用，写入失败");
                await Task.Delay(8);
                Native.SendCtrlV();
                await Task.Delay(16);
                if (ctrlEnter) Native.SendCtrlEnter();
                else Native.SendEnter();
                await Task.Delay(8);
                return;
            }

            string oldText = null;
            bool hasOld = false;
            try
            {
                if (Clipboard.ContainsText())
                {
                    oldText = Clipboard.GetText();
                    hasOld = true;
                }
            }
            catch
            {
            }

            bool copied = false;
            for (int i = 0; i < 3 && !copied; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    copied = true;
                }
                catch
                {
                    System.Threading.Thread.Sleep(80);
                }
            }
            if (!copied) throw new InvalidOperationException("剪贴板被占用，写入失败");

            await Task.Delay(120);
            Native.SendCtrlV();
            await Task.Delay(160);
            if (ctrlEnter) Native.SendCtrlEnter();
            else Native.SendEnter();
            await Task.Delay(220);

            if (hasOld && oldText != null)
            {
                try
                {
                    Clipboard.SetText(oldText);
                }
                catch
                {
                }
            }
        }
    }
}
