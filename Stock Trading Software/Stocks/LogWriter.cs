using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public class LogWriter
    {
        private List<CumulativeFileWriter> LogWriters;
        private string Path;
        private string Name;

        public LogWriter()
        {
            LogWriters = new List<CumulativeFileWriter>();
        }

        public LogWriter(string path, string name)
        {
            Path = path;
            Name = name;
            LogWriters = new List<CumulativeFileWriter>();
        }

        public void WriteLine(string message, params object[] arg)
        {
            if (!LogWriters.Any(writers => (writers.Name == Name && writers.Path == Path)))
            {
                LogWriters.Add(new CumulativeFileWriter(Path, Name));
            }
            LogWriters.Find(writers => (writers.Name == Name && writers.Path == Path)).WriteLine(message, arg);
        }

        public void WriteLine(string path, string name, string message, params object[] arg)
        {
            if (!LogWriters.Any(writers => (writers.Name == name && writers.Path == path)))
            {
                LogWriters.Add(new CumulativeFileWriter(path, name));
            }
            LogWriters.Find(writers => (writers.Name == name && writers.Path == path)).WriteLine(message, arg);
        }

        private class CumulativeFileWriter
        {
            public string Path { get; set; }
            public string Name { get; set; }
            private DateTime Datetime;
            private object WriterSync = new object();

            public CumulativeFileWriter(string path, string name)
            {
                Path = path;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                Name = name;
                Datetime = DateTime.MinValue;
            }

            public void WriteLine(string message, params object[] arg)
            {
                lock (WriterSync)
                {
                    using (var stream = new StreamWriter(Path + @"\" + @ServerTime.GetRealTime().ToString("yyyy-MM-dd") + "_" + Name + ".log", true, Encoding.Default))
                    {
                        try
                        {
                            stream.WriteLine(message, arg);
                            stream.WriteLine();
                            stream.AutoFlush = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }
    }
}
