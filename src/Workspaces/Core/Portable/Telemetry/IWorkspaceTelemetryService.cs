// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Telemetry
{
    /// <summary>
    /// Provides access to the telemetry service to workspace services.
    /// Abstract away the actual implementation of the telemetry service (e.g. Microsoft.VisualStudio.Telemetry).
    /// </summary>
    internal interface IWorkspaceTelemetryService : IWorkspaceService
    {
        /// <summary>
        /// True if a telemetry session has started.
        /// </summary>
        bool HasActiveSession { get; }

        /// <summary>
        /// Serialized the current telemetry settings. Returns <see langword="null"/> if session hasn't started.
        /// </summary>
        string? SerializeCurrentSessionSettings();

        /// <summary>
        /// Adds a <see cref="TraceSource"/> used to log unexpected exceptions.
        /// </summary>
        void RegisterUnexpectedExceptionLogger(TraceSource logger);

        /// <summary>
        /// Removes a <see cref="TraceSource"/> used to log unexpected exceptions.
        /// </summary>
        void UnregisterUnexpectedExceptionLogger(TraceSource logger);
    }
}
