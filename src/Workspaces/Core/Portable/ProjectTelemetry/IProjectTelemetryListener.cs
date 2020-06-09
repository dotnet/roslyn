// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ProjectTelemetry
{
    /// <summary>
    /// Callback the host (VS) passes to the OOP service to allow it to send batch notifications
    /// about telemetry.
    /// </summary>
    internal interface IProjectTelemetryListener
    {
        Task ReportProjectTelemetryDataAsync(ProjectTelemetryData data, CancellationToken cancellationToken);
    }
}
