// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Roslyn.Test.Utilities.CoreClr
{
    internal static class SharedConsole
    {
        private static readonly object s_guard = new();

        public static (string Output, string ErrorOutput) CaptureOutput(Action action)
        {
            lock (s_guard)
            {
                return CaptureOutputCore(action);
            }
        }

        public static (string Output, string ErrorOutput) CaptureOutputCore(Action action)
        {
            var savedConsoleOut = Console.Out;
            var savedConsoleError = Console.Error;

            using var outputWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            try
            {
                Console.SetOut(outputWriter);
                Console.SetError(errorWriter);
                action();
            }
            finally
            {
                Console.SetOut(savedConsoleOut);
                Console.SetError(savedConsoleError);
            }

            var output = outputWriter.ToString();
            var errorOutput = errorWriter.ToString();
            return (output, errorOutput);
        }
    }
}

#endif
