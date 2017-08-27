// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.Remote.Telemetry
{
    internal class WatsonReporter
    {
        private static TelemetrySession s_sessionOpt;

        /// <summary>
        /// Set default telemetry session
        /// </summary>
        public static void SetTelemetrySession(TelemetrySession session)
        {
            s_sessionOpt = session;
        }

        /// <summary>
        /// Default telemetry session
        /// </summary>
        public static TelemetrySession SessionOpt => s_sessionOpt;

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        public static void Report(Exception exception)
        {
            Report("Roslyn NonFatal Watson", exception);
        }

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="description">any description you want to save with this watson report</param>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        public static void Report(string description, Exception exception)
        {
            // if given exception is non recoverable exception,
            // crash instead of NFW
            if (IsNonRecoverableException(exception))
            {
                CodeAnalysis.FailFast.OnFatalException(exception);
            }

            SessionOpt?.PostFault(
                eventName: FunctionId.NonFatalWatson.GetEventName(),
                description: description,
                exceptionObject: exception,
                gatherEventDetails: arg =>
                {
                    arg.AddProcessDump(System.Diagnostics.Process.GetCurrentProcess().Id);

                    // 0 means send watson, otherwise, cancel watson
                    // we always send watson since dump itself can have valuable data
                    return 0;
                });
        }

        private static bool IsNonRecoverableException(Exception exception)
        {
            return exception is OutOfMemoryException;
        }
    }
}
