// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal static void LogWarning(string line)
        {
            lock (s_lines)
            {
                s_lines.Add($"Warning: {line}");
            }
        }

        internal static void LogError(Exception ex, string line)
        {
            lock (s_lines)
            {
                s_hasErrors = true;
                s_lines.Add($"Error {ex.Message}: {line}");
                s_lines.Add(ex.StackTrace ?? "");
            }
        }

        internal static void Log(string message, Exception ex)
        {
            lock (s_lines)
            {
                s_lines.Add(message);
                s_lines.Add(ex.Message);
                s_lines.Add(ex.StackTrace ?? "");
            }
        }

        internal static void Log(Exception ex)
        {
            lock (s_lines)
            {
                s_lines.Add(ex.Message);
                s_lines.Add(ex.StackTrace ?? "");
            }
        }

        internal static void Log(string line)
        {
            lock (s_lines)
            {
                s_lines.Add(line);
            }
        }

        internal static void Clear()
        {
            lock (s_lines)
            {
                s_lines.Clear();
            }
        }

        internal static void WriteTo(TextWriter textWriter)
        {
            lock (s_lines)
            {
                foreach (var line in s_lines)
                {
                    textWriter.WriteLine(line);
                }
            }
        }
    }
}
