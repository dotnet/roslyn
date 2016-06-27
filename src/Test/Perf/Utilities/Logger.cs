// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
        /// <param name="file"></param>
        public ConsoleAndFileLogger(string file = "log.txt")
        {
            _file = file;
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
