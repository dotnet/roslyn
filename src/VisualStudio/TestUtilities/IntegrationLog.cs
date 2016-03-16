// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public sealed class IntegrationLog : IDisposable
    {
        private static IntegrationLog _current;

        private string _outputFolder;
        private string _outputFile;
        private StreamWriter _outputStream;

        private IntegrationLog()
        {
            _outputFolder = Path.Combine(Environment.CurrentDirectory, "VsIntegrationTestLogs");
            IntegrationHelper.CreateDirectory(_outputFolder, deleteExisting: false);

            _outputFile = Path.Combine(_outputFolder, $"{nameof(IntegrationHost)}.{Process.GetCurrentProcess().Id}.log");
            _outputStream = new StreamWriter(_outputFile, append: false);
        }

        public static IntegrationLog Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new IntegrationLog();
                }

                return _current;
            }
        }

        public void Dispose()
        {
            _outputStream?.Dispose();
        }

        public void EnableTraceListenerLogging()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new IntegrationTraceListener());
        }

        public void Error(string message, params object[] arguments)
            => WriteLine($"Error: {message}", arguments);

        public void Warning(string message, params object[] arguments)
            => WriteLine($"Warning: {message}", arguments);

        public void Write(string message, params object[] arguments)
            => _outputStream.Write($"[{DateTime.UtcNow}] {message}", arguments);

        public void WriteLine(string message, params object[] arguments)
            => Write($"{message}{Environment.NewLine}", arguments);
    }
}
