// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace RunTests
{
    /// <summary>
    /// Used to write out to the Console. In addition to writing to the console this will output the same messages
    /// to our log file. This ensures the log file can be used as our single point of diagnostics.
    /// </summary>
    internal static class ConsoleUtil
    {
        internal static void Write(string message)
        {
            Console.Write(message);
            Logger.Log(message + Environment.NewLine);
        }

        internal static void WriteLine()
        {
            Console.WriteLine();
            Logger.Log("");
        }

        internal static void Warning(string message)
        {
            Console.Write("##vso[task.logissue type=warning]");
            Console.WriteLine(message);
            Logger.LogWarning(message);
        }

        internal static void WriteLine(string message)
        {
            Console.WriteLine(message);
            Logger.Log(message);
        }

        internal static void Write(ConsoleColor color, string message)
        {
            WithColor(color, () => Write(message));
        }

        internal static void WriteLine(ConsoleColor color, string message)
        {
            WithColor(color, () => WriteLine(message));
        }

        private static void WithColor(ConsoleColor color, Action action)
        {
            var saved = Console.ForegroundColor;
            try
            {
                Console.Out.Flush();
                Console.ForegroundColor = color;
                action();
                Console.Out.Flush();
            }
            finally
            {
                Console.ForegroundColor = saved;
            }
        }
    }
}
