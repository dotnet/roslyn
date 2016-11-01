// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal class WatsonReporter
    {
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
            TelemetryService.DefaultSession.PostFault(
                eventName: FunctionId.NonFatalWatson.GetEventName(),
                description: description,
                exceptionObject: exception);
        }
    }
}
