// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Export(typeof(IVSTypeScriptDiagnosticService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptDiagnosticService() : IVSTypeScriptDiagnosticService
{
    public Task<ImmutableArray<VSTypeScriptDiagnosticData>> GetPushDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
    {
        // This type is only for push diagnostics, which is now no longer how any of our diagnostic systems work. So
        // this just returns nothing.
        return SpecializedTasks.EmptyImmutableArray<VSTypeScriptDiagnosticData>();
    }

    [Obsolete]
    public IDisposable RegisterDiagnosticsUpdatedEventHandler(Action<VSTypeScriptDiagnosticsUpdatedArgsWrapper> action)
        => new EventHandlerWrapper();

    public IDisposable RegisterDiagnosticsUpdatedEventHandler(Action<ImmutableArray<VSTypeScriptDiagnosticsUpdatedArgsWrapper>> action)
        => new EventHandlerWrapper();

    private sealed class EventHandlerWrapper : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
