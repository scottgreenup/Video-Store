using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Common {
    public class Logging {

        // Severity levels from https://tools.ietf.org/html/rfc5424 and added Trace.
        public enum Level {
            Emergency = 0,
            Alert = 1,
            Critical = 2,
            Error = 3,
            Warning = 4,
            Notice = 5,
            Info = 6,
            Debug = 7,
            Trace = 8,
        }

        private Level mLogLevel;
        private List<string> mFilenames;

        private string LevelToString(Level level)
        {
            return level.ToString().ToUpper();
        }

        private static Logging instance;
        private Logging()
        {
            mFilenames = new List<string>();
            mLogLevel = Level.Info;
        }

        public static void SetLevel(Level level)
        {
            Instance.mLogLevel = level;
        }

        public static void AddFile(string filename)
        {
            // string directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            string folder = @"\VideoStore\log\";
            Directory.CreateDirectory(directory + folder);
            Instance.mFilenames.Add(directory + folder + filename);
        }

        public static Logging Instance {
            get {
                if (instance == null) {
                    instance = new Logging();
                }

                return instance;
            }
        }

        private void LogEvent(Level level, string message)
        {
            if (level > mLogLevel) {
                return;
            }

            string levelString = LevelToString(level);
            string timestamp = DateTime.UtcNow.ToString("o");
            string line = $"{timestamp}: [{levelString}] {message}";

            Console.WriteLine(line);

            foreach (string filename in mFilenames) {
                using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter sw = new StreamWriter(fs)) {
                    sw.WriteLine(line);
                }
            }

        }

        public static void Debug(string message)
        {
            Instance.LogEvent(Level.Debug, message);
        }

        public static void Info(string message)
        {
            Instance.LogEvent(Level.Info, message);
        }

        public static void Notice(string message)
        {
            Instance.LogEvent(Level.Notice, message);
        }

        public static void Warning(string message)
        {
            Instance.LogEvent(Level.Warning, message);
        }

        public static void Error(string message)
        {
            Instance.LogEvent(Level.Error, message);
        }

        public static void Critical(string message)
        {
            Instance.LogEvent(Level.Critical, message);
        }

        public static void Alert(string message)
        {
            Instance.LogEvent(Level.Alert, message);
        }

        public static void Emergency(string message)
        {
            Instance.LogEvent(Level.Emergency, message);
        }
    }
}
