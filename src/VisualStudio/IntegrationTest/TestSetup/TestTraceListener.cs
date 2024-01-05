// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal class TestTraceListener : TraceListener
    {
        private ImmutableList<Exception> _failures = ImmutableList<Exception>.Empty;

        public static TestTraceListener Instance { get; } = new();

        public override void Fail(string? message, string? detailMessage)
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

        public override void Write(object? o)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, o?.ToString());
            }
        }

        public override void Write(object? o, string? category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, o?.ToString());
            }
        }

        public override void Write(string? message)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, message);
            }
        }

        public override void Write(string? message, string? category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, message);
            }
        }

        public override void WriteLine(object? o)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, o?.ToString() + Environment.NewLine);
            }
        }

        public override void WriteLine(object? o, string? category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, o?.ToString() + Environment.NewLine);
            }
        }

        public override void WriteLine(string? message)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, null, message + Environment.NewLine);
            }
        }

        public override void WriteLine(string? message, string? category)
        {
            if (Debugger.IsLogging())
            {
                Debugger.Log(0, category, message + Environment.NewLine);
            }
        }

        private static void Exit(string? message)
        {
            var reportedException = new Exception(message);
            try
            {
                // Set stack trace on the exception for logging
                ExceptionDispatchInfo.Capture(reportedException).Throw();
            }
            catch (Exception ex)
            {
                reportedException = ex;
            }

            if (message?.Contains("Pretty-listing introduced errors in error-free code") ?? false)
            {
                // Ignore this known assertion failure
                FatalError.ReportAndCatch(reportedException, ErrorSeverity.Critical);
                return;
            }

            FatalError.ReportAndPropagate(reportedException, ErrorSeverity.Critical);
            Instance.AddException(reportedException);
        }

        public void AddException(Exception exception)
        {
            ImmutableInterlocked.Update(ref _failures, static (failures, exception) => failures.Add(exception), exception);
        }

        public void VerifyNoErrorsAndReset()
        {
            var failures = Interlocked.Exchange(ref _failures, ImmutableList<Exception>.Empty);
            if (!failures.IsEmpty)
            {
                throw new AggregateException(failures);
            }
        }

        internal static void Install()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(Instance);
        }
    }
}
