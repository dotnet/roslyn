// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Export(typeof(IVSTypeScriptDiagnosticService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptDiagnosticService(IDiagnosticService service) : IVSTypeScriptDiagnosticService
{
    private readonly IDiagnosticService _service = service;

    public Task<ImmutableArray<VSTypeScriptDiagnosticData>> GetPushDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
    {
        // This type is only for push diagnostics, which is now no longer how any of our diagnostic systems work. So
        // this just returns nothing.
        return SpecializedTasks.EmptyImmutableArray<VSTypeScriptDiagnosticData>();
    }

    [Obsolete]
    public IDisposable RegisterDiagnosticsUpdatedEventHandler(Action<VSTypeScriptDiagnosticsUpdatedArgsWrapper> action)
        => new EventHandlerWrapper(_service, action);

    public IDisposable RegisterDiagnosticsUpdatedEventHandler(Action<ImmutableArray<VSTypeScriptDiagnosticsUpdatedArgsWrapper>> action)
        => new EventHandlerWrapper(_service, action);

    private sealed class EventHandlerWrapper : IDisposable
    {
        private readonly IDiagnosticService _service;
        private readonly EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>> _handler;

        [Obsolete]
        internal EventHandlerWrapper(IDiagnosticService service, Action<VSTypeScriptDiagnosticsUpdatedArgsWrapper> action)
        {
            _service = service;
            _handler = (sender, argsCollection) =>
            {
                foreach (var args in argsCollection)
                    action(new VSTypeScriptDiagnosticsUpdatedArgsWrapper(args));
            };
            _service.DiagnosticsUpdated += _handler;
        }

        internal EventHandlerWrapper(IDiagnosticService service, Action<ImmutableArray<VSTypeScriptDiagnosticsUpdatedArgsWrapper>> action)
        {
            _service = service;
            _handler = (sender, argsCollection) =>
            {
                action(ImmutableArray.CreateRange(argsCollection, static args => new VSTypeScriptDiagnosticsUpdatedArgsWrapper(args)));
            };
            _service.DiagnosticsUpdated += _handler;
        }

        public void Dispose()
        {
            _service.DiagnosticsUpdated -= _handler;
        }
    }
}
