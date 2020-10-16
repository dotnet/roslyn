﻿// Licensed to the .NET Foundation under one or more agreements.
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
        public override void Write(string message)
        {
            Exit(message);
        }

        public override void WriteLine(string message)
        {
            Exit(message);
        }

        internal static void Install()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ExitingTraceListener());
        }

        private static void Exit(string originalMessage)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Debug.Assert failed with message: {originalMessage}");
            builder.AppendLine("Stack Trace");
            var stackTrace = new StackTrace();
            builder.AppendLine(stackTrace.ToString());

            var message = builder.ToString();
            var logFullName = GetLogFileFullName();
            File.WriteAllText(logFullName, message);

            Console.WriteLine(message);
            Console.WriteLine($"Log at: {logFullName}");

            Environment.Exit(1);
        }

        private static string GetLogFileFullName()
        {
            var assembly = typeof(ExitingTraceListener).Assembly;
            var name = $"{Path.GetFileName(assembly.Location)}.tracelog";
            var path = Path.GetDirectoryName(assembly.Location);
            return Path.Combine(path, name);
        }
    }
}
