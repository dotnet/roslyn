// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ConsoleOutput
    {
        private static readonly object s_consoleGuard = new object();

        private sealed class CappedStringWriter : StringWriter
        {
            private readonly int _expectedLength;
            private int _remaining;

            public CappedStringWriter(int expectedLength)
                : base(System.Globalization.CultureInfo.InvariantCulture)
            {
                if (expectedLength < 0)
                {
                    _expectedLength = _remaining = 1024 * 1024;
                }
                else
                {
                    _expectedLength = expectedLength;
                    _remaining = Math.Max(256, expectedLength * 4);
                }
            }

            private void CapReached()
            {
                Assert.True(false, "Test produced more output than expected (" + _expectedLength + " characters). Is it in an infinite loop? Output so far:\r\n" + GetStringBuilder());
            }

            public override void Write(char value)
            {
                if (1 <= _remaining)
                {
                    _remaining--;
                    base.Write(value);
                }
                else
                {
                    CapReached();
                }
            }

            public override void Write(char[] buffer, int index, int count)
            {
                if (count <= _remaining)
                {
                    _remaining -= count;
                    base.Write(buffer, index, count);
                }
                else
                {
                    CapReached();
                }
            }

            public override void Write(string value)
            {
                if (value.Length <= _remaining)
                {
                    _remaining -= value.Length;
                    base.Write(value);
                }
                else
                {
                    CapReached();
                }
            }
        }

        public static void Capture(Action action, int expectedLength, out string output, out string errorOutput)
        {
            TextWriter errorOutputWriter = new CappedStringWriter(expectedLength);
            TextWriter outputWriter = new CappedStringWriter(expectedLength);

            lock (s_consoleGuard)
            {
                TextWriter originalOut = Console.Out;
                TextWriter originalError = Console.Error;
                try
                {
                    Console.SetOut(outputWriter);
                    Console.SetError(errorOutputWriter);
                    action();
                }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalError);
                }
            }

            output = outputWriter.ToString();
            errorOutput = errorOutputWriter.ToString();
        }

        public static void AssertEqual(Action action, string expectedOutput, string expectedErrorOutput, Func<string, string, bool> equalityComparer = null)
        {
            string output, error;
            Capture(action, (equalityComparer == null) ? expectedOutput.Length : -1, out output, out error);

            expectedOutput = expectedOutput.Trim();
            string actualOutput = output.Trim();

            expectedErrorOutput = expectedErrorOutput.Trim();
            string actualErrorOutput = error.Trim();

            if (equalityComparer == null)
            {
                Assert.Equal(expectedOutput, actualOutput);
                Assert.Equal(expectedErrorOutput, actualErrorOutput);
            }
            else
            {
                Assert.True(equalityComparer(expectedOutput, actualOutput), "Unexpected output");
                Assert.True(equalityComparer(expectedErrorOutput, actualErrorOutput), "Unexpected error output");
            }
        }
    }
}
