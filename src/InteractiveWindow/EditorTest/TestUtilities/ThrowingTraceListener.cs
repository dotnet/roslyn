// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    // To enable this for a process, add the following to the app.config for the project:
    //
    // <configuration>
    //  <system.diagnostics>
    //    <trace>
    //      <listeners>
    //        <remove name="Default" />
    //        <add name="ThrowingTraceListener" type="Microsoft.CodeAnalysis.ThrowingTraceListener, Roslyn.Test.Utilities.Desktop" />
    //      </listeners>
    //    </trace>
    //  </system.diagnostics>
    //</configuration>
    public sealed class ThrowingTraceListener : TraceListener
    {
        public override void Fail(string message, string detailMessage)
        {
            throw new DebugAssertFailureException(message + Environment.NewLine + detailMessage);
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

        [Serializable]
        public class DebugAssertFailureException : Exception
        {
            public DebugAssertFailureException() { }
            public DebugAssertFailureException(string message) : base(message) { }
            public DebugAssertFailureException(string message, Exception inner) : base(message, inner) { }
            protected DebugAssertFailureException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context)
            { }
        }
    }
}
