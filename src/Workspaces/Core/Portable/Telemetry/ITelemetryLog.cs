// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry
{
    internal interface ITelemetryLog
    {
        /// <summary>
        /// Adds a telemetry event with values obtained from context message <paramref name="logMessage"/>
        /// </summary>
        public void Log(LogMessage logMessage);

        /// <summary>
        /// Adds an execution time telemetry event representing the <paramref name="name"/> operation
        /// only if  block duration meets or exceeds <paramref name="minThresholdMs"/> milliseconds.
        /// </summary>
        /// <param name="minThresholdMs">Optional parameter used to determine whether to send the telemetry event (in milliseconds)</param>
        public IDisposable? LogBlockTime(string name, int minThresholdMs = -1);
    }
}
