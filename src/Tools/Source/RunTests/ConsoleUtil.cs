// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
