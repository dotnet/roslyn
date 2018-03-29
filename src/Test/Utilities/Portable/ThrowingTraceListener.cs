// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#if !NETSTANDARD1_3
using System.Runtime.Serialization;
#endif

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
            throw new DebugAssertFailureException(message + Environment.NewLine + detailMessage);
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

#if !NETSTANDARD1_3
        [Serializable]
#endif
        public class DebugAssertFailureException : Exception
        {
            public DebugAssertFailureException() { }
            public DebugAssertFailureException(string message) : base(message) { }
            public DebugAssertFailureException(string message, Exception innerException) : base(message, innerException) { }
#if !NETSTANDARD1_3
            protected DebugAssertFailureException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
        }
    }
}
