// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SourceGeneratorTelemetry;

/// <summary>
/// A service that lets us know when we should report telemetry for source generators. This is only created in the main process since we want to do reporting from there.
/// </summary>
internal interface ISourceGeneratorTelemetryReporterWorkspaceService : IWorkspaceService
{
    void QueueReportingOfTelemetry();
}
