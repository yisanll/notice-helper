using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QQNotify
{
    internal static class ScriptStore
    {
        /// <summary>读取 TXT：一行一条通知，自动识别 UTF-8 / UTF-16 / GBK。</summary>
        public static List<string> Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            string text = Decode(bytes);
            string[] raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            List<string> list = new List<string>();
            foreach (string line in raw)
            {
                string t = line.Trim();
                if (t.Length == 0) continue;
                list.Add(t);
            }
            return list;
        }

        private static string Decode(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            try
            {
                UTF8Encoding strict = new UTF8Encoding(false, true);
                return strict.GetString(bytes);
            }
            catch
            {
                return Encoding.GetEncoding(936).GetString(bytes);
            }
        }
    }

    internal static class MessageRules
    {
        public const int MaxLength = 500;
        public const int MaxSendsPerRun = 200;

        public static bool Validate(string text, out string reason)
        {
            reason = null;
            if (text == null || text.Trim().Length == 0)
            {
                reason = "内容为空";
                return false;
            }
            if (text.Length > MaxLength)
            {
                reason = string.Format("超过 {0} 字", MaxLength);
                return false;
            }
            string lower = text.ToLowerInvariant();
            if (lower.Contains("@全体成员") || lower.Contains("@所有人") ||
                lower.Contains("@all") || lower.Contains("@everyone"))
            {
                reason = "包含 @全体成员 / @所有人，已拦截";
                return false;
            }
            return true;
        }
    }
}
