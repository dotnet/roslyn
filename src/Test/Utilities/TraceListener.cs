// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
#if DEBUG
    // To enable, add to <listeners> in *.exe.config, specifying the assembly-qualified
    // type name with optional initializeData="..." for .ctor args. For instance:
    // <configuration>
    //   <system.diagnostics>
    //     <trace>
    //       <listeners>
    //         <add name=""
    //           type="Microsoft.CodeAnalysis.TraceListener, Microsoft.CodeAnalysis, Version=..."
    //           initializeData="true"/>
    //         <remove name="Default"/>
    //       </listeners>
    //     </trace>
    //   </system.diagnostics>
    // </configuration>
    public sealed class TraceListener : System.Diagnostics.TraceListener
    {
        private readonly bool _continueOnFailure;

        public TraceListener()
        {
        }

        public TraceListener(bool continueOnFailure)
        {
            _continueOnFailure = continueOnFailure;
        }

        public override void Fail(string message, string detailMessage)
        {
            // Tools currently depend on the prefix appearing as an exception.
            WriteLine(new AssertFailureException(string.Format("{0}\r\n{1}", message, detailMessage)));
            WriteLine(new StackTrace(fNeedFileInfo: true));
            if (!_continueOnFailure)
            {
                Environment.Exit(-1);
            }
        }

        public override void Write(string message)
        {
            Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }

    public sealed class ThrowingTraceListener : System.Diagnostics.TraceListener
    {
        public override void Fail(string message, string detailMessage)
        {
            throw new AssertFailureException(string.Format("{0}\r\n{1}", message, detailMessage));
        }

        public override void Write(string message)
        {
            Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }

    public sealed class AssertFailureException : Exception
    {
        public AssertFailureException(string message) : base(message) { }
    }
#endif
}
