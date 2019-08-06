// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#if COMPILERCORE
namespace Microsoft.CodeAnalysis
#else
namespace Microsoft.CodeAnalysis.ErrorReporting
#endif
{
    internal static class FatalError
    {
        private static Action<Exception> s_fatalHandler;
        private static Action<Exception> s_nonFatalHandler;

        private static Exception s_reportedException;
        private static string s_reportedExceptionMessage;

        /// <summary>
        /// Set by the host to a fail fast trigger, 
        /// if the host desires to crash the process on a fatal exception.
        /// </summary>
        public static Action<Exception> Handler
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
        public static Action<Exception> NonFatalHandler
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
        public static void OverwriteHandler(Action<Exception> value)
        {
            s_fatalHandler = value;
        }

        /// <summary>
        /// Use in an exception filter to report a fatal error. 
        /// Unless the exception is <see cref="OperationCanceledException"/> 
        /// it calls <see cref="Handler"/>. The exception is passed through (the method returns false).
        /// </summary>
        /// <returns>False to avoid catching the exception.</returns>
        [DebuggerHidden]
        public static bool ReportUnlessCanceled(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return false;
            }

            return Report(exception);
        }

        /// <summary>
        /// Use in an exception filter to report a non fatal error. 
        /// Unless the exception is <see cref="OperationCanceledException"/> 
        /// it calls <see cref="NonFatalHandler"/>. The exception isn't passed through (the method returns true).
        /// </summary>
        /// <returns>True to catch the exception.</returns>
        [DebuggerHidden]
        public static bool ReportWithoutCrashUnlessCanceled(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return false;
            }

            return ReportWithoutCrash(exception);
        }

        /// <summary>
        /// Use in an exception filter to report a fatal error. 
        /// Unless the exception is <see cref="NotImplementedException"/> 
        /// it calls <see cref="Handler"/>. The exception is passed through (the method returns false).
        /// </summary>
        /// <returns>False to avoid catching the exception.</returns>
        [DebuggerHidden]
        public static bool ReportUnlessNotImplemented(Exception exception)
        {
            if (exception is NotImplementedException)
            {
                return false;
            }

            return Report(exception);
        }

        /// <summary>
        /// Use in an exception filter to report a fatal error.
        /// Calls <see cref="Handler"/> and passes the exception through (the method returns false).
        /// </summary>
        /// <returns>False to avoid catching the exception.</returns>
        [DebuggerHidden]
        public static bool Report(Exception exception)
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
        public static bool ReportWithoutCrash(Exception exception)
        {
            Report(exception, s_nonFatalHandler);
            return true;
        }

        /// <summary>
        /// Report a non-fatal error like <see cref="ReportWithoutCrash"/> but propagates the exception.
        /// </summary>
        /// <returns>False to propagate the exception.</returns>
        [DebuggerHidden]
        public static bool ReportWithoutCrashAndPropagate(Exception exception)
        {
            Report(exception, s_nonFatalHandler);
            return false;
        }

        /// <summary>
        /// Report a non-fatal error like <see cref="ReportWithoutCrash"/> but propagates the exception.
        /// </summary>
        /// <returns>False to propagate the exception.</returns>
        [DebuggerHidden]
        public static bool ReportWithoutCrashUnlessCanceledAndPropagate(Exception exception)
        {
            if (!(exception is OperationCanceledException))
            {
                Report(exception, s_nonFatalHandler);
            }

            return false;
        }

        private static readonly object s_reportedMarker = new object();

        private static void Report(Exception exception, Action<Exception> handler)
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

#if !NETFX20
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
