// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    using Debugger = System.Diagnostics.Debugger;

    internal class IntegrationTestTraceListener : TraceListener
    {
        public override void Fail(string message, string detailMessage)
        {
            if (!string.IsNullOrEmpty(detailMessage))
            {
                Exit(message + " " + detailMessage);
            }
            else
            {
                Exit(message);
            }
        }

        public override void Write(object o)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, o?.ToString());
            }
        }

        public override void Write(object o, string category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, o?.ToString());
            }
        }

        public override void Write(string message)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, message);
            }
        }

        public override void Write(string message, string category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, message);
            }
        }

        public override void WriteLine(object o)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, o?.ToString() + Environment.NewLine);
            }
        }

        public override void WriteLine(object o, string category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, o?.ToString() + Environment.NewLine);
            }
        }

        public override void WriteLine(string message)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, message + Environment.NewLine);
            }
        }

        public override void WriteLine(string message, string category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, message + Environment.NewLine);
            }
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
