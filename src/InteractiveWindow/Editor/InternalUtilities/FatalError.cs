// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.VisualStudio.InteractiveWindow
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
        /// Use in an exception filter to report a non fatal error.
        /// Calls <see cref="NonFatalHandler"/> and doesn't pass the exception through (the method returns true).
        /// </summary>
        /// <returns>True to catch the exception.</returns>
        [DebuggerHidden]
        public static bool ReportWithoutCrash(Exception exception)
        {
            Report(exception, s_nonFatalHandler);
            return true;
        }

        private static void Report(Exception exception, Action<Exception> handler)
        {
            // hold onto last exception to make investigation easier
            s_reportedException = exception;
            s_reportedExceptionMessage = exception.ToString();

            handler?.Invoke(exception);
        }
    }
}
