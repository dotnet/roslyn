// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class IntegrationTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            IntegrationLog.Current.Write(message);
        }

        public override void WriteLine(string message)
        {
            IntegrationLog.Current.WriteLine(message);
        }
    }
}
