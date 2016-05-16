using System;
using System.IO;
using System.Text;

namespace Roslyn.Test.Performance.Utilities
{
    public interface ILogger
    {
        void Log(string v);
        void Flush();
    }
    public class ConsoleAndFileLogger : ILogger
    {
        private readonly string _file;
        private readonly StringBuilder _buffer = new StringBuilder();

        public ConsoleAndFileLogger(string file = "log.txt")
        {
            _file = file;
        }

        public void Flush()
        {
            File.AppendAllText(_file, _buffer.ToString());
        }

        public void Log(string v)
        {
            Console.WriteLine(DateTime.Now + " : " + v);
            _buffer.AppendLine(DateTime.Now + " : " + v);
        }
    }
}
