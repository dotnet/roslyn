// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    internal sealed class TestConsoleIO : ConsoleIO
    {
        private const ConsoleColor InitialColor = ConsoleColor.Gray;

        public TestConsoleIO(string input)
            : this(new Reader(input))
        {
        }

        private TestConsoleIO(Reader reader)
            : this(reader, new Writer(reader))
        {
        }

        private TestConsoleIO(Reader reader, TextWriter output)
            : base(output: output, error: new TeeWriter(output), input: reader)
        {
        }

        public override void SetForegroundColor(ConsoleColor consoleColor) => ((Writer)Out).CurrentColor = consoleColor;

        public override void ResetColor() => SetForegroundColor(InitialColor);

        private sealed class Reader : StringReader
        {
            public readonly StringBuilder ContentRead = new StringBuilder();

            public Reader(string input)
                : base(input)
            {
            }

            public override string ReadLine()
            {
                string result = base.ReadLine();
                ContentRead.AppendLine(result);
                return result;
            }
        }

        private sealed class Writer : StringWriter
        {
            private ConsoleColor _lastColor = InitialColor;
            public ConsoleColor CurrentColor = InitialColor;
            public override Encoding Encoding => Encoding.UTF8;
            private readonly Reader _reader;

            public Writer(Reader reader)
            {
                _reader = reader;
            }

            private void OnBeforeWrite()
            {
                if (_reader.ContentRead.Length > 0)
                {
                    GetStringBuilder().Append(_reader.ContentRead.ToString());
                    _reader.ContentRead.Clear();
                }

                if (_lastColor != CurrentColor)
                {
                    GetStringBuilder().AppendLine($"«{CurrentColor}»");
                    _lastColor = CurrentColor;
                }
            }

            public override void Write(char value)
            {
                OnBeforeWrite();
                base.Write(value);
            }

            public override void Write(string value)
            {
                OnBeforeWrite();
                GetStringBuilder().Append(value);
            }

            public override void WriteLine(string value)
            {
                OnBeforeWrite();
                GetStringBuilder().AppendLine(value);
            }

            public override void WriteLine()
            {
                OnBeforeWrite();
                GetStringBuilder().AppendLine();
            }
        }

        private sealed class TeeWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
            private readonly TextWriter _other;

            public TeeWriter(TextWriter other)
            {
                _other = other;
            }

            public override void Write(char value)
            {
                _other.Write(value);
                GetStringBuilder().Append(value);
            }

            public override void Write(string value)
            {
                _other.Write(value);
                GetStringBuilder().Append(value);
            }

            public override void WriteLine(string value)
            {
                _other.WriteLine(value);
                GetStringBuilder().AppendLine(value);
            }

            public override void WriteLine()
            {
                _other.WriteLine();
                GetStringBuilder().AppendLine();
            }
        }
    }
}
