using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace Logger
{
    public static class Log
    {
        static string filepath = Connect.FilePaths.Logs;
        public static int errorcounter { get; private set; } =  0;
        public static int crashcounter { get; private set; } = 0;

        public static event EventHandler<LoggerEventArgs> InfotoForm;
        //public static event EventHandler<LoggerEventArgs> MessagetoForm;
        public class LoggerEventArgs : EventArgs
        {
            public string log { get; set; }
        }

        static Log()
        {
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
        }

        public static string error(string errormessage)
        {
            ++errorcounter;
            string path = filepath;
            errormessage = "///" + DateTime.Now.ToString("hh:mm.ss") + "/// " + errormessage;
            path = path + "error " + DateTime.Today.ToString("MM-dd-yyyy") + ".log";
            using (StreamWriter streamWriter = new StreamWriter(path, true))
            {
                streamWriter.WriteLine(errormessage);
                streamWriter.Close();
            }
            InfotoForm?.Invoke(null, new LoggerEventArgs() { log = errormessage });
            return errormessage;

        }

        public static string info(string infomessage)
        {
            string path = filepath;
            infomessage = "///" + DateTime.Now.ToString("hh:mm.ss") + "/// " + infomessage;
            path = path + "log " + DateTime.Today.ToString("MM-dd-yyyy") + ".log";
            using (StreamWriter streamWriter = new StreamWriter(path, true))
            {
                streamWriter.WriteLine(infomessage);
                streamWriter.Close();
            }
            InfotoForm?.Invoke(null, new LoggerEventArgs() { log = infomessage });
            return infomessage;
        }

        public static string crash(string crashmessage)
        {
            ++crashcounter;
            string path = filepath;
            crashmessage = "///" + DateTime.Now.ToString("hh:mm.ss") + "/// " + crashmessage;
            path = path + "crash " + DateTime.Today.ToString("MM-dd-yyyy") + ".log";
            using (StreamWriter streamWriter = new StreamWriter(path, true))
            {
                streamWriter.WriteLine(crashmessage);
                streamWriter.Close();
            }
            return crashmessage;
        }

        public static void reset()
        {
            errorcounter = 0;
        }

        public static string message(string messages)
        {
            string path = filepath;
            messages = "///" + DateTime.Now.ToString("hh:mm.ss") + "/// " + messages;
            path = path + "messages " + DateTime.Today.ToString("MM-dd-yyyy") + ".log";
            using (StreamWriter streamWriter = new StreamWriter(path, true))
            {
                streamWriter.WriteLine(messages);
                streamWriter.Close();
            }
            return path;
        }
    }

    public static class History
    {
        static string filepath = Connect.FilePaths.History;
        public static string GetPath(int? ID)
        {
            if (ID == null || ID == 0)
            {
                return null;
            }
            string file = filepath + ID + ".log";
            if (File.Exists(file))
            {
                return file;
            }

            return null;
        }
        public static string Get(int? ID)
        {
            if (ID == null || ID == 0)
            {
                return null;
            }
            string file = filepath + ID + ".log";
            if (File.Exists(file))
            {
                string info = File.ReadAllText(file);
                if (!string.IsNullOrEmpty(info))
                {
                    return info;
                }
                else
                {
                    return "No history was found.";
                }
            }
            else
            {
                File.Create(file).Close();
                return "File not found. New file was created.";
            }
        }
        public static void Record(int? ID, string recording)
        {
            if (ID == null || ID == 0)
            {
                return;
            }
            string file = filepath + ID + ".log";
            recording = "(" + DateTime.Now.ToString("MM/dd/yy hh:mm.ss") + ")\r\n" + recording + "\r\n";

            if (File.Exists(file))
            {
                recording += "\r\n" + File.ReadAllText(file);
            }

            using (StreamWriter streamWriter = new StreamWriter(file, false))
            {
                streamWriter.WriteLine(recording);
                streamWriter.Close();
            }
        }
    }

    public static class MethodWatch
    {
        static Dictionary<string, Stopwatch> watches = new Dictionary<string, Stopwatch>();

        public static void Start(string methodname)
        {
            if (!watches.ContainsKey(methodname))
            {
                watches.Add(methodname, new Stopwatch());
            }

            watches[methodname].Restart();
        }

        public static void Stop(string methodname)
        {
            string msg = null;
            if (!watches.ContainsKey(methodname))
            {
                msg = $"Method {methodname} was not timed.";
            }

            watches[methodname].Stop();
            msg = $"Method {methodname} was timed, and lasted {watches[methodname].ElapsedMilliseconds} ms.";

            Debug.WriteLine(msg);
        }
    }
}
