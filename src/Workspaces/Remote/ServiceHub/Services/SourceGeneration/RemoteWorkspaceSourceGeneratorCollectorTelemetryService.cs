// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;

namespace Microsoft.CodeAnalysis.Remote.Services.SourceGeneration;

[ExportWorkspaceServiceFactory(typeof(ISourceGeneratorTelemetryCollectorWorkspaceService), WorkspaceKind.RemoteWorkspace), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RemoteWorkspaceSourceGeneratorCollectorTelemetryService() : IWorkspaceServiceFactory
{
    private readonly SourceGeneratorTelemetryCollectorWorkspaceService _service = new();

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices) => _service;
}
