// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace RunTests
{
    internal static class Logger
    {
        private static readonly List<string> s_lines = new List<string>();
        private static bool s_hasErrors;
        private static StreamWriter? s_writer;

        internal static void Initialize(string path)
        {
            lock (s_lines)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                s_writer?.Dispose();
                s_writer = new StreamWriter(path, append: false) { AutoFlush = true };
                foreach (var line in s_lines)
                    s_writer.WriteLine(line);
            }
        }

        internal static void Close()
        {
            lock (s_lines)
            {
                s_writer?.Dispose();
                s_writer = null;
            }
        }

        private static void Add(string line)
        {
            s_lines.Add(line);
            try
            {
                s_writer?.WriteLine(line);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                s_hasErrors = true;
                s_writer = null;
                Console.Error.WriteLine($"RunTests log persistence failed: {ex}");
            }
        }

        internal static bool HasErrors => s_hasErrors;

        internal static void LogWarning(string line)
        {
            lock (s_lines)
            {
                Add($"Warning: {line}");
            }
        }

        internal static void LogError(Exception ex, string line)
        {
            lock (s_lines)
            {
                s_hasErrors = true;
                Add($"Error {ex.Message}: {line}");
                Add(ex.StackTrace ?? "");
            }
        }

        internal static void Log(string message, Exception ex)
        {
            lock (s_lines)
            {
                Add(message);
                Add(ex.Message);
                Add(ex.StackTrace ?? "");
            }
        }

        internal static void Log(Exception ex)
        {
            lock (s_lines)
            {
                Add(ex.Message);
                Add(ex.StackTrace ?? "");
            }
        }

        internal static void Log(string line)
        {
            lock (s_lines)
            {
                Add(line);
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
