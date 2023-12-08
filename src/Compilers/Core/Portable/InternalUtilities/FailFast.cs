// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class FailFast
    {
        /// <summary>
        /// A pre-created delegate to assign to <see cref="FatalError.ErrorReporterHandler" /> if needed.
        /// </summary>
        internal static readonly FatalError.ErrorReporterHandler Handler = static (e, _, _) => OnFatalException(e);

        [DebuggerHidden]
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static void OnFatalException(Exception exception)
        {
            // EDMAURER Now using the managed API to fail fast so as to default
            // to the managed VS debug engine and hopefully get great
            // Watson bucketing. Before vanishing trigger anyone listening.
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            // don't fail fast with an aggregate exception that is masking true exception
            if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count == 1)
            {
                exception = aggregate.InnerExceptions[0];
            }

            DumpStackTrace(exception: exception);

            Environment.FailFast(exception.ToString(), exception);
            throw ExceptionUtilities.Unreachable(); // to satisfy [DoesNotReturn]
        }

        [DebuggerHidden]
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static void Fail(string message)
        {
            DumpStackTrace(message: message);
            Environment.FailFast(message);
            throw ExceptionUtilities.Unreachable(); // to satisfy [DoesNotReturn]
        }

        /// <summary>
        /// Dumps the stack trace of the exception and the handler to the console. This is useful
        /// for debugging unit tests that hit a fatal exception
        /// </summary>
        [Conditional("DEBUG")]
        internal static void DumpStackTrace(Exception? exception = null, string? message = null)
        {
            Console.WriteLine("Dumping info before call to failfast");
            if (message is object)
            {
                Console.WriteLine(message);
            }

            if (exception is object)
            {
                Console.WriteLine("Exception info");
                for (Exception? current = exception; current is object; current = current.InnerException)
                {
                    Console.WriteLine(current.Message);
                    Console.WriteLine(current.StackTrace);
                }
            }

            Console.WriteLine("Stack trace of handler");
            var stackTrace = new StackTrace();
            Console.WriteLine(stackTrace.ToString());

            Console.Out.Flush();
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

            Fail("ASSERT FAILED" + Environment.NewLine + message);
        }
    }
}
