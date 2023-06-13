// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    /// <summary>
    /// Provides access to an appropriate <see cref="ITelemetryLogProvider"/> for logging telemetry.
    /// </summary>
    internal sealed class TelemetryLogProvider : ITelemetryLogProvider
    {
        private readonly AggregatingTelemetryLogManager _aggregatingTelemetryLogManager;
        private readonly VisualStudioTelemetryLogManager _visualStudioTelemetryLogManager;

        private TelemetryLogProvider(TelemetrySession session, ILogger telemetryLogger, IAsynchronousOperationListener asyncListener)
        {
            _aggregatingTelemetryLogManager = new AggregatingTelemetryLogManager(session, asyncListener);
            _visualStudioTelemetryLogManager = new VisualStudioTelemetryLogManager(session, telemetryLogger);
        }

        public static TelemetryLogProvider Create(TelemetrySession session, ILogger telemetryLogger, IAsynchronousOperationListener asyncListener)
        {
            var logProvider = new TelemetryLogProvider(session, telemetryLogger, asyncListener);

            TelemetryLogging.SetLogProvider(logProvider);

            return logProvider;
        }

        /// <summary>
        /// Returns an <see cref="ITelemetryLog"/> for logging telemetry.
        /// </summary>
        public ITelemetryLog? GetLog(FunctionId functionId)
        {
            return _visualStudioTelemetryLogManager.GetLog(functionId);
        }

        /// <summary>
        /// Returns an aggregating <see cref="ITelemetryLog"/> for logging telemetry.
        /// </summary>
        public ITelemetryLog? GetAggregatingLog(FunctionId functionId, double[]? bucketBoundaries)
        {
            return _aggregatingTelemetryLogManager.GetLog(functionId, bucketBoundaries);
        }
    }
}
