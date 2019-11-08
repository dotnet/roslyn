// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    internal static class FailFast
    {
        [DebuggerHidden]
        [DoesNotReturn]
        internal static void OnFatalException(Exception exception)
        {
            // EDMAURER Now using the managed API to fail fast so as to default
            // to the managed VS debug engine and hopefully get great
            // Watson bucketing. Before vanishing trigger anyone listening.
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

#if !NETFX20
            // don't fail fast with an aggregate exception that is masking true exception
            var aggregate = exception as AggregateException;
            if (aggregate is { InnerExceptions: { Count: 1 } })
            {
                exception = aggregate.InnerExceptions[0];
            }
#endif

            Environment.FailFast(exception.ToString(), exception);
        }

        /// <summary>
        /// Checks for the given <paramref name="condition"/>; if the <paramref name="condition"/> is <c>true</c>, 
        /// immediately terminates the process without running any pending <c>finally</c> blocks or finalizers
        /// and causes a crash dump to be collected (if the system is configured to do so). 
        /// Otherwise, the process continues normally.
        /// </summary>
        /// <param name="condition">The conditional expression to evaluate.</param>
        /// <param name="message">An optional message to be recorded in the dump in case of failure. Can be <c>null</c>.</param>
        [Conditional("DEBUG")]
        [DebuggerHidden]
        internal static void Assert([DoesNotReturnIf(false)] bool condition, string? message = null)
        {
            if (condition)
            {
                return;
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Environment.FailFast("ASSERT FAILED" + Environment.NewLine + message);
        }
    }
}
