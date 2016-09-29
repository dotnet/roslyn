// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal static class Logger
    {
        private static readonly List<string> s_lines = new List<string>();
        private static bool s_hasErrors;

        internal static bool HasErrors => s_hasErrors;

        internal static void LogError(Exception ex, string line)
        {
            lock (s_lines)
            {
                s_hasErrors = true;
                s_lines.Add($"Error {ex.Message}: {line}");
                s_lines.Add(ex.StackTrace);
            }
        }

        internal static void Log(string line)
        {
            lock (s_lines)
            {
                s_lines.Add(line);
            }
        }

        internal static void Finish(string logDir)
        {
            var logFilePath = Path.Combine(logDir, "runtests.log");
            lock (s_lines)
            {
                File.WriteAllLines(logFilePath, s_lines.ToArray());
                s_lines.Clear();
            }
        }
    }
}
