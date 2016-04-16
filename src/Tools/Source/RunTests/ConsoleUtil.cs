// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal static class ConsoleUtil
    {
        internal static void Write(ConsoleColor color, string format, params object[] args)
        {
            WithColor(color, () => Console.Write(format, args));
        }

        internal static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            WithColor(color, () => Console.WriteLine(format, args));
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
