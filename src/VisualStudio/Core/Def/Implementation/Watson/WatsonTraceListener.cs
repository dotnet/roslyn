// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal class WatsonTraceListener : TraceListener
    {
        public override void Fail(string message, string detailMessage)
        {
            if (string.IsNullOrEmpty(message))
            {
                Exit("Assertion failed");
            }
            else if (string.IsNullOrEmpty(detailMessage))
            {
                Exit(message);
            }
            else
            {
                Exit(message + " " + detailMessage);
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
            Trace.Listeners.Add(new WatsonTraceListener());
        }
    }
}
