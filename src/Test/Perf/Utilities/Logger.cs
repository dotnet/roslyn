// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Text;

namespace Roslyn.Test.Performance.Utilities
{
    /// <summary>
    /// An interface for logging messages.  A global ILogger implementation
    /// exists at Roslyn.Test.Performance.Utilities.RuntimeSettings.logger.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a string through the logger.
        /// </summary>
        /// <param name="v"></param>
        void Log(string v);

        /// <summary>
        /// Flushes the cache (if one exists).
        /// </summary>
        void Flush();
    }

    /// <summary>
    /// An implementation of ILogger that prints to the console, and also
    /// writes to a file.
    /// </summary>
    public class ConsoleAndFileLogger : ILogger
    {
        private readonly string _file;
        private readonly StringBuilder _buffer = new StringBuilder();

        /// <summary>
        /// Constructs a new ConsoleAndFileLogger with a default log 
        /// file of 'log.txt'.
        /// </summary>
        public ConsoleAndFileLogger()
        {
            if (Directory.Exists(TestUtilities.GetCPCDirectoryPath()))
            {
                _file = Path.Combine(TestUtilities.GetCPCDirectoryPath(), "perf-log.txt");
            }
            else
            {
                _file = "./perf-log.txt";
            }
        }

        void ILogger.Flush()
        {
            File.AppendAllText(_file, _buffer.ToString());
            _buffer.Clear();
        }

        void ILogger.Log(string v)
        {
            Console.WriteLine(DateTime.Now + " : " + v);
            _buffer.AppendLine(DateTime.Now + " : " + v);
        }
    }
}
