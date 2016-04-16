// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            : base(output: new Writer(reader), error: new StringWriter(), input: reader)
        {
        }

        public override ConsoleColor ForegroundColor
        {
            set
            {
                ((Writer)Out).CurrentColor = value;
            }
        }

        public override void ResetColor()
        {
            ForegroundColor = InitialColor;
        }

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
                    base.Write(_reader.ContentRead.ToString());
                    _reader.ContentRead.Clear();
                }

                if (_lastColor != CurrentColor)
                {
                    base.WriteLine($"«{CurrentColor}»");
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
                base.Write(value);
            }

            public override void WriteLine(string value)
            {
                OnBeforeWrite();
                base.WriteLine(value);
            }

            public override void WriteLine()
            {
                OnBeforeWrite();
                base.WriteLine();
            }
        }
    }
}
