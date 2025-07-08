// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry;

internal interface ITelemetryBlockLog : ITelemetryLog
{
    /// <summary>
    /// Adds an execution time telemetry event representing <paramref name="logMessage"/>
    /// only if  block duration meets or exceeds <paramref name="minThresholdMs"/> milliseconds.
    /// </summary>
    /// <param name="logMessage">Event data to be sent</param>
    /// <param name="minThresholdMs">Optional parameter used to determine whether to send the telemetry event (in milliseconds)</param>
    IDisposable? LogBlockTime(KeyValueLogMessage logMessage, int minThresholdMs = -1);
}
