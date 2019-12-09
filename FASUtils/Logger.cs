using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace FASUtils
{
    public class Logger
    {
        private static string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Assembly.GetExecutingAssembly().GetName().Name);
        public static string BasePath { get { return basePath; } set { basePath = value; Directory.CreateDirectory(value); } }
        public static string logpath { get { return Path.Combine(BasePath, "log.txt"); } }
        public static string sessionLogpath { get { return Path.Combine(BasePath, "session_log.txt"); } }
        public static bool WriteFile = true;
        public static bool PrintConsole = true;
        public static bool DebugMode = false;
        public static int WriteTimeout = 10000;

        public static event EventHandler<string> OnLoggerError;

        public static string LastError = "";

        private static StringBuilder sessionLog = new StringBuilder();
        private static ReaderWriterLock locker = new ReaderWriterLock();

        public static void Init()
        {
            Directory.CreateDirectory(BasePath);
        }

        public static void PrintLn(string msg, bool debug)
        {
            if (!debug || DebugMode)
            {
                if (WriteFile)
                {
                    try
                    {
                        locker.AcquireWriterLock(WriteTimeout);
                        using (StreamWriter w = File.AppendText(logpath))
                        {
                            w.WriteLine(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerError(ex.Message);
                    }
                    finally
                    {
                        locker.ReleaseWriterLock();
                    }
                }
                if (PrintConsole)
                {
                    Console.WriteLine(msg);
                }
                sessionLog.AppendLine(msg);
            }
        }

        private static void LoggerError(string msg)
        {
            OnLoggerError?.Invoke("Logger", msg);
        }

        public static void Debug(string msg, object sender)
        {
            PrintLn(DateTime.Now + " [DEBUG][" + sender + "] " + msg, true);
        }

        public static void Log(string msg, object sender)
        {
            PrintLn(DateTime.Now + " [LOG][" + sender + "] " + msg, false);
        }

        public static void Error(string msg, object sender)
        {
            LastError = DateTime.Now + " [ERROR][" + sender + "] " + msg;
            PrintLn(LastError, false);
        }

        public static void WriteSessionLog()
        {
            try
            {
                File.WriteAllText(sessionLogpath, GetSessionLog());
            }
            catch (Exception ex)
            {
                LoggerError(ex.Message);
            }
        }

        public static string GetSessionLog()
        {
            return sessionLog.ToString();
        }
    }
}
