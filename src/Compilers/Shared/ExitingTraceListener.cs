// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.CommandLine
{
    /// <summary>
    /// This trace listener is useful in environments where we don't want a dialog but instead want
    /// to exit with a reliable stack trace of the failure.  For example during a bootstrap build where
    /// the assert dialog would otherwise cause a Jenkins build to timeout. 
    /// </summary>
    internal sealed class ExitingTraceListener : TraceListener
    {
        internal ICompilerServerLogger Logger { get; }

        internal ExitingTraceListener(ICompilerServerLogger logger)
        {
            Logger = logger;
        }

        public override void Write(string message)
        {
            Exit(message);
        }

        public override void WriteLine(string message)
        {
            Exit(message);
        }

        internal static void Install(ICompilerServerLogger logger)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ExitingTraceListener(logger));
        }

        private void Exit(string originalMessage)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Debug.Assert failed with message: {originalMessage}");
            builder.AppendLine("Stack Trace");
            var stackTrace = new StackTrace();
            builder.AppendLine(stackTrace.ToString());

            var message = builder.ToString();
            Logger.Log(message);

            // Use FailFast so that the process fails rudely and goes through 
            // windows error reporting (on Windows at least). This will allow our 
            // CI environment to capture crash dumps for future investigation
            Environment.FailFast(message);
        }
    }
}
