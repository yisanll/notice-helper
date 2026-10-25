using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace QQNotify
{
    internal class AppSettings
    {
        public string AvatarPath = "";
        public string ScriptPath = "";
        public double IntervalSeconds = 10;
        public bool Loop = false;
        public bool CtrlEnter = false;
        public int MiniX = -1;
        public int MiniY = -1;
        public string LastTargetTitle = "";
        public bool SanZiJing = false;

        /// <summary>数据目录：放在用户本地应用数据，不再在 exe（桌面）旁边生成文件。</summary>
        public static string BaseDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "通知助手");
            }
        }

        public static string DataDir
        {
            get { return Path.Combine(BaseDir, "data"); }
        }

        public static string LogDir
        {
            get { return Path.Combine(BaseDir, "logs"); }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(BaseDir, "settings.ini"); }
        }

        /// <summary>旧版本把 settings.ini/data/logs 放在 exe 同目录。</summary>
        public static string LegacyDir
        {
            get { return Application.StartupPath; }
        }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                MigrateLegacyData();
                if (!File.Exists(SettingsFile)) return s;
                string[] lines = File.ReadAllLines(SettingsFile, Encoding.UTF8);
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "avatar": s.AvatarPath = val; break;
                        case "script": s.ScriptPath = val; break;
                        case "interval": s.IntervalSeconds = ParseDouble(val, 10); break;
                        case "loop": s.Loop = val == "1"; break;
                        case "ctrlEnter": s.CtrlEnter = val == "1"; break;
                        case "miniX":
                            int mx;
                            if (int.TryParse(val, out mx)) s.MiniX = mx;
                            break;
                        case "miniY":
                            int my;
                            if (int.TryParse(val, out my)) s.MiniY = my;
                            break;
                        case "lastTitle": s.LastTargetTitle = val; break;
                        case "sanzijing": s.SanZiJing = val == "1"; break;
                    }
                }
            }
            catch
            {
            }
            if (s.IntervalSeconds < 0.01) s.IntervalSeconds = 10;
            if (s.IntervalSeconds > 3600) s.IntervalSeconds = 3600;
            return s;
        }

        private static double ParseDouble(string text, double fallback)
        {
            double v;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            if (double.TryParse(text, out v)) return v;
            return fallback;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(BaseDir);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# 通知助手设置（自动生成，可直接编辑）");
                sb.AppendLine("avatar=" + AvatarPath);
                sb.AppendLine("script=" + ScriptPath);
                sb.AppendLine("interval=" + IntervalSeconds.ToString("0.##", CultureInfo.InvariantCulture));
                sb.AppendLine("loop=" + (Loop ? "1" : "0"));
                sb.AppendLine("ctrlEnter=" + (CtrlEnter ? "1" : "0"));
                sb.AppendLine("miniX=" + MiniX);
                sb.AppendLine("miniY=" + MiniY);
                sb.AppendLine("lastTitle=" + LastTargetTitle);
                sb.AppendLine("sanzijing=" + (SanZiJing ? "1" : "0"));
                File.WriteAllText(SettingsFile, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        public string ResolveAvatarPath()
        {
            if (string.IsNullOrEmpty(AvatarPath)) return "";
            if (Path.IsPathRooted(AvatarPath)) return AvatarPath;
            return Path.Combine(BaseDir, AvatarPath);
        }

        private static void MigrateLegacyData()
        {
            try
            {
                if (string.Equals(LegacyDir, BaseDir, StringComparison.OrdinalIgnoreCase)) return;
                if (File.Exists(SettingsFile)) return;
                string legacySettings = Path.Combine(LegacyDir, "settings.ini");
                string legacyData = Path.Combine(LegacyDir, "data");
                string legacyLogs = Path.Combine(LegacyDir, "logs");
                if (!File.Exists(legacySettings) && !Directory.Exists(legacyData) && !Directory.Exists(legacyLogs))
                {
                    return;
                }
                Directory.CreateDirectory(BaseDir);
                if (File.Exists(legacySettings)) MoveOrCopyFile(legacySettings, SettingsFile);
                if (Directory.Exists(legacyData) && !Directory.Exists(DataDir))
                {
                    MoveOrCopyDirectory(legacyData, DataDir);
                }
                if (Directory.Exists(legacyLogs) && !Directory.Exists(LogDir))
                {
                    MoveOrCopyDirectory(legacyLogs, LogDir);
                }
            }
            catch
            {
            }
        }

        private static void MoveOrCopyFile(string source, string target)
        {
            try
            {
                File.Move(source, target);
            }
            catch
            {
                try
                {
                    File.Copy(source, target, false);
                }
                catch
                {
                }
            }
        }

        private static void MoveOrCopyDirectory(string source, string target)
        {
            try
            {
                Directory.Move(source, target);
            }
            catch
            {
                CopyDirectory(source, target);
            }
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
            {
                string dest = Path.Combine(target, Path.GetFileName(file));
                if (!File.Exists(dest)) File.Copy(file, dest, false);
            }
            foreach (string dir in Directory.GetDirectories(source))
            {
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }
    }
}
