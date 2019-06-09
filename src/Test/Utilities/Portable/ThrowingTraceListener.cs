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
    //        <add name="ThrowingTraceListener" type="Microsoft.CodeAnalysis.ThrowingTraceListener, Roslyn.Test.Utilities" />
    //      </listeners>
    //    </trace>
    //  </system.diagnostics>
    //</configuration>
    public sealed class ThrowingTraceListener : TraceListener
    {
        public override void Fail(string message, string detailMessage)
        {
            throw new InvalidOperationException(message + Environment.NewLine + detailMessage);
        }

        public override void Write(object o)
        {
        }

        public override void Write(object o, string category)
        {
        }

        public override void Write(string message)
        {
        }

        public override void Write(string message, string category)
        {
        }

        public override void WriteLine(object o)
        {
        }

        public override void WriteLine(object o, string category)
        {
        }

        public override void WriteLine(string message)
        {
        }

        public override void WriteLine(string message, string category)
        {
        }
    }
}
