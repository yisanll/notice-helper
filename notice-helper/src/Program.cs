using System;
using System.Threading;
using System.Windows.Forms;

namespace QQNotify
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool created;
            using (Mutex mutex = new Mutex(true, "QQNotifyHelper.SingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("通知助手已经在运行。", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    Logger.Write("界面异常：" + e.Exception);
                    MessageBox.Show("程序出现异常：" + e.Exception.Message, "通知助手",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    Logger.Write("未处理异常：" + e.ExceptionObject);
                };

                Application.Run(new MainForm(args));
            }
        }
    }
}
