// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.Scripting
{
    public sealed class OutputRedirect : IDisposable
    {
        private static readonly object s_guard = new object();

        private readonly TextWriter _oldOut;
        private readonly StringWriter _newOut;

        public OutputRedirect(IFormatProvider formatProvider)
        {
            Monitor.Enter(s_guard);

            _oldOut = Console.Out;
            _newOut = new StringWriter(formatProvider);
            Console.SetOut(_newOut);
        }

        public string Output => _newOut.ToString();

        void IDisposable.Dispose()
        {
            Console.SetOut(_oldOut);
            _newOut.Dispose();

            Monitor.Exit(s_guard);
        }
    }
}
