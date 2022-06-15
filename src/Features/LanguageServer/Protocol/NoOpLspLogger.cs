// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class NoOpLspLogger : IRoslynLspLogger
    {
        public static readonly IRoslynLspLogger Instance = new NoOpLspLogger();

        private NoOpLspLogger() { }

        public void TraceException(Exception exception) { }
        public void TraceInformation(string message) { }
        public void TraceWarning(string message) { }
        public void TraceError(string message) { }
        public void TraceStart(string message) { }
        public void TraceStop(string message) { }
    }
}
