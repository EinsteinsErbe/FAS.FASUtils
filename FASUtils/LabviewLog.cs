using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorBrowser
{
    public class LabviewLogCollection
    {
        public const string DEFAULT_DIR = @"V:\ProdSysteme\";
        public const string DEFAULT_PREFIX = "semp";
        public const string DEFAULT_REL_PATH = @"\User Settings\LogFiles\";

        private string dir;
        private string prefix;
        private string relPath;

        public LabviewLogCollection(string dir = DEFAULT_DIR, string prefix = DEFAULT_PREFIX, string relPath = DEFAULT_REL_PATH)
        {
            this.dir = dir;
            this.prefix = prefix;
            this.relPath = relPath.Trim('\\');
        }

        public string[] ListLogDirs()
        {
            return Directory.GetDirectories(dir, prefix + '*').Where(d =>
            {
                string path = Path.Combine(d, relPath);
                return Directory.Exists(path) && Directory.GetFiles(path).Length > 0;
            }).Select(d => Path.GetFileName(d)).ToArray();
        }

        public LabviewLog GetLog(string name)
        {
            return new LabviewLog(Path.Combine(dir, name, relPath));
        }
    }

    public class LabviewLog
    {
        private string dir;
        public bool Exists { get { return Directory.Exists(dir); } }
        public string[] logfiles;
        public string[] currentLines;

        public event EventHandler<TextLoadedEventArgs> TextLoaded;

        public class TextLoadedEventArgs : EventArgs
        {
            public string Text { get; set; }
        }

        protected virtual void OnTextLoaded(TextLoadedEventArgs e)
        {
            TextLoaded?.Invoke(this, e);
        }

        internal LabviewLog(string dir)
        {
            this.dir = dir;
            currentLines = new string[0];
            if (Exists)
            {
                logfiles = Directory.GetFiles(dir).Select(s => Path.GetFileName(s)).Reverse().ToArray();
            }
            else
            {
                logfiles = new string[0];
            }
        }

        public bool LoadFile(string path)
        {
            if (File.Exists(path))
            {
                currentLines = File.ReadAllLines(path, Encoding.GetEncoding(1252));
                return true;
            }
            else
            {
                currentLines = new string[] { string.Empty };
                return false;
            }
        }

        public bool LoadLog(string name)
        {
            return LoadFile(Path.Combine(dir, name));
        }

        public string[] GetLines(params string[] filter)
        {
            return currentLines.Where(s => filter.All(f => s.Contains(f))).ToArray();
        }

        public string GetText(params string[] filter)
        {
            string[] lines = GetLines(filter);
            if (lines.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach(string l in lines)
                {
                    sb.AppendLine(l);
                }
                return sb.ToString();
            }
        }

        public void GetTextAsync(params string[] filter)
        {
            Task.Run(() =>
            {
                TextLoadedEventArgs e = new TextLoadedEventArgs
                {
                    Text = GetText(filter)
                };
                OnTextLoaded(e);
            });
        }
    }
}
