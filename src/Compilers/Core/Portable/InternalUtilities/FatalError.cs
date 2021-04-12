// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

#if NET20
// Some APIs referenced by documentation comments are not available on .NET Framework 2.0.
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif

#if COMPILERCORE
namespace Microsoft.CodeAnalysis
#else
namespace Microsoft.CodeAnalysis.ErrorReporting
#endif
{
    internal static class FatalError
    {
        private static Action<Exception>? s_fatalHandler;
        private static Action<Exception>? s_nonFatalHandler;

#pragma warning disable IDE0052 // Remove unread private members - We want to hold onto last exception to make investigation easier
        private static Exception? s_reportedException;
        private static string? s_reportedExceptionMessage;
#pragma warning restore IDE0052

        /// <summary>
        /// Set by the host to a fail fast trigger, 
        /// if the host desires to crash the process on a fatal exception.
        /// </summary>
        [DisallowNull]
        public static Action<Exception>? Handler
        {
            get
            {
                return s_fatalHandler;
            }

            set
            {
                if (s_fatalHandler != value)
                {
                    Debug.Assert(s_fatalHandler == null, "Handler already set");
                    s_fatalHandler = value;
                }
            }
        }

        /// <summary>
        /// Set by the host to a fail fast trigger, 
        /// if the host desires to NOT crash the process on a non fatal exception.
        /// </summary>
        [DisallowNull]
        public static Action<Exception>? NonFatalHandler
        {
            get
            {
                return s_nonFatalHandler;
            }

            set
            {
                if (s_nonFatalHandler != value)
                {
                    Debug.Assert(s_nonFatalHandler == null, "Handler already set");
                    s_nonFatalHandler = value;
                }
            }
        }

        // Same as setting the Handler property except that it avoids the assert.  This is useful in 
        // test code which needs to verify the handler is called in specific cases and will continually
        // overwrite this value.
        public static void OverwriteHandler(Action<Exception>? value)
        {
            s_fatalHandler = value;
        }

        private static bool IsCurrentOperationBeingCancelled(Exception exception, CancellationToken cancellationToken)
            => exception is OperationCanceledException && cancellationToken.IsCancellationRequested;

        /// <summary>
        /// Use in an exception filter to report a fatal error (by calling <see cref="Handler"/>), unless the
        /// operation has been cancelled. The exception is never caught.
        /// </summary>
        /// <returns><see langword="false"/> to avoid catching the exception.</returns>
        [DebuggerHidden]
        public static bool ReportAndPropagateUnlessCanceled(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return false;
            }

            return ReportAndPropagate(exception);
        }

        /// <summary>
        /// <para>Use in an exception filter to report a fatal error (by calling <see cref="Handler"/>), unless the
        /// operation has been cancelled at the request of <paramref name="contextCancellationToken"/>. The exception is
        /// never caught.</para>
        ///
        /// <para>Cancellable operations are only expected to throw <see cref="OperationCanceledException"/> if the
        /// applicable <paramref name="contextCancellationToken"/> indicates cancellation is requested by setting
        /// <see cref="CancellationToken.IsCancellationRequested"/>. Unexpected cancellation, i.e. an
        /// <see cref="OperationCanceledException"/> which occurs without <paramref name="contextCancellationToken"/>
        /// requesting cancellation, is treated as an error by this method.</para>
        ///
        /// <para>This method does not require <see cref="OperationCanceledException.CancellationToken"/> to match
        /// <paramref name="contextCancellationToken"/>, provided cancellation is expected per the previous
        /// paragraph.</para>
        /// </summary>
        /// <param name="contextCancellationToken">A <see cref="CancellationToken"/> which will have
        /// <see cref="CancellationToken.IsCancellationRequested"/> set if cancellation is expected.</param>
        /// <returns><see langword="false"/> to avoid catching the exception.</returns>
        [DebuggerHidden]
        public static bool ReportAndPropagateUnlessCanceled(Exception exception, CancellationToken contextCancellationToken)
        {
            if (IsCurrentOperationBeingCancelled(exception, contextCancellationToken))
            {
                return false;
            }

            return ReportAndPropagate(exception);
        }

        /// <summary>
        /// Use in an exception filter to report a non-fatal error (by calling <see cref="NonFatalHandler"/>) and catch
        /// the exception, unless the operation was cancelled.
        /// </summary>
        /// <returns><see langword="true"/> to catch the exception if the non-fatal error was reported; otherwise,
        /// <see langword="false"/> to propagate the exception if the operation was cancelled.</returns>
        [DebuggerHidden]
        public static bool ReportAndCatchUnlessCanceled(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return false;
            }

            return ReportAndCatch(exception);
        }

        /// <summary>
        /// <para>Use in an exception filter to report a non-fatal error (by calling <see cref="NonFatalHandler"/>) and
        /// catch the exception, unless the operation was cancelled at the request of
        /// <paramref name="contextCancellationToken"/>.</para>
        ///
        /// <para>Cancellable operations are only expected to throw <see cref="OperationCanceledException"/> if the
        /// applicable <paramref name="contextCancellationToken"/> indicates cancellation is requested by setting
        /// <see cref="CancellationToken.IsCancellationRequested"/>. Unexpected cancellation, i.e. an
        /// <see cref="OperationCanceledException"/> which occurs without <paramref name="contextCancellationToken"/>
        /// requesting cancellation, is treated as an error by this method.</para>
        ///
        /// <para>This method does not require <see cref="OperationCanceledException.CancellationToken"/> to match
        /// <paramref name="contextCancellationToken"/>, provided cancellation is expected per the previous
        /// paragraph.</para>
        /// </summary>
        /// <param name="contextCancellationToken">A <see cref="CancellationToken"/> which will have
        /// <see cref="CancellationToken.IsCancellationRequested"/> set if cancellation is expected.</param>
        /// <returns><see langword="true"/> to catch the exception if the non-fatal error was reported; otherwise,
        /// <see langword="false"/> to propagate the exception if the operation was cancelled.</returns>
        [DebuggerHidden]
        public static bool ReportAndCatchUnlessCanceled(Exception exception, CancellationToken contextCancellationToken)
        {
            if (IsCurrentOperationBeingCancelled(exception, contextCancellationToken))
            {
                return false;
            }

            return ReportAndCatch(exception);
        }

        /// <summary>
        /// Use in an exception filter to report a fatal error without catching the exception.
        /// The error is reported by calling <see cref="Handler"/>.
        /// </summary>
        /// <returns><see langword="false"/> to avoid catching the exception.</returns>
        [DebuggerHidden]
        public static bool ReportAndPropagate(Exception exception)
        {
            Report(exception, s_fatalHandler);
            return false;
        }

        /// <summary>
        /// Report a non-fatal error.
        /// Calls <see cref="NonFatalHandler"/> and doesn't pass the exception through (the method returns true).
        /// This is generally expected to be used within an exception filter as that allows us to
        /// capture data at the point the exception is thrown rather than when it is handled.
        /// However, it can also be used outside of an exception filter. If the exception has not
        /// already been thrown the method will throw and catch it itself to ensure we get a useful
        /// stack trace.
        /// </summary>
        /// <returns>True to catch the exception.</returns>
        [DebuggerHidden]
        public static bool ReportAndCatch(Exception exception)
        {
            Report(exception, s_nonFatalHandler);
            return true;
        }

        private static readonly object s_reportedMarker = new();

        private static void Report(Exception exception, Action<Exception>? handler)
        {
            // hold onto last exception to make investigation easier
            s_reportedException = exception;
            s_reportedExceptionMessage = exception.ToString();

            if (handler == null)
            {
                return;
            }

            // only report exception once
            if (exception.Data[s_reportedMarker] != null)
            {
                return;
            }

#if !NET20
            if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count == 1 && aggregate.InnerExceptions[0].Data[s_reportedMarker] != null)
            {
                return;
            }
#endif
            if (!exception.Data.IsReadOnly)
            {
                exception.Data[s_reportedMarker] = s_reportedMarker;
            }

            handler.Invoke(exception);
        }
    }
}
