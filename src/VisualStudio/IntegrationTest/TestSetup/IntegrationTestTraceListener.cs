// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    internal class IntegrationTestTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            Exit(message);
        }

        public override void WriteLine(string message)
        {
            Exit(message);
        }

        private static void Exit(string message)
        {
            FatalError.Report(new Exception(message));
        }

        internal static void Install()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new IntegrationTestTraceListener());
        }
    }
}
