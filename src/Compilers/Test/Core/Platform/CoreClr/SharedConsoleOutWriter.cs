// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET

using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Roslyn.Test.Utilities.CoreClr
{
    internal static class SharedConsole
    {
        private static TextWriter s_savedConsoleOut;
        private static TextWriter s_savedConsoleError;

        private static AsyncLocal<StringWriter> s_currentOut;
        private static AsyncLocal<StringWriter> s_currentError;

        internal static void OverrideConsole()
        {
            s_savedConsoleOut = Console.Out;
            s_savedConsoleError = Console.Error;

            s_currentOut = new AsyncLocal<StringWriter>();
            s_currentError = new AsyncLocal<StringWriter>();

            Console.SetOut(new SharedConsoleOutWriter());
            Console.SetError(new SharedConsoleErrorWriter());
        }

        public static void CaptureOutput(Action action, int expectedLength, out string output, out string errorOutput)
        {
            var outputWriter = new CappedStringWriter(expectedLength);
            var errorOutputWriter = new CappedStringWriter(expectedLength);

            var savedOutput = s_currentOut.Value;
            var savedError = s_currentError.Value;

            try
            {
                s_currentOut.Value = outputWriter;
                s_currentError.Value = errorOutputWriter;
                action();
            }
            finally
            {
                s_currentOut.Value = savedOutput;
                s_currentError.Value = savedError;
            }

            output = outputWriter.ToString();
            errorOutput = errorOutputWriter.ToString();
        }

        private sealed class SharedConsoleOutWriter : SharedConsoleWriter
        {
            public override TextWriter Underlying => s_currentOut.Value ?? s_savedConsoleOut;
        }

        private sealed class SharedConsoleErrorWriter : SharedConsoleWriter
        {
            public override TextWriter Underlying => s_currentError.Value ?? s_savedConsoleError;
        }

        private abstract class SharedConsoleWriter : TextWriter
        {
            public override Encoding Encoding => Underlying.Encoding;

            public abstract TextWriter Underlying { get; }

            public override void Write(char value) => Underlying.Write(value);
        }
    }
}

#endif
