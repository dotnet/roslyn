// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal class ConsoleIO
    {
        public static readonly ConsoleIO Default = new ConsoleIO(ConsoleShims.Out, ConsoleShims.Error, ConsoleShims.In);

        public TextWriter Error { get; }
        public TextWriter Out { get; }
        public TextReader In { get; }

        public ConsoleIO(TextWriter output, TextWriter error, TextReader input)
        {
            Debug.Assert(output != null);
            Debug.Assert(input != null);

            Out = output;
            Error = error;
            In = input;
        }

        public virtual ConsoleColor ForegroundColor
        {
            set
            {
                ConsoleShims.ForegroundColor = value;
            }
        }

        public virtual void ResetColor() => ConsoleShims.ResetColor();
    }
}
