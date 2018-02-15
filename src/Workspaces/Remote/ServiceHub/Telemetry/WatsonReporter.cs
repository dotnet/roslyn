﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal class WatsonReporter
    {
        private static TelemetrySession s_sessionOpt;

        /// <summary>
        /// The default callback to pass to <see cref="TelemetrySessionExtensions.PostFault(TelemetrySession, string, string, Exception, Func{IFaultUtility, int})"/>.
        /// Returning "0" signals that we should send data to Watson; any other value will cancel the Watson report.
        /// </summary>
        private static Func<IFaultUtility, int> s_defaultCallback = _ => 0;

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
        /// Check whether current user is microsoft internal or not
        /// </summary>
        public static bool IsUserMicrosoftInternal => SessionOpt?.IsUserMicrosoftInternal ?? false;

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
            Report(description, exception, s_defaultCallback);
        }

        /// <summary>
        /// Report Non-Fatal Watson
        /// </summary>
        /// <param name="description">any description you want to save with this watson report</param>
        /// <param name="exception">Exception that triggered this non-fatal error</param>
        /// <param name="callback">Callback to include extra data with the NFW. Note that we always collect
        /// a dump of the current process, but this can be used to add further information or files to the
        /// CAB.</param>
        public static void Report(string description, Exception exception, Func<IFaultUtility, int> callback)
        {
            var emptyCallstack = exception.SetCallstackIfEmpty();

            // if given exception is non recoverable exception,
            // crash instead of NFW
            if (IsNonRecoverableException(exception))
            {
                CodeAnalysis.FailFast.OnFatalException(exception);
            }

            if (!exception.ShouldReport())
            {
                return;
            }

            if (SessionOpt == null)
            {
                return;
            }

            var faultEvent = new FaultEvent(
                eventName: FunctionId.NonFatalWatson.GetEventName(),
                description: description,
                exceptionObject: exception,
                gatherEventDetails: arg =>
                {
                    // always add current processes dump
                    arg.AddProcessDump(System.Diagnostics.Process.GetCurrentProcess().Id);

                    return callback(arg);
                });

            // add extra bucket parameters to bucket better in NFW
            // we do it here so that it gets bucketted better in both
            // watson and telemetry. 
            faultEvent.SetExtraParameters(exception, emptyCallstack);

            SessionOpt.PostEvent(faultEvent);
        }

        private static bool IsNonRecoverableException(Exception exception)
        {
            return exception is OutOfMemoryException;
        }
    }
}
