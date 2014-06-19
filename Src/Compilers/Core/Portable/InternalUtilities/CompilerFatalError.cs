// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static class CompilerFatalError
    {
        private static Action<Exception> handler;

        // Set by the host to a fail fast trigger, 
        // if the host desires to crash the process on a fatal exception.
        // May also just report a non-fatal Watson and continue.
        public static Action<Exception> Handler
        {
            get
            {
                return handler;
            }
            set
            {
                Debug.Assert(handler == null, "Handler already set");
                handler = value;
            }
        }

        /// <summary>
        /// Use in an exception filter to report a fatal error. 
        /// Unless the exception is <see cref="OperationCanceledException"/> 
        /// it calls <see cref="Handler"/>. The exception is passed thru (the method returns false).
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
        /// Use in an exception filter to report a fatal error.
        /// Calls <see cref="Handler"/> and passes the exception thru (the method returns false).
        /// </summary>
        /// <returns>False to avoid catching the exception.</returns>
        [DebuggerHidden]
        public static bool Report(Exception exception)
        {
            var localHandler = handler;
            if (localHandler != null)
            {
                localHandler(exception);
            }

            return false;
        }
    }
}
