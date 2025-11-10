// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;

namespace Microsoft.VisualStudio.LanguageServices;

/// <summary>
/// Exports the reporting workspace service, which in the VS case is implemented on the same type that implements the collector factory, so we just forward it over.
/// This is mostly a workaround for the fact we can't have a workspace factory that exports multiple different kinds of workspaces at once.
/// </summary>
[ExportWorkspaceServiceFactory(typeof(ISourceGeneratorTelemetryReporterWorkspaceService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioSourceGeneratorTelemetryReporterWorkspaceServiceFactory(VisualStudioSourceGeneratorTelemetryCollectorWorkspaceServiceFactory implementation) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return implementation;
    }
}
