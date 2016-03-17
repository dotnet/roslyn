// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Redirects traces and asserts to the debug output stream.</summary>
    /// <remarks>This works around the assert dialog issue caused by calling <see cref="Debug.Assert"/> in a <c>DEBUG</c> build.</remarks>
    public class IntegrationTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            Debug.Write(message);
        }

        public override void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
